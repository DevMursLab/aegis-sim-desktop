using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Aegis.Presentation;

namespace Aegis.Desktop.Wpf;

// Drawing, viewport gestures and hit-testing only; no simulation state mutation.
public sealed class SyntheticMap : FrameworkElement
{
    public static readonly DependencyProperty NodesProperty = DependencyProperty.Register(nameof(Nodes), typeof(IReadOnlyList<MapNode>), typeof(SyntheticMap), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty SelectedNameProperty = DependencyProperty.Register(nameof(SelectedName), typeof(string), typeof(SyntheticMap), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty LastDeliveryProperty = DependencyProperty.Register(nameof(LastDelivery), typeof(string), typeof(SyntheticMap), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public IReadOnlyList<MapNode>? Nodes { get => (IReadOnlyList<MapNode>?)GetValue(NodesProperty); set => SetValue(NodesProperty, value); }
    public string? SelectedName { get => (string?)GetValue(SelectedNameProperty); set => SetValue(SelectedNameProperty, value); }
    public string? LastDelivery { get => (string?)GetValue(LastDeliveryProperty); set => SetValue(LastDeliveryProperty, value); }
    public MapViewport Viewport { get; } = new();
    private Point dragStart, previous;
    private bool dragging;

    public SyntheticMap()
    {
        ClipToBounds = true;
        Focusable = true;
        Cursor = Cursors.Hand;
        ToolTip = "Select a shelter · Mouse wheel: zoom · Drag: pan · Home: fit map";
        SizeChanged += (_, _) => { if (ActualWidth > 0 && ActualHeight > 0) Viewport.Resize(ActualWidth, ActualHeight); InvalidateVisual(); };
    }

    public void Fit() { Viewport.Reset(); InvalidateVisual(); }
    public void Zoom(double factor) { Viewport.ZoomAt(factor, new(ActualWidth / 2, ActualHeight / 2)); InvalidateVisual(); }
    public void SelectAt(MapPoint position)
    {
        SetCurrentValue(SelectedNameProperty, Viewport.HitTest(Nodes ?? Array.Empty<MapNode>(), position));
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(Brush("#ECF3F2"), null, new Rect(0, 0, ActualWidth, ActualHeight));
        for (int x = 0; x <= 1200; x += 100) Line(dc, new(x, 0), new(x, 800), "#D8E5E2", 1);
        for (int y = 0; y <= 800; y += 100) Line(dc, new(0, y), new(1200, y), "#D8E5E2", 1);
        // Fictional river and conceptual links, not a routing network.
        Line(dc, new(780, 0), new(690, 250), "#C2E3E9", 42);
        Line(dc, new(690, 250), new(780, 540), "#C2E3E9", 42);
        Line(dc, new(780, 540), new(680, 800), "#C2E3E9", 42);
        var depot = new MapPoint(170, 410);
        foreach (var node in Nodes ?? Array.Empty<MapNode>())
        {
            var world = new MapPoint(node.X, node.Y);
            Line(dc, depot, world, "#FCFEFD", 10);
            Line(dc, depot, world, node.IsBlocked ? "#BC544B" : node.Name == LastDelivery ? "#D59935" : "#9BB8AE", 3);
            Point center = Screen(world);
            if (node.Name == SelectedName) dc.DrawEllipse(null, new Pen(Brush("#1B5E73"), 3), center, 22, 22);
            dc.DrawEllipse(Brush(node.IsBlocked ? "#BC544B" : node.Remaining == 0 ? "#277B65" : "#173C4F"), new Pen(Brush("#FFFFFF"), 3), center, 14, 14);
            Label(dc, node.Name, center.X + 25, center.Y - 22, "#173C4F", 14, true);
            Label(dc, node.IsBlocked ? $"CLOSED · {node.Remaining} left" : $"{node.Remaining} packets left", center.X + 25, center.Y + 1, "#526878", 12);
            if (node.Name == LastDelivery)
            {
                dc.DrawRoundedRectangle(Brush("#E8AE48"), new Pen(Brush("#FFFFFF"), 2), new Rect(center.X - 38, center.Y - 8, 18, 16), 3, 3);
            }
        }
        var origin = Screen(depot);
        dc.DrawRoundedRectangle(Brush("#173C4F"), new Pen(Brush("#FFFFFF"), 3), new Rect(origin.X - 17, origin.Y - 17, 34, 34), 5, 5);
        Label(dc, "DEPOT", origin.X - 25, origin.Y + 26, "#173C4F", 13, true);
        Label(dc, "Synthetic local grid · meters", 16, 14, "#526878", 12);
        Label(dc, $"Zoom {Viewport.Zoom:0.0}×", 16, ActualHeight - 28, "#526878", 12);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        var p = e.GetPosition(this);
        Viewport.ZoomAt(e.Delta > 0 ? 1.2 : 1 / 1.2, new(p.X, p.Y));
        InvalidateVisual(); e.Handled = true;
    }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        Focus(); dragStart = previous = e.GetPosition(this); dragging = true; CaptureMouse(); e.Handled = true;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!dragging) return;
        var next = e.GetPosition(this);
        Viewport.Pan(next.X - previous.X, next.Y - previous.Y); previous = next; InvalidateVisual();
    }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (!dragging) return;
        var next = e.GetPosition(this); dragging = false; ReleaseMouseCapture();
        if ((next - dragStart).Length < 4) SelectAt(new(next.X, next.Y));
        e.Handled = true;
    }
    protected override void OnLostMouseCapture(MouseEventArgs e) { dragging = false; base.OnLostMouseCapture(e); }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Home: Fit(); break;
            case Key.Add: case Key.OemPlus: Zoom(1.2); break;
            case Key.Subtract: case Key.OemMinus: Zoom(1 / 1.2); break;
            case Key.Left: Viewport.Pan(30, 0); break;
            case Key.Right: Viewport.Pan(-30, 0); break;
            case Key.Up: Viewport.Pan(0, 30); break;
            case Key.Down: Viewport.Pan(0, -30); break;
            default: base.OnKeyDown(e); return;
        }
        InvalidateVisual(); e.Handled = true;
    }
    private Point Screen(MapPoint p) { var s = Viewport.ToScreen(p); return new(s.X, s.Y); }
    private void Line(DrawingContext dc, MapPoint a, MapPoint b, string color, double width) => dc.DrawLine(new Pen(Brush(color), width), Screen(a), Screen(b));
    private static SolidColorBrush Brush(string color) => new((Color)ColorConverter.ConvertFromString(color));
    private void Label(DrawingContext dc, string text, double x, double y, string color, double size, bool bold = false)
    {
        var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, bold ? FontWeights.SemiBold : FontWeights.Normal, FontStretches.Normal),
            size, Brush(color), VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(formatted, new Point(x, y));
    }
}
