namespace Aegis.Simulation;

public enum RunStatus { Ready, Running, Paused, Completed }
public sealed record ShelterState(string Name, int InitialDemand, int Remaining);
public sealed record DeliveryEvent(int Tick, string Shelter, int Amount);

// One owner mutates a session. No UI, timer, database or wall clock in this model.
public sealed class SimulationSession
{
    private readonly ShelterState[] initial;
    private ShelterState[] shelters;
    public int Capacity { get; }
    public int Tick { get; private set; }
    public int DeliveredTotal { get; private set; }
    public int InitialDemand { get; }
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
        shelters = initial.ToArray();
        Status = InitialDemand == 0 ? RunStatus.Completed : RunStatus.Ready;
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
        int index = Array.FindIndex(shelters, s => s.Remaining > 0);
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

    public static SimulationSession CreateDemo() => new(
        new[] { ("Shelter-A", 25), ("Shelter-B", 5), ("Shelter-C", 18) }, 10);
}
