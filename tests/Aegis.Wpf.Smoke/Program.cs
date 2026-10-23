using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Aegis.Desktop.Wpf;
using Aegis.Presentation;

internal static class Program
{
    [STAThread]
    public static int Main()
    {
        var errors = new BindingErrors();
        PresentationTraceSources.DataBindingSource.Listeners.Add(errors);
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        var app = new App();
        app.DispatcherUnhandledException += (_, e) => { Console.Error.WriteLine(e.Exception); e.Handled = true; app.Shutdown(1); };
        app.Startup += (_, _) => app.Dispatcher.BeginInvoke(new Action(async () =>
        {
            try
            {
                var window = app.Windows.OfType<MainWindow>().Single();
                var vm = (RunViewModel)window.DataContext;
                await Flush();
                Require(Text(window, "StatusText") == "Ready", "Initial status binding");
                Invoke(window, "StepButton"); await Flush();
                Require(Text(window, "TickText") == "1" && vm.Events.Count == 1, "Step button and tick binding");
                Require(((DataGrid)window.FindName("ShelterGrid")).Items.Count == 3, "Shelter rows");
                Invoke(window, "StartButton"); await Flush();
                Require(!Button(window, "StepButton").IsEnabled && Button(window, "PauseButton").IsEnabled, "Running button states");
                await WaitUntil(() => vm.Tick >= 2);
                Invoke(window, "PauseButton"); await Flush();
                int pausedTick = vm.Tick; await Task.Delay(1250);
                Require(vm.Tick == pausedTick && Text(window, "StatusText") == "Paused", "Real timer stops on pause");
                Invoke(window, "StartButton");
                await WaitUntil(() => vm.Status == "Completed"); await Flush();
                Require(Text(window, "TickText") == "6" && vm.DeliveredTotal == 48, "Timer completes run");
                Require(!Button(window, "StartButton").IsEnabled && !Button(window, "StepButton").IsEnabled, "Completed buttons");
                Require(((DataGrid)window.FindName("EventGrid")).Items.Count == 6, "History binding");
                var content = (FrameworkElement)window.Content;
                int width = (int)Math.Ceiling(content.ActualWidth + content.Margin.Left + content.Margin.Right);
                int height = (int)Math.Ceiling(content.ActualHeight + content.Margin.Top + content.Margin.Bottom);
                var image = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                image.Render(window);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
                string path = Path.Combine(Path.GetTempPath(), "aegis-wpf-smoke.png");
                using (var file = File.Create(path)) encoder.Save(file);
                Console.WriteLine($"SCREENSHOT: {path}");
                Invoke(window, "ResetButton"); await Flush();
                Require(vm.Tick == 0 && vm.Remaining == 48 && vm.Events.Count == 0 && Text(window, "StatusText") == "Ready", "Reset bindings");
                Invoke(window, "StartButton"); window.Close();
                Require(app.Windows.Count == 0, "Window closes while running");
                Require(errors.Messages.Count == 0, string.Join(Environment.NewLine, errors.Messages));
                Console.WriteLine("PASS: WPF buttons, bindings, timer, pause/resume, completion, reset and close. No binding errors.");
                app.Shutdown(0);
            }
            catch (Exception exception) { Console.Error.WriteLine(exception); app.Shutdown(1); }
        }));
        return app.Run();
    }
    static Button Button(MainWindow window, string name) => (Button)window.FindName(name);
    static string Text(MainWindow window, string name) => ((TextBlock)window.FindName(name)).Text;
    static void Invoke(MainWindow window, string name)
    {
        var button = Button(window, name); Require(button.IsEnabled, name + " must be enabled");
        ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
    }
    static async Task Flush() => await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    static async Task WaitUntil(Func<bool> condition)
    {
        var watch = Stopwatch.StartNew();
        while (!condition()) { if (watch.Elapsed > TimeSpan.FromSeconds(12)) throw new TimeoutException(); await Task.Delay(30); }
    }
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private sealed class BindingErrors : TraceListener
    {
        public List<string> Messages { get; } = new();
        public override void Write(string? message) { if (!string.IsNullOrEmpty(message)) Messages.Add(message); }
        public override void WriteLine(string? message) => Write(message);
    }
}
