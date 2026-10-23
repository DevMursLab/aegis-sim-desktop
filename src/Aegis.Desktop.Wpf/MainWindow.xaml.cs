using System.Windows;

namespace Aegis.Desktop.Wpf;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();
    private void ZoomIn_Click(object sender, RoutedEventArgs e) => AreaMap.Zoom(1.2);
    private void ZoomOut_Click(object sender, RoutedEventArgs e) => AreaMap.Zoom(1 / 1.2);
    private void FitMap_Click(object sender, RoutedEventArgs e) => AreaMap.Fit();
}
