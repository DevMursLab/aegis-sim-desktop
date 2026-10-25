namespace Aegis.Simulation;

public sealed record RoadPoint(double X, double Y);
public sealed record RoadDefinition(string Id, string From, string To, int Meters, RoadPoint[] Points);
public sealed record RoadRoute(string Shelter, string TargetNode, string[] RoadIds, RoadPoint[] Points, int Meters);
public sealed record GeographyDefinition(string DepotId, RoadPoint Depot, int MetersPerTick,
    RoadDefinition[] Roads, RoadRoute[] Routes, string SourceHash)
{
    public GeographyDefinition Snapshot() => this with
    {
        Roads = Roads.Select(r => r with { Points = r.Points.ToArray() }).ToArray(),
        Routes = Routes.Select(r => r with { RoadIds = r.RoadIds.ToArray(), Points = r.Points.ToArray() }).ToArray()
    };

    public void Validate(ShelterDefinition[] shelters)
    {
        if (string.IsNullOrWhiteSpace(DepotId) || MetersPerTick is < 1 or > 100000 || Roads == null || Roads.Length is < 1 or > 128 ||
            Routes == null || Routes.Length != shelters.Length || SourceHash == null || SourceHash.Length != 64 || !SourceHash.All(Uri.IsHexDigit))
            throw new ArgumentException("Invalid geographical network metadata.");
        bool Valid(RoadPoint? p) => p != null && double.IsFinite(p.X) && double.IsFinite(p.Y) && p.X is >= 50 and <= 1050 && p.Y is >= 70 and <= 700;
        if (!Valid(Depot)) throw new ArgumentException("Depot is outside the display plane.");
        var roads = new Dictionary<string, RoadDefinition>(StringComparer.Ordinal);
        var nodes = new Dictionary<string, RoadPoint>(StringComparer.Ordinal) { [DepotId] = Depot };
        int vertices = 0;
        foreach (var road in Roads)
        {
            if (road == null || string.IsNullOrWhiteSpace(road.Id) || road.Id.Length > 80 || !roads.TryAdd(road.Id, road) ||
                string.IsNullOrWhiteSpace(road.From) || string.IsNullOrWhiteSpace(road.To) || road.From == road.To || road.Meters is < 1 or > 1000000 ||
                road.Points == null || road.Points.Length < 2 || road.Points.Any(p => !Valid(p)) || (vertices += road.Points.Length) > 4096)
                throw new ArgumentException("Invalid road geometry, ID or length.");
            foreach (var endpoint in new[] { (road.From, road.Points[0]), (road.To, road.Points[^1]) })
            {
                if (nodes.TryGetValue(endpoint.Item1, out var known) && known != endpoint.Item2) throw new ArgumentException("Road endpoints disagree.");
                nodes[endpoint.Item1] = endpoint.Item2;
            }
        }
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var route in Routes)
        {
            if (route == null || !names.Add(route.Shelter) || route.RoadIds == null || route.RoadIds.Length is < 1 or > 128 ||
                route.RoadIds.Distinct(StringComparer.Ordinal).Count() != route.RoadIds.Length || route.Points == null || route.Points.Length is < 2 or > 4096)
                throw new ArgumentException("Invalid or duplicate shelter route.");
            var shelter = shelters.SingleOrDefault(s => s.Name == route.Shelter) ?? throw new ArgumentException("Unknown route shelter.");
            string current = DepotId; int distance = 0; var points = new List<RoadPoint> { Depot };
            foreach (string id in route.RoadIds)
            {
                if (!roads.TryGetValue(id, out var road) || road.From != current && road.To != current) throw new ArgumentException("Route is disconnected.");
                bool forward = road.From == current; current = forward ? road.To : road.From;
                points.AddRange((forward ? road.Points : road.Points.Reverse()).Skip(1)); distance = checked(distance + road.Meters);
            }
            if (current != route.TargetNode || points[^1] != new RoadPoint(shelter.X, shelter.Y) || !points.SequenceEqual(route.Points) ||
                distance != route.Meters || shelter.TravelTicks != (distance + MetersPerTick - 1) / MetersPerTick)
                throw new ArgumentException("Route distance, endpoint, geometry or travel ticks do not match.");
        }
    }
}
