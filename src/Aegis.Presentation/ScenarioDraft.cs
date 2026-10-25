using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Aegis.Simulation;

namespace Aegis.Presentation;

public sealed class ShelterDraft
{
    public string Name { get; set; } = "";
    public string Demand { get; set; } = "0";
    public string X { get; set; } = "480";
    public string Y { get; set; } = "180";
    public string TravelTicks { get; set; } = "";
    public string Priority { get; set; } = "0";
}

// Strings preserve incomplete user input until explicit validation. The source is never edited.
public sealed class ScenarioDraft : INotifyPropertyChanged
{
    private bool usesLogistics;
    private bool usesFleet;
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Name { get; set; }
    public string Capacity { get; set; }
    public bool UsesLogistics { get => usesLogistics; set { if (usesLogistics == value) return; usesLogistics = value; if (!value) usesFleet = false; PropertyChanged?.Invoke(this, new(nameof(UsesLogistics))); PropertyChanged?.Invoke(this, new(nameof(UsesFleet))); } }
    public bool UsesFleet { get => usesFleet; set { if (usesFleet == value) return; usesFleet = value; if (value) UsesLogistics = true; PropertyChanged?.Invoke(this, new(nameof(UsesFleet))); } }
    public string FleetSize { get; set; }
    public string DepotStock { get; set; }
    public string TravelTicks { get; set; }
    public string ReturnTicks { get; set; }
    public ObservableCollection<ShelterDraft> Shelters { get; } = new();

    public ScenarioDraft(ScenarioDefinition source)
    {
        source.Validate();
        if (source.Geography != null) throw new ArgumentException("Edit the road GeoJSON and import it again to change a geographical exercise.");
        Name = source.Name; Capacity = Number(source.Capacity); UsesLogistics = source.SchemaVersion >= 2; UsesFleet = source.SchemaVersion == 3;
        FleetSize = Number(source.FleetSize ?? 3);
        DepotStock = Number(source.DepotStock ?? source.Shelters.Sum(s => s.Demand));
        TravelTicks = Number(source.TravelTicks); ReturnTicks = Number(source.ReturnTicks);
        foreach (var s in source.Shelters) Shelters.Add(new() { Name = s.Name, Demand = Number(s.Demand), X = Number(s.X), Y = Number(s.Y), TravelTicks = s.TravelTicks.HasValue ? Number(s.TravelTicks.Value) : "", Priority = Number(s.Priority) });
    }

    public bool AddShelter()
    {
        if (Shelters.Count >= 64) return false;
        int index = 1;
        while (Shelters.Any(s => s.Name.Equals("Shelter-" + index, StringComparison.OrdinalIgnoreCase))) index++;
        Shelters.Add(new() { Name = "Shelter-" + index }); return true;
    }

    public bool TryBuild(out ScenarioDefinition? scenario, out string message)
    {
        scenario = null;
        try
        {
            if (string.IsNullOrWhiteSpace(Name) || Name.Any(char.IsControl)) throw new ArgumentException("Scenario name must be printable and nonempty.");
            var rows = new List<ShelterDefinition>();
            foreach (var row in Shelters)
            {
                if (string.IsNullOrWhiteSpace(row.Name) || row.Name.Any(char.IsControl)) throw new ArgumentException("Each shelter needs a printable name.");
                rows.Add(new(row.Name.Trim(), Integer(row.Demand, $"{row.Name}: demand"), Coordinate(row.X, $"{row.Name}: X"), Coordinate(row.Y, $"{row.Name}: Y"),
                    UsesLogistics && !string.IsNullOrWhiteSpace(row.TravelTicks) ? Integer(row.TravelTicks, $"{row.Name}: travel override") : null,
                    UsesFleet ? Integer(row.Priority, $"{row.Name}: priority") : 0));
            }
            var candidate = new ScenarioDefinition(UsesFleet ? 3 : UsesLogistics ? 2 : 1, Name.Trim(), Integer(Capacity, "Capacity"), rows.ToArray(),
                UsesLogistics ? Integer(DepotStock, "Depot stock") : null,
                UsesLogistics ? Integer(TravelTicks, "Travel ticks") : 1,
                UsesLogistics ? Integer(ReturnTicks, "Return ticks") : 0, UsesFleet ? Integer(FleetSize, "Vehicle count") : null);
            candidate.Validate();
            scenario = candidate;
            message = $"Valid schema {candidate.SchemaVersion} scenario / {rows.Count} shelters / {rows.Sum(s => s.Demand)} packets requested.";
            return true;
        }
        catch (ArgumentException error) { message = error.Message; return false; }
    }

    private static int Integer(string value, string field) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : throw new ArgumentException(field + " must be a whole number.");
    private static double Coordinate(string value, string field) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) && double.IsFinite(parsed) ? parsed : throw new ArgumentException(field + " must be a finite number; use a dot for decimals.");
    private static string Number<T>(T value) where T : IFormattable => value.ToString(null, CultureInfo.InvariantCulture);
}
