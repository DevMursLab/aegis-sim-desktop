using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Aegis.Infrastructure;
using Microsoft.Win32;

namespace Aegis.Desktop.Wpf;

public sealed class LayerWindow : Window
{
    private readonly SyntheticMap map = new();
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.DarkSlateGray, Margin = new(0, 8, 0, 12) };
    private readonly ListBox features = new() { DisplayMemberPath = "Name", MinWidth = 230 };
    public LayerWindow()
    {
        NameScope.SetNameScope(this, new NameScope());
        Title = "Offline GIS / GeoJSON inspector"; Width = 1160; Height = 780; MinWidth = 900; MinHeight = 600;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        RegisterName("GeoMap", map); RegisterName("FeatureList", features);
        var panel = new DockPanel { Margin = new(22) };
        var top = new StackPanel(); DockPanel.SetDock(top, Dock.Top);
        top.Children.Add(new TextBlock { Text = "OFFLINE VECTOR LAYERS", FontSize = 24, FontWeight = FontWeights.SemiBold });
        var actions = new WrapPanel { Margin = new(0, 12, 0, 0) };
        var open = new Button { Content = "Open GeoJSON", Padding = new(14, 8, 14, 8) };
        open.Click += (_, _) => { var picker = new OpenFileDialog { Filter = "GeoJSON FeatureCollection|*.geojson;*.json" }; if (picker.ShowDialog(this) == true) TryLoad(picker.FileName); };
        actions.Children.Add(open);
        var fit = new Button { Content = "Fit map", Padding = new(14, 8, 14, 8), Margin = new(8, 0, 12, 0) }; fit.Click += (_, _) => map.Fit(); actions.Children.Add(fit);
        foreach (string type in new[] { "Points", "Lines", "Areas" })
        {
            var check = new CheckBox { Content = type, IsChecked = true, VerticalAlignment = VerticalAlignment.Center, Margin = new(8, 0, 8, 0) };
            check.Click += (_, _) => { if (type == "Points") map.ShowPoints = check.IsChecked == true; else if (type == "Lines") map.ShowLines = check.IsChecked == true; else map.ShowAreas = check.IsChecked == true; map.InvalidateVisual(); };
            actions.Children.Add(check);
        }
        top.Children.Add(actions); top.Children.Add(status); panel.Children.Add(top);
        var side = new DockPanel { Width = 260, Margin = new(16, 0, 0, 0) }; DockPanel.SetDock(side, Dock.Right);
        var detail = new TextBlock { Text = "Select a feature to inspect it.\nPan: drag. Zoom: wheel. Home: fit.\n\nImported layers are view-only and are not saved in exercise history.", TextWrapping = TextWrapping.Wrap, Margin = new(0, 10, 0, 0) };
        DockPanel.SetDock(detail, Dock.Bottom); side.Children.Add(detail); side.Children.Add(features); panel.Children.Add(side); panel.Children.Add(map); Content = panel;
        features.SelectionChanged += (_, _) =>
        {
            if (features.SelectedItem is not GeoFeature feature) return;
            map.SelectedFeatureId = feature.Id; map.InvalidateVisual();
            detail.Text = $"{feature.Name}\nID: {feature.Id}\n{feature.Geometry} / {feature.Parts.Sum(p => p.Count)} positions\n\nWGS84 source; north points up. Regional equirectangular display, not a distance measurement tool.\n\nView-only: simulation coordinates and routing are unchanged.";
        };
        status.Text = "Open a regional WGS84 layer. Point, LineString and simple Polygon; up to 128 features / 4096 positions / 512 KB.";
    }
    public bool TryLoad(string path)
    {
        try
        {
            var layer = GeoJsonLayer.Load(path);
            map.Layer = layer; map.SelectedFeatureId = null; features.ItemsSource = layer.Features;
            map.Fit(); status.Text = layer.Description; return true;
        }
        catch (Exception e) { status.Text = "Layer rejected; previous layer preserved. " + e.Message; return false; }
    }
}
