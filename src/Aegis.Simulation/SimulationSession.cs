using System.Text.Json.Serialization;
namespace Aegis.Simulation;

public enum RunStatus { Ready, Running, Paused, Completed, SupplyExhausted }
public enum VehiclePhase { AtDepot, Outbound, Returning }
public sealed record VehicleState(VehiclePhase Phase, string? Destination, int Load, int TicksRemaining, int Duration,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] RoadRoute? Route = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] RoadJourney? Journey = null)
{
    public VehicleState Snapshot() => this with
    {
        Route = Route == null ? null : Route with { RoadIds = Route.RoadIds.ToArray(), Points = Route.Points.ToArray() },
        Journey = Journey == null ? null : Journey with { Points = Journey.Points.ToArray() }
    };
}
public sealed record ShelterState(string Name, int InitialDemand, int Remaining, int BlockedUntil = 0,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] int Priority = 0);
public sealed record VehicleAction(string VehicleId, string Shelter, int Amount, string Kind,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Reason = null);
public sealed record DeliveryEvent(int Tick, string Shelter, int Amount, string Kind = "Delivery",
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] VehicleAction[]? Actions = null);

// One owner mutates a session. No UI, timer, database or wall clock in this model.
public sealed partial class SimulationSession
{
    private readonly ShelterState[] initial;
    private ShelterState[] shelters;
    public int Capacity { get; }
    public int Tick { get; private set; }
    public int DeliveredTotal { get; private set; }
    public int InitialDemand { get; }
    public ScenarioDefinition Scenario { get; }
    public RunStatus Status { get; private set; }
    public bool UsesLogistics => Scenario.SchemaVersion >= 2;
    public bool UsesFleet => Scenario.SchemaVersion >= 3;
    public int? DepotStockRemaining { get; private set; }
    public int RestockedTotal { get; private set; }
    private VehicleState singleVehicle = new(VehiclePhase.AtDepot, null, 0, 0, 0);
    public VehicleState Vehicle { get => UsesFleet ? Vehicles[0].State : singleVehicle; private set => singleVehicle = value; }
    public int InTransit => UsesFleet ? fleet.Sum(v => v.State.Load) : Vehicle.Load;
    public IReadOnlyList<ShelterState> Shelters => Array.AsReadOnly(shelters);

    public SimulationSession(IEnumerable<(string Name, int Demand)> definitions, int capacity)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        initial = definitions.Select(d => new ShelterState(d.Name, d.Demand, d.Demand)).ToArray();
        if (initial.Any(s => string.IsNullOrWhiteSpace(s.Name) || s.InitialDemand < 0))
            throw new ArgumentException("Shelters need names and nonnegative demand.", nameof(definitions));
        if (initial.Select(s => s.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != initial.Length)
            throw new ArgumentException("Shelter names must be unique.", nameof(definitions));
        InitialDemand = initial.Aggregate(0, (sum, s) => checked(sum + s.InitialDemand));
        Capacity = capacity;
        Scenario = new(1, "Custom synthetic scenario", capacity, initial.Select((s, i) => new ShelterDefinition(s.Name, s.InitialDemand, 480 + i % 2 * 430, 180 + i / 2 * 310)).ToArray());
        shelters = initial.ToArray();
        Status = InitialDemand == 0 ? RunStatus.Completed : RunStatus.Ready;
    }

    public SimulationSession(ScenarioDefinition scenario) : this(Validated(scenario).Shelters.Select(s => (s.Name, s.Demand)), scenario.Capacity)
    {
        Scenario = scenario.Snapshot();
        initial = scenario.Shelters.Select(s => new ShelterState(s.Name, s.Demand, s.Demand, Priority: s.Priority)).ToArray();
        shelters = initial.ToArray();
        DepotStockRemaining = scenario.DepotStock;
        InitializeFleet();
        UpdateLogisticsStatus();
    }

    public void Restock(int amount)
    {
        if (!UsesLogistics) throw new InvalidOperationException("Restocking requires a schema 2 or 3 logistics scenario.");
        if (Status == RunStatus.Completed) throw new InvalidOperationException("Completed runs are immutable.");
        if (amount <= 0 || (long)Scenario.DepotStock!.Value + RestockedTotal + amount > 10000)
            throw new ArgumentOutOfRangeException(nameof(amount), "Total supplied stock must stay within 10000 packets.");
        DepotStockRemaining += amount;
        RestockedTotal += amount;
        if (Status == RunStatus.SupplyExhausted) Status = RunStatus.Paused;
    }

    private static ScenarioDefinition Validated(ScenarioDefinition scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        scenario.Validate();
        return scenario;
    }

    public void CloseRoute(string shelterName, int duration)
    {
        if (Status == RunStatus.Completed) throw new InvalidOperationException("Completed runs are immutable; create a new run.");
        if (duration is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(duration), "Closure duration must be 1–100 ticks.");
        int index = Array.FindIndex(shelters, s => s.Name == shelterName);
        if (index < 0) throw new ArgumentException("Unknown shelter.", nameof(shelterName));
        shelters[index] = shelters[index] with { BlockedUntil = checked(Tick + duration) };
    }

