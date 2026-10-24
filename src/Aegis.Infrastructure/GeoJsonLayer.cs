using System.Text;
using System.Text.Json;

namespace Aegis.Infrastructure;

public sealed record LayerPoint(double X, double Y);
public sealed record GeoFeature(string Id, string Name, string Geometry, IReadOnlyList<IReadOnlyList<LayerPoint>> Parts);
public sealed record GeoLayer(IReadOnlyList<GeoFeature> Features, double West, double South, double East, double North)
{
    public string Description => $"WGS84 lon/lat | {Features.Count} features | {West:F5}, {South:F5} to {East:F5}, {North:F5}";
}

// Strict regional GeoJSON subset. Projection belongs to this display adapter, never to simulation rules.
public static class GeoJsonLayer
{
    public static GeoLayer Load(string path)
    {
        if (new FileInfo(path).Length > 524288) throw new InvalidDataException("GeoJSON is limited to 512 KB.");
        return Parse(File.ReadAllText(path));
    }

    public static GeoLayer Parse(string text)
    {
        if (Encoding.UTF8.GetByteCount(text) > 524288) throw new InvalidDataException("GeoJSON is limited to 512 KB.");
        try
        {
            using var json = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 24 });
            var root = json.RootElement;
            UniqueMembers(root);
            if (root.GetProperty("type").GetString() != "FeatureCollection" || root.TryGetProperty("crs", out _))
                throw new InvalidDataException("Use a WGS84 FeatureCollection without a legacy crs member.");
            var features = root.GetProperty("features");
            if (features.ValueKind != JsonValueKind.Array || features.GetArrayLength() is < 1 or > 128)
                throw new InvalidDataException("A layer needs 1..128 features.");
            var parsed = new List<GeoFeature>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            int count = 0;
            LayerPoint Position(JsonElement value)
            {
                if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 2 || ++count > 4096)
                    throw new InvalidDataException("Use two-dimensional positions, at most 4096 per layer.");
                double x = value[0].GetDouble(), y = value[1].GetDouble();
                if (!double.IsFinite(x) || !double.IsFinite(y) || x is < -180 or > 180 || y is < -85 or > 85)
                    throw new InvalidDataException("Coordinates must be longitude [-180,180], latitude [-85,85]. Planar meters are not GeoJSON.");
                return new(x, y);
            }
            foreach (var feature in features.EnumerateArray())
            {
                if (feature.GetProperty("type").GetString() != "Feature") throw new InvalidDataException("Expected a Feature.");
                string id = feature.GetProperty("id").GetString() ?? "";
                string name = feature.GetProperty("properties").GetProperty("name").GetString() ?? "";
                if (string.IsNullOrWhiteSpace(id) || id.Length > 80 || !ids.Add(id) || string.IsNullOrWhiteSpace(name) || name.Length > 120 || name.Any(char.IsControl))
                    throw new InvalidDataException("Every feature needs a unique short string id and a printable name.");
                var geometry = feature.GetProperty("geometry");
                string type = geometry.GetProperty("type").GetString() ?? "";
                var coordinates = geometry.GetProperty("coordinates");
                var parts = new List<IReadOnlyList<LayerPoint>>();
                if (type == "Point") parts.Add(Array.AsReadOnly(new[] { Position(coordinates) }));
                else if (type == "LineString")
                {
                    var line = coordinates.EnumerateArray().Select(Position).ToArray();
                    if (line.Length < 2 || line.Distinct().Count() < 2) throw new InvalidDataException("A line needs two distinct positions.");
                    parts.Add(Array.AsReadOnly(line));
                }
                else if (type == "Polygon")
                {
                    foreach (var ring in coordinates.EnumerateArray())
                    {
                        var points = ring.EnumerateArray().Select(Position).ToArray();
                        ValidateRing(points);
                        parts.Add(Array.AsReadOnly(points));
                    }
                    if (parts.Count != 1) throw new InvalidDataException("This viewer accepts single-ring polygons; holes and multipart geometries are unsupported.");
                }
                else throw new InvalidDataException("Supported geometries: Point, LineString and single-ring Polygon.");
                parsed.Add(new(id, name, type, parts.AsReadOnly()));
            }
            var all = parsed.SelectMany(f => f.Parts.SelectMany(p => p)).ToArray();
            double west = all.Min(p => p.X), east = all.Max(p => p.X), south = all.Min(p => p.Y), north = all.Max(p => p.Y);
            if (east - west > 2 || north - south > 2) throw new InvalidDataException("Use a regional layer spanning at most two degrees; split antimeridian crossings first.");
            double midX = (east + west) / 2, midY = (north + south) / 2;
            double correction = Math.Cos(midY * Math.PI / 180);
            double scale = Math.Min(1040 / Math.Max((east - west) * correction, 0.00001), 640 / Math.Max(north - south, 0.00001));
            LayerPoint Project(LayerPoint p) => new(600 + (p.X - midX) * correction * scale, 400 - (p.Y - midY) * scale);
            return new(parsed.Select(f => f with { Parts = f.Parts.Select(p => (IReadOnlyList<LayerPoint>)Array.AsReadOnly(p.Select(Project).ToArray())).ToList().AsReadOnly() }).ToList().AsReadOnly(), west, south, east, north);
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or OverflowException)
        { throw new InvalidDataException("Invalid GeoJSON structure or coordinate value.", e); }
    }

    private static void UniqueMembers(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate JSON members are ambiguous.");
                UniqueMembers(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array) foreach (var child in value.EnumerateArray()) UniqueMembers(child);
    }

    private static void ValidateRing(LayerPoint[] points)
    {
        if (points.Length is < 4 or > 257 || points[0] != points[^1] || points[..^1].Distinct().Count() != points.Length - 1)
            throw new InvalidDataException("A polygon needs 3..256 distinct vertices and an explicit closing position.");
        double Cross(LayerPoint a, LayerPoint b, LayerPoint c) => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        bool On(LayerPoint a, LayerPoint b, LayerPoint p) => Math.Abs(Cross(a, b, p)) < 1e-12 && p.X >= Math.Min(a.X, b.X) && p.X <= Math.Max(a.X, b.X) && p.Y >= Math.Min(a.Y, b.Y) && p.Y <= Math.Max(a.Y, b.Y);
        for (int i = 0; i < points.Length - 1; i++)
            for (int j = i + 2; j < points.Length - 1; j++)
            {
                if (i == 0 && j == points.Length - 2) continue;
                var a = points[i]; var b = points[i + 1]; var c = points[j]; var d = points[j + 1];
                if (Cross(a, b, c) * Cross(a, b, d) < 0 && Cross(c, d, a) * Cross(c, d, b) < 0 || On(a, b, c) || On(a, b, d) || On(c, d, a) || On(c, d, b))
                    throw new InvalidDataException("Self-intersecting polygons are unsupported.");
            }
        double area = Enumerable.Range(0, points.Length - 1).Sum(i => points[i].X * points[i + 1].Y - points[i + 1].X * points[i].Y);
        if (Math.Abs(area) < 1e-12) throw new InvalidDataException("Polygon area must be positive.");
    }
}
