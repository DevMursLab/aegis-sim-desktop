using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Windows;
using Aegis.Desktop.Wpf;
using Aegis.Presentation;
using Aegis.Simulation;

internal static class NetworkWindowSmoke
{
    public static async Task Run(Action<MainWindow, string> screenshot)
    {
        string root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "Aegis.Sim.slnx"))) root = Directory.GetParent(root)?.FullName ?? throw new Exception("Repository not found.");
        string directory = Path.Combine(Path.GetTempPath(), "aegis-network-ui-" + Guid.NewGuid().ToString("N"));
        var socket = new TcpListener(IPAddress.Loopback, 0); socket.Start(); int port = ((IPEndPoint)socket.LocalEndpoint).Port; socket.Stop();
        string url = $"http://127.0.0.1:{port}";
        Process StartHost()
        {
            var info = new ProcessStartInfo(Path.Combine(root, "src", "Aegis.SyncHost", "bin", "Release", "net10.0", "Aegis.SyncHost.exe")) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string arg in new[] { "--urls", url, "--data-dir", directory }) info.ArgumentList.Add(arg);
            var process = Process.Start(info)!; process.OutputDataReceived += (_, _) => { }; process.ErrorDataReceived += (_, _) => { }; process.BeginOutputReadLine(); process.BeginErrorReadLine(); return process;
        }
        using var http = new HttpClient();
        async Task Healthy() => await Until(async () => { try { return (await http.GetAsync(url + "/health")).IsSuccessStatusCode; } catch { return false; } });
        using var host = StartHost();
        var controller = new RunViewModel(SimulationSession.CreateDemo()); var observer = new RunViewModel(SimulationSession.CreateDemo());
        var first = new MainWindow { DataContext = controller, Title = "AEGIS-SIM / Controller" };
        var second = new MainWindow { DataContext = observer, Title = "AEGIS-SIM / Observer (read only)" };
        try
        {
            await Healthy();
            first.Show(); second.Show();
            string secrets = Path.Combine(directory, "session-secrets.json");
            await using var controllerClient = new NetworkClient(url, secrets, false, controller, Application.Current.Dispatcher);
            await using var observerClient = new NetworkClient(url, secrets, true, observer, Application.Current.Dispatcher);
            await controllerClient.Connect(); await observerClient.Connect();
            controller.SelectedShelterName = "Shelter-A"; observer.SelectedShelterName = "Shelter-A";
            controller.CloseRouteCommand.Execute(null);
            await Until(() => Task.FromResult(controller.Document.Commands.Length == 1 && observer.Document.Commands.Length == 1 && controller.CloseRouteCommand.CanExecute(null)));
            controller.StepCommand.Execute(null);
            await Until(() => Task.FromResult(controller.Tick == 1 && observer.Tick == 1));
            Require(!observer.StepCommand.CanExecute(null) && !observer.ResetCommand.CanExecute(null), "Observer UI must stay read-only.");
            Require(observer.MapNodes[0].IsBlocked && observer.Shelters[1].Remaining == 0, "Observer map and table must match the controller incident.");
            host.Kill(true); await host.WaitForExitAsync();
            await Until(() => Task.FromResult(controller.ConnectionLabel.Contains("STALE") || controller.ConnectionLabel.Contains("DISCONNECTED")));
            Require(!controller.StepCommand.CanExecute(null), "Disconnected controller must not write.");
            using var restarted = StartHost();
            try
            {
                await Healthy();
                await Until(() => Task.FromResult(controller.ConnectionLabel.Contains("HOST") && observer.ConnectionLabel.Contains("HOST")));
                controller.StepCommand.Execute(null);
                await Until(() => Task.FromResult(controller.Tick == 2 && observer.Tick == 2));
                Require(controller.Document.Id == observer.Document.Id && controller.Document.Commands.SequenceEqual(observer.Document.Commands), "Clients must recover the same persisted run.");
                first.UpdateLayout(); second.UpdateLayout();
                screenshot(first, "aegis-controller.png"); screenshot(second, "aegis-observer.png");
                Console.WriteLine("PASS: Two actual WPF windows synchronize incidents, enforce observer read-only, show stale state, and automatically reconnect after host restart.");
            }
            finally { if (!restarted.HasExited) restarted.Kill(true); }
        }
        finally { first.Close(); second.Close(); if (!host.HasExited) host.Kill(true); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task Until(Func<Task<bool>> condition)
    {
        var watch = Stopwatch.StartNew();
        while (!await condition()) { if (watch.Elapsed > TimeSpan.FromSeconds(40)) throw new TimeoutException("Network UI check timed out."); await Task.Delay(50); }
    }
}