    public void Start()
    {
        if (Status is not (RunStatus.Ready or RunStatus.Paused))
            throw new InvalidOperationException("Only a ready or paused run can start.");
        Status = RunStatus.Running;
    }

    public void Pause()
    {
        if (Status != RunStatus.Running) throw new InvalidOperationException("Only a running run can pause.");
        Status = RunStatus.Paused;
    }

    // Running: automatic tick. Ready/Paused: one manual tick. Completed: reject.
    public DeliveryEvent Step()
    {
        if (Status is RunStatus.Completed or RunStatus.SupplyExhausted) throw new InvalidOperationException("Reset a completed run, or restock a supply-exhausted run before advancing.");
        if (Tick >= 20000) throw new InvalidOperationException("Exercise tick limit reached; reset or use a smaller scenario.");
        if (UsesFleet) return StepFleet();
        if (UsesLogistics) return StepLogistics();
        int nextTick = Tick + 1;
        int index = Array.FindIndex(shelters, s => s.Remaining > 0 && s.BlockedUntil < nextTick);
        if (index < 0) { Tick = nextTick; return new DeliveryEvent(Tick, "All pending routes closed", 0, "Waiting"); }
        var shelter = shelters[index];
        int amount = Math.Min(shelter.Remaining, Capacity);
        Tick++;
        DeliveredTotal += amount;
        shelters[index] = shelter with { Remaining = shelter.Remaining - amount };
        if (DeliveredTotal == InitialDemand) Status = RunStatus.Completed;
        return new DeliveryEvent(Tick, shelter.Name, amount);
    }

    public void Reset()
    {
        shelters = initial.ToArray();
        roadClosures.Clear();
        JunctionRoutingEnabled = false;
        Tick = 0;
        DeliveredTotal = 0;
        Status = InitialDemand == 0 ? RunStatus.Completed : RunStatus.Ready;
        DepotStockRemaining = Scenario.DepotStock;
        RestockedTotal = 0;
        Vehicle = new(VehiclePhase.AtDepot, null, 0, 0, 0);
        InitializeFleet();
        UpdateLogisticsStatus();
    }

    private DeliveryEvent StepLogistics()
    {
        Tick++;
        if (Vehicle.Phase == VehiclePhase.Outbound)
        {
            int index = Array.FindIndex(shelters, s => s.Name == Vehicle.Destination);
            if (shelters[index].BlockedUntil >= Tick) return new(Tick, Vehicle.Destination!, 0, "RouteWait");
            Vehicle = Vehicle with { TicksRemaining = Vehicle.TicksRemaining - 1 };
            if (Vehicle.TicksRemaining > 0) return new(Tick, Vehicle.Destination!, 0, "Travelling");
            int amount = Vehicle.Load;
            string destination = Vehicle.Destination!;
            shelters[index] = shelters[index] with { Remaining = shelters[index].Remaining - amount };
            DeliveredTotal += amount;
            Vehicle = Scenario.ReturnTicks == 0 ? new(VehiclePhase.AtDepot, null, 0, 0, 0) :
                new(VehiclePhase.Returning, destination, 0, Scenario.ReturnTicks, Scenario.ReturnTicks);
            UpdateLogisticsStatus();
            return new(Tick, destination, amount);
        }
        if (Vehicle.Phase == VehiclePhase.Returning)
        {
            string destination = Vehicle.Destination!;
            Vehicle = Vehicle with { TicksRemaining = Vehicle.TicksRemaining - 1 };
            if (Vehicle.TicksRemaining == 0) Vehicle = new(VehiclePhase.AtDepot, null, 0, 0, 0);
            UpdateLogisticsStatus();
            return new(Tick, destination, 0, Vehicle.Phase == VehiclePhase.AtDepot ? "Returned" : "Returning");
        }
        int target = Array.FindIndex(shelters, s => s.Remaining > 0 && s.BlockedUntil < Tick);
        if (target < 0) return new(Tick, "All pending routes closed", 0, "Waiting");
        int load = Math.Min(Math.Min(Capacity, DepotStockRemaining!.Value), shelters[target].Remaining);
        DepotStockRemaining -= load;
        var definition = Scenario.Shelters.First(s => s.Name == shelters[target].Name);
        int duration = definition.TravelTicks ?? Scenario.TravelTicks;
        Vehicle = new(VehiclePhase.Outbound, shelters[target].Name, load, duration, duration);
        return new(Tick, shelters[target].Name, 0, "Dispatched");
    }

    private void UpdateLogisticsStatus()
    {
        if (UsesFleet) { UpdateFleetStatus(); return; }
        if (!UsesLogistics || Vehicle.Phase != VehiclePhase.AtDepot) return;
        if (DeliveredTotal == InitialDemand) Status = RunStatus.Completed;
        else if (DepotStockRemaining == 0) Status = RunStatus.SupplyExhausted;
    }

    public static SimulationSession CreateDemo() => new(ScenarioDefinition.FloodDemo());
}
