namespace Aegis.Presentation;

public readonly record struct MapPoint(double X, double Y);
public sealed record MapNode(string Name, double X, double Y, int InitialDemand, int Remaining, bool IsBlocked = false);

// Synthetic planar coordinates in meters, not longitude/latitude or GPS.
public sealed class MapViewport
{
    public const double WorldWidth = 1200;
    public const double WorldHeight = 800;
    public double Zoom { get; private set; } = 1;
    public double PanX { get; private set; }
    public double PanY { get; private set; }
    private double width = 1, height = 1;
    private double Scale => Math.Min(width / WorldWidth, height / WorldHeight) * Zoom;

    public void Resize(double width, double height)
    {
        if (!double.IsFinite(width) || !double.IsFinite(height) || width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        this.width = width;
        this.height = height;
    }

    public MapPoint ToScreen(MapPoint point) => new(
        width / 2 + PanX + (point.X - WorldWidth / 2) * Scale,
        height / 2 + PanY + (point.Y - WorldHeight / 2) * Scale);

    public MapPoint ToWorld(MapPoint point) => new(
        (point.X - width / 2 - PanX) / Scale + WorldWidth / 2,
        (point.Y - height / 2 - PanY) / Scale + WorldHeight / 2);

    public void ZoomAt(double factor, MapPoint anchor)
    {
        if (!double.IsFinite(factor) || factor <= 0 || !double.IsFinite(anchor.X) || !double.IsFinite(anchor.Y))
            throw new ArgumentOutOfRangeException(nameof(factor));
        var worldAnchor = ToWorld(anchor);
        Zoom = Math.Clamp(Zoom * factor, 0.6, 5);
        var shiftedAnchor = ToScreen(worldAnchor);
        Pan(anchor.X - shiftedAnchor.X, anchor.Y - shiftedAnchor.Y);
    }

    public void Pan(double dx, double dy)
    {
        if (!double.IsFinite(dx) || !double.IsFinite(dy)) throw new ArgumentOutOfRangeException(nameof(dx));
        PanX = Math.Clamp(PanX + dx, -width * 5, width * 5);
        PanY = Math.Clamp(PanY + dy, -height * 5, height * 5);
    }

    public void Reset() { Zoom = 1; PanX = 0; PanY = 0; }

    public string? HitTest(IEnumerable<MapNode> nodes, MapPoint screen, double radius = 20)
    {
        return nodes.OrderBy(node => Distance(ToScreen(new(node.X, node.Y)), screen))
            .FirstOrDefault(node => Distance(ToScreen(new(node.X, node.Y)), screen) <= radius)?.Name;
    }

    private static double Distance(MapPoint a, MapPoint b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
