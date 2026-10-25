using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aegis.Simulation;

namespace Aegis.Infrastructure;

// Compiles a bounded, fictional WGS84 road graph into self-contained deterministic routes.
public static class GeoExercise
{
    private sealed record Node(string Id, string Name, double Lon, double Lat, RoadPoint Point, string Kind, int Demand, int Priority);
    public static ScenarioDefinition Load(string path)
    {
        if (new FileInfo(path).Length > 524288) throw new InvalidDataException("GeoJSON is limited to 512 KB.");
        return Parse(File.ReadAllText(path));
    }
    public static ScenarioDefinition Parse(string text)
    {
        var layer = GeoJsonLayer.Parse(text);
        try
        {
            using var json = JsonDocument.Parse(text);
            var root = json.RootElement; var settings = root.GetProperty("exercise");
            var features = root.GetProperty("features").EnumerateArray().ToArray();
            RoadPoint Local(LayerPoint p) => new(50 + p.X * 0.8, 55 + p.Y * 0.8);
            var nodes = new Dictionary<string, Node>(StringComparer.Ordinal);
            var nodeOrder = new List<string>();
            foreach (var f in features.Where(f => f.GetProperty("geometry").GetProperty("type").GetString() == "Point"))
            {
                string id = f.GetProperty("id").GetString()!; var p = f.GetProperty("properties"); var coordinates = f.GetProperty("geometry").GetProperty("coordinates");
                nodeOrder.Add(id);
                string kind = p.GetProperty("kind").GetString()!;
                if (kind is not ("depot" or "shelter" or "junction")) throw new InvalidDataException("Point kind must be depot, shelter or junction.");
                nodes.Add(id, new(id, p.GetProperty("name").GetString()!, coordinates[0].GetDouble(), coordinates[1].GetDouble(), Local(layer.Features.Single(x => x.Id == id).Parts[0][0]), kind,
                    kind == "shelter" ? p.GetProperty("demand").GetInt32() : 0, p.TryGetProperty("priority", out var rank) ? rank.GetInt32() : 0));
            }
            var depots = nodes.Values.Where(n => n.Kind == "depot").ToArray();
            if (depots.Length != 1 || !nodes.Values.Any(n => n.Kind == "shelter")) throw new InvalidDataException("An exercise needs exactly one depot and at least one shelter.");
            var depot = depots[0]; var roads = new List<RoadDefinition>();
            foreach (var f in features.Where(f => f.GetProperty("geometry").GetProperty("type").GetString() != "Point"))
            {
                if (f.GetProperty("geometry").GetProperty("type").GetString() != "LineString") throw new InvalidDataException("Exercise roads must be LineStrings.");
                string id = f.GetProperty("id").GetString()!; var p = f.GetProperty("properties");
                string from = p.GetProperty("from").GetString()!, to = p.GetProperty("to").GetString()!;
                if (!nodes.TryGetValue(from, out var a) || !nodes.TryGetValue(to, out var b) || from == to) throw new InvalidDataException("Road references an unknown or identical endpoint.");
                var coordinates = f.GetProperty("geometry").GetProperty("coordinates").EnumerateArray().Select(c => (Lon: c[0].GetDouble(), Lat: c[1].GetDouble())).ToArray();
                bool Match((double Lon, double Lat) point, Node n) => Math.Abs(point.Lon - n.Lon) < 1e-8 && Math.Abs(point.Lat - n.Lat) < 1e-8;
                if (!Match(coordinates[0], a) || !Match(coordinates[^1], b)) throw new InvalidDataException("Road geometry must match its from/to point positions.");
                double length = 0; for (int i = 1; i < coordinates.Length; i++) length += Distance(coordinates[i - 1].Lon, coordinates[i - 1].Lat, coordinates[i].Lon, coordinates[i].Lat);
                if (length < 0.01 || length > 1000000) throw new InvalidDataException("Road length is outside the supported range.");
                var points = layer.Features.Single(x => x.Id == id).Parts[0].Select(Local).ToArray(); points[0] = a.Point; points[^1] = b.Point;
                roads.Add(new(id, from, to, (int)Math.Ceiling(length), points));
            }
            roads.Sort((a, b) => StringComparer.Ordinal.Compare(a.Id, b.Id));
            var distances = nodes.Keys.ToDictionary(id => id, _ => int.MaxValue, StringComparer.Ordinal);
            var previous = new Dictionary<string, (string Node, RoadDefinition Road)>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal); distances[depot.Id] = 0;
            while (true)
            {
                string? current = nodes.Keys.Where(id => !visited.Contains(id) && distances[id] != int.MaxValue).OrderBy(id => distances[id]).ThenBy(id => id, StringComparer.Ordinal).FirstOrDefault();
                if (current == null) break; visited.Add(current);
                foreach (var road in roads.Where(r => r.From == current || r.To == current))
                {
                    string next = road.From == current ? road.To : road.From; int candidate = checked(distances[current] + road.Meters);
                    if (candidate >= distances[next]) continue;
                    distances[next] = candidate; previous[next] = (current, road);
                }
            }
            int metersPerTick = settings.GetProperty("metersPerTick").GetInt32();
            if (metersPerTick is < 1 or > 100000) throw new InvalidDataException("metersPerTick must be 1..100000.");
            var routes = new List<RoadRoute>(); var shelters = new List<ShelterDefinition>();
            // Original feature order is retained as the priority tie-break order.
            foreach (var node in nodeOrder.Select(id => nodes[id]).Where(n => n.Kind == "shelter"))
            {
                if (distances[node.Id] == int.MaxValue) throw new InvalidDataException("Shelter is unreachable: " + node.Name);
                var route = new List<RoadDefinition>(); string cursor = node.Id;
                while (cursor != depot.Id) { var step = previous[cursor]; route.Add(step.Road); cursor = step.Node; }
                route.Reverse(); cursor = depot.Id; var points = new List<RoadPoint> { depot.Point };
                foreach (var road in route)
                {
                    bool forward = road.From == cursor; points.AddRange((forward ? road.Points : road.Points.Reverse()).Skip(1)); cursor = forward ? road.To : road.From;
                }
                routes.Add(new(node.Name, node.Id, route.Select(r => r.Id).ToArray(), points.ToArray(), distances[node.Id]));
                int ticks = (distances[node.Id] + metersPerTick - 1) / metersPerTick;
                shelters.Add(new(node.Name, node.Demand, node.Point.X, node.Point.Y, ticks, node.Priority));
            }
            var result = new ScenarioDefinition(4, settings.GetProperty("name").GetString()!, settings.GetProperty("capacity").GetInt32(), shelters.ToArray(),
                settings.GetProperty("stock").GetInt32(), 1, 0, settings.GetProperty("fleetSize").GetInt32(),
                new(depot.Id, depot.Point, metersPerTick, roads.ToArray(), routes.ToArray(), Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))));
            result.Validate();
            if (Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(result, ScenarioFiles.JsonOptions)) > 262144) throw new InvalidDataException("Compiled exercise exceeds the scenario size limit; simplify the network.");
            return result;
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or ArgumentException or OverflowException)
        { throw new InvalidDataException("Invalid geographical exercise: " + e.Message, e); }
    }

    // Spherical training-model distance, not an ellipsoidal survey measurement.
    public static double Distance(double lon1, double lat1, double lon2, double lat2)
    {
        double radians = Math.PI / 180, dy = (lat2 - lat1) * radians, dx = (lon2 - lon1) * radians;
        double a = Math.Pow(Math.Sin(dy / 2), 2) + Math.Cos(lat1 * radians) * Math.Cos(lat2 * radians) * Math.Pow(Math.Sin(dx / 2), 2);
        return 6371000 * 2 * Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1)));
    }
}
