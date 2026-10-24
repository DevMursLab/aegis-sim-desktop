namespace Aegis.Simulation;

public sealed record ShelterDefinition(string Name, int Demand, double X, double Y, int? TravelTicks = null);
public sealed record ScenarioDefinition(int SchemaVersion, string Name, int Capacity, ShelterDefinition[] Shelters,
    int? DepotStock = null, int TravelTicks = 1, int ReturnTicks = 0)
{
    public void Validate()
    {
        if (SchemaVersion is not (1 or 2) || string.IsNullOrWhiteSpace(Name) || Name.Length > 80)
            throw new ArgumentException("Expected scenario schema 1 or 2 and a name of 1–80 characters.");
        if (Capacity is <= 0 or > 10000 || Shelters == null || Shelters.Length > 64)
            throw new ArgumentException("Use capacity 1–10000 and at most 64 shelters.");
        if (Shelters.Any(s => s == null || string.IsNullOrWhiteSpace(s.Name) || s.Name.Length > 40 || s.Demand is < 0 or > 10000 ||
            !double.IsFinite(s.X) || !double.IsFinite(s.Y) || s.X is < 50 or > 1050 || s.Y is < 70 or > 700))
            throw new ArgumentException("Shelters need names, demand 0–10000 and finite local coordinates x=50–1050, y=70–700.");
        if (Shelters.Select(s => s.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Shelters.Length)
            throw new ArgumentException("Shelter names must be unique.");
        if (Shelters.Sum(s => s.Demand) > 10000) throw new ArgumentException("Total scenario demand is limited to 10000 packets.");
        if (SchemaVersion == 1 && (DepotStock != null || TravelTicks != 1 || ReturnTicks != 0 || Shelters.Any(s => s.TravelTicks != null)))
            throw new ArgumentException("Stock and travel settings require schema 2; schema 1 preserves legacy instant visits.");
        if (SchemaVersion == 2 && (DepotStock is null or < 0 or > 10000 || TravelTicks is < 1 or > 20 || ReturnTicks is < 0 or > 20 ||
            Shelters.Any(s => s.TravelTicks is < 1 or > 20)))
            throw new ArgumentException("Schema 2 requires stock 0–10000, outbound travel 1–20 ticks and return travel 0–20 ticks.");
    }
    public static ScenarioDefinition FloodDemo() => new(1, "Flood Relief / Synthetic-01", 10,
        new[] { new ShelterDefinition("Shelter-A", 25, 480, 180), new ShelterDefinition("Shelter-B", 5, 910, 180), new ShelterDefinition("Shelter-C", 18, 480, 490) });
    public static ScenarioDefinition LogisticsDemo() => FloodDemo() with
    { SchemaVersion = 2, Name = "Finite Stock / Travel Training", DepotStock = 30, TravelTicks = 2, ReturnTicks = 1 };
}
