namespace Aegis.Simulation;

public sealed record ShelterDefinition(string Name, int Demand, double X, double Y);
public sealed record ScenarioDefinition(int SchemaVersion, string Name, int Capacity, ShelterDefinition[] Shelters)
{
    public void Validate()
    {
        if (SchemaVersion != 1 || string.IsNullOrWhiteSpace(Name) || Name.Length > 80)
            throw new ArgumentException("Expected scenario schema 1 and a name of 1–80 characters.");
        if (Capacity is <= 0 or > 10000 || Shelters == null || Shelters.Length > 64)
            throw new ArgumentException("Use capacity 1–10000 and at most 64 shelters.");
        if (Shelters.Any(s => s == null || string.IsNullOrWhiteSpace(s.Name) || s.Name.Length > 40 || s.Demand is < 0 or > 10000 ||
            !double.IsFinite(s.X) || !double.IsFinite(s.Y) || s.X is < 50 or > 1050 || s.Y is < 70 or > 700))
            throw new ArgumentException("Shelters need names, demand 0–10000 and finite local coordinates x=50–1050, y=70–700.");
        if (Shelters.Select(s => s.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Shelters.Length)
            throw new ArgumentException("Shelter names must be unique.");
        if (Shelters.Sum(s => s.Demand) > 10000) throw new ArgumentException("Total scenario demand is limited to 10000 packets.");
    }
    public static ScenarioDefinition FloodDemo() => new(1, "Flood Relief / Synthetic-01", 10,
        new[] { new ShelterDefinition("Shelter-A", 25, 480, 180), new ShelterDefinition("Shelter-B", 5, 910, 180), new ShelterDefinition("Shelter-C", 18, 480, 490) });
}
