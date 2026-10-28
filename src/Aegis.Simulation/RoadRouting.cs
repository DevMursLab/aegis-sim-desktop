namespace Aegis.Simulation;

public sealed record RoadClosure(string RoadId, int BlockedUntil);

// Stable undirected Dijkstra: node and road IDs break equal-distance ties.
public static class RoadRouting
{
    public static RoadRoute? Find(GeographyDefinition map, string shelter, IEnumerable<string> blockedRoads)
    {
        var target = map.Routes.Single(r => r.Shelter == shelter);
        return FindBetween(map, map.DepotId, target.TargetNode, shelter, blockedRoads);
    }

    public static RoadPoint NodePoint(GeographyDefinition map, string node)
    {
        if (node == map.DepotId) return map.Depot;
        var road = map.Roads.First(r => r.From == node || r.To == node);
        return road.From == node ? road.Points[0] : road.Points[^1];
    }

    public static RoadRoute? FindBetween(GeographyDefinition map, string from, string to, string shelter, IEnumerable<string> blockedRoads)
    {
        var blocked = blockedRoads.ToHashSet(StringComparer.Ordinal);
        var roads = map.Roads.OrderBy(r => r.Id, StringComparer.Ordinal).ToArray();
        var nodes = roads.SelectMany(r => new[] { r.From, r.To }).Distinct(StringComparer.Ordinal).ToArray();
        var distances = nodes.ToDictionary(n => n, _ => int.MaxValue, StringComparer.Ordinal);
        var previous = new Dictionary<string, (string Node, RoadDefinition Road)>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        distances[from] = 0;
        while (true)
        {
            string? current = nodes.Where(n => !visited.Contains(n) && distances[n] != int.MaxValue)
                .OrderBy(n => distances[n]).ThenBy(n => n, StringComparer.Ordinal).FirstOrDefault();
            if (current == null) return null;
            if (current == to) break;
            visited.Add(current);
            foreach (var road in roads.Where(r => !blocked.Contains(r.Id) && (r.From == current || r.To == current)))
            {
                string next = road.From == current ? road.To : road.From;
                int distance = checked(distances[current] + road.Meters);
                if (distance >= distances[next]) continue;
                distances[next] = distance; previous[next] = (current, road);
            }
        }
        string cursor = to;
        var path = new List<RoadDefinition>();
        while (cursor != from) { var step = previous[cursor]; path.Add(step.Road); cursor = step.Node; }
        path.Reverse(); cursor = from;
        var points = new List<RoadPoint> { NodePoint(map, from) };
        foreach (var road in path)
        {
            bool forward = road.From == cursor;
            points.AddRange((forward ? road.Points : road.Points.Reverse()).Skip(1));
            cursor = forward ? road.To : road.From;
        }
        return new(shelter, to, path.Select(r => r.Id).ToArray(), points.ToArray(), distances[to]);
    }
}

public sealed partial class SimulationSession
{
    private readonly Dictionary<string, int> roadClosures = new(StringComparer.Ordinal);
    // Retain expired incidents so replay keeps its dynamic-routing hash contract.
    public IReadOnlyList<RoadClosure> RoadClosures => roadClosures.OrderBy(r => r.Key, StringComparer.Ordinal)
        .Select(r => new RoadClosure(r.Key, r.Value)).ToArray();
    public void CloseRoad(string roadId, int duration)
    {
        if (Scenario.Geography == null) throw new ArgumentException("Road closures require a geographical exercise.");
        if (Status == RunStatus.Completed) throw new InvalidOperationException("Completed runs are immutable.");
        if (duration is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(duration));
        if (!Scenario.Geography.Roads.Any(r => r.Id == roadId)) throw new ArgumentException("Unknown road.");
        roadClosures[roadId] = Math.Max(roadClosures.GetValueOrDefault(roadId), checked(Tick + duration));
    }
    private bool RoadBlocked(RoadRoute route) => route.RoadIds.Any(id => roadClosures.GetValueOrDefault(id) >= Tick && roadClosures.ContainsKey(id));
    private RoadRoute? AvailableRoute(string shelter) => RoadRouting.Find(Scenario.Geography!, shelter,
        roadClosures.Where(r => r.Value >= Tick).Select(r => r.Key));
}
