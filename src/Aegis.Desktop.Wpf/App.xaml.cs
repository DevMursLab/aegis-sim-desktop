using System.Windows;
using System.Windows.Threading;
using Aegis.Presentation;
using Aegis.Simulation;

namespace Aegis.Desktop.Wpf;

public partial class App : Application
{
    private DispatcherTimer? timer;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var viewModel = new RunViewModel(SimulationSession.CreateDemo());
        var window = new MainWindow { DataContext = viewModel };
        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => viewModel.Advance();
        viewModel.PropertyChanged += (_, _) =>
        {
            if (viewModel.IsRunning) timer.Start();
            else timer.Stop();
        };
        window.Closed += (_, _) => timer.Stop();
        window.Show();
    }
    protected override void OnExit(ExitEventArgs e)
    {
        timer?.Stop();
        base.OnExit(e);
    }
}
