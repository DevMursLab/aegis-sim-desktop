namespace Aegis.Simulation;

public enum RunStatus { Ready, Running, Paused, Completed }
public sealed record ShelterState(string Name, int InitialDemand, int Remaining, int BlockedUntil = 0);
public sealed record DeliveryEvent(int Tick, string Shelter, int Amount, string Kind = "Delivery");

// One owner mutates a session. No UI, timer, database or wall clock in this model.
public sealed class SimulationSession
{
    private readonly ShelterState[] initial;
    private ShelterState[] shelters;
    public int Capacity { get; }
    public int Tick { get; private set; }
    public int DeliveredTotal { get; private set; }
    public int InitialDemand { get; }
    public ScenarioDefinition Scenario { get; }
    public RunStatus Status { get; private set; }
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
        Scenario = scenario with { Shelters = scenario.Shelters.ToArray() };
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
        if (Status == RunStatus.Completed) throw new InvalidOperationException("Reset before advancing a completed run.");
        if (Tick >= 20000) throw new InvalidOperationException("Exercise tick limit reached; reset or use a smaller scenario.");
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
        Tick = 0;
        DeliveredTotal = 0;
        Status = InitialDemand == 0 ? RunStatus.Completed : RunStatus.Ready;
    }

    public static SimulationSession CreateDemo() => new(ScenarioDefinition.FloodDemo());
}
