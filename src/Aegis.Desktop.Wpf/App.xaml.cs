using System.IO;
using System.Windows;
using System.Windows.Threading;
using Aegis.Infrastructure;
using Aegis.Presentation;
using Aegis.Simulation;

namespace Aegis.Desktop.Wpf;

public partial class App : Application
{
    private DispatcherTimer? timer;
    private NetworkClient? network;
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            string directory = Option(e.Args, "--data-dir") ?? Environment.GetEnvironmentVariable("AEGIS_DATA_DIR") ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AEGIS-SIM", "local");
            var repository = await Task.Run(() => new RunRepository(Path.Combine(directory, "runs.db")));
            var model = new RunViewModel(SimulationSession.CreateDemo()) { BackgroundReplay = true };
            string? server = Option(e.Args, "--server");
            if (server == null)
            {
                model.Persist = repository.Save;
                var latest = await Task.Run(() => repository.List().FirstOrDefault());
                if (latest == null) await Task.Run(() => repository.Save(model.Document));
                else await model.LoadReplayAsync(await Task.Run(() => repository.Load(latest.Id)));
                if (repository.UpgradeBackupPath != null) model.Notify("Database upgraded. A safety backup was created beside runs.db; saved history is preserved.");
            }
            var window = new MainWindow { DataContext = model };
            var workspace = new DesktopWorkspace(window, model, repository);
            window.WorkspaceAction += workspace.Execute;
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (_, _) => model.Advance();
            model.PropertyChanged += (_, _) => { if (model.IsRunning) timer.Start(); else timer.Stop(); };
            window.Closed += (_, _) => { timer.Stop(); model.CancelReplayWork(); };
            if (server != null)
                network = new NetworkClient(server, Option(e.Args, "--secrets") ?? throw new ArgumentException("Specify --secrets <host session-secrets.json>."),
                    Option(e.Args, "--role") != "controller", model, Dispatcher);
            window.Show();
            if (e.Args.Contains("--verify-startup"))
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    window.UpdateLayout();
                    if (window.FindName("StatusText") is not System.Windows.Controls.TextBlock status || string.IsNullOrWhiteSpace(status.Text))
                        throw new InvalidOperationException("Startup binding failed.");
                    window.Close();
                }, DispatcherPriority.ApplicationIdle);
                return;
            }
            if (network != null)
            {
                try { await network.Connect(); }
                catch (Exception error) { model.SetConnection(false, "DISCONNECTED"); model.Notify(error.Message); }
            }
        }
        catch (Exception error)
        {
            MessageBox.Show("AEGIS-SIM could not start: " + error.Message, "Startup error"); Shutdown(1);
        }
    }
    private static string? Option(string[] args, string key)
    {
        int index = Array.IndexOf(args, key);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
    protected override void OnExit(ExitEventArgs e)
    {
        timer?.Stop();
        if (network != null) network.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.OnExit(e);
    }
}
