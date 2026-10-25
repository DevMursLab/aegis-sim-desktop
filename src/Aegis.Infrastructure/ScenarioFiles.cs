using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;
using Aegis.Simulation;

namespace Aegis.Infrastructure;

public static class ScenarioFiles
{
    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
    { WriteIndented = false, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 32 };

    public static ScenarioDefinition Load(string path)
    {
        if (new FileInfo(path).Length > 262144) throw new InvalidDataException("Scenario file is limited to 256 KB.");
        string text = File.ReadAllText(path);
        return Parse(text, Path.GetExtension(path).Equals(".xml", StringComparison.OrdinalIgnoreCase));
    }

    public static ScenarioDefinition Parse(string text, bool xml)
    {
        if (text.Length > 262144) throw new InvalidDataException("Scenario is too large.");
        ScenarioDefinition scenario;
        if (!xml) scenario = JsonSerializer.Deserialize<ScenarioDefinition>(text, JsonOptions) ?? throw new InvalidDataException("Empty scenario.");
        else
        {
            using var input = new StringReader(text);
            using var reader = XmlReader.Create(input, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 262144 });
            var root = XDocument.Load(reader).Root ?? throw new InvalidDataException("Missing Scenario element.");
            if (root.Name != "Scenario" || root.Elements().Any(e => e.Name != "Shelter")) throw new InvalidDataException("Unknown XML element.");
            var rootAttributes = new[] { "version", "name", "capacity", "stock", "travelTicks", "returnTicks", "fleetSize" };
            var shelterAttributes = new[] { "name", "demand", "x", "y", "travelTicks" };
            if (root.Attributes().Any(a => !rootAttributes.Contains(a.Name.ToString())) || root.Elements().Any(s =>
                s.HasElements || !string.IsNullOrWhiteSpace(s.Value) || s.Attributes().Any(a => !shelterAttributes.Contains(a.Name.ToString()))))
                throw new InvalidDataException("Unknown XML attribute or nested shelter content.");
            scenario = new(int.Parse(Required(root, "version"), CultureInfo.InvariantCulture), Required(root, "name"),
                int.Parse(Required(root, "capacity"), CultureInfo.InvariantCulture), root.Elements("Shelter").Select(s => new ShelterDefinition(
                    Required(s, "name"), int.Parse(Required(s, "demand"), CultureInfo.InvariantCulture),
                    double.Parse(Required(s, "x"), CultureInfo.InvariantCulture), double.Parse(Required(s, "y"), CultureInfo.InvariantCulture),
                    OptionalInt(s, "travelTicks"))).ToArray(), OptionalInt(root, "stock"), OptionalInt(root, "travelTicks") ?? 1, OptionalInt(root, "returnTicks") ?? 0, OptionalInt(root, "fleetSize"));
        }
        scenario.Validate();
        return scenario;
    }

    public static void Save(string path, ScenarioDefinition scenario)
    {
        scenario.Validate();
        if (Path.GetExtension(path).Equals(".xml", StringComparison.OrdinalIgnoreCase))
            new XDocument(new XElement("Scenario", new XAttribute("version", scenario.SchemaVersion), new XAttribute("name", scenario.Name), new XAttribute("capacity", scenario.Capacity),
                scenario.SchemaVersion >= 2 ? new object[] { new XAttribute("stock", scenario.DepotStock!.Value), new XAttribute("travelTicks", scenario.TravelTicks), new XAttribute("returnTicks", scenario.ReturnTicks) } : null,
                scenario.FleetSize.HasValue ? new XAttribute("fleetSize", scenario.FleetSize.Value) : null,
                scenario.Shelters.Select(s => new XElement("Shelter", new XAttribute("name", s.Name), new XAttribute("demand", s.Demand), new XAttribute("x", s.X), new XAttribute("y", s.Y),
                    s.TravelTicks.HasValue ? new XAttribute("travelTicks", s.TravelTicks.Value) : null)))).Save(path);
        else File.WriteAllText(path, JsonSerializer.Serialize(scenario, new JsonSerializerOptions(JsonOptions) { WriteIndented = true }));
    }
    private static string Required(XElement element, string attribute) => (string?)element.Attribute(attribute) ?? throw new InvalidDataException($"Missing {attribute}.");
    private static int? OptionalInt(XElement element, string attribute) => element.Attribute(attribute) is { } value ? int.Parse(value.Value, CultureInfo.InvariantCulture) : null;
}
