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
        Environment.SetEnvironmentVariable("AEGIS_DATA_DIR", Path.Combine(Path.GetTempPath(), "aegis-smoke-" + Guid.NewGuid().ToString("N")));
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
                var map = (SyntheticMap)window.FindName("AreaMap");
                var firstNode = vm.MapNodes[0];
                map.SelectAt(map.Viewport.ToScreen(new(firstNode.X, firstNode.Y))); await Flush();
                Require(vm.SelectedShelterName == "Shelter-A" && Text(window, "SelectedDetailText").Contains("25 remaining"), "Map selection binding");
                Invoke(window, "ZoomInButton"); await Flush();
                Require(map.Viewport.Zoom > 1, "Map zoom button");
                map.Viewport.Pan(20, -10); map.InvalidateVisual();
                Invoke(window, "FitMapButton"); await Flush();
                Require(map.Viewport.Zoom == 1 && map.Viewport.PanX == 0, "Map fit button");
                ((ComboBox)window.FindName("ShelterPicker")).SelectedItem = "Shelter-B"; await Flush();
                Require(map.SelectedName == "Shelter-B" && vm.SelectedShelterName == "Shelter-B", "Keyboard selection alternative");
                map.SelectAt(map.Viewport.ToScreen(new(firstNode.X, firstNode.Y))); await Flush();
                Invoke(window, "StepButton"); await Flush();
                Require(Text(window, "TickText") == "1" && vm.Events.Count == 1, "Step button and tick binding");
                Require(map.Nodes![0].Remaining == 15 && Text(window, "SelectedDetailText").Contains("15 remaining"), "Map and inspector update after delivery");
                Require(((DataGrid)window.FindName("ShelterGrid")).Items.Count == 3, "Shelter rows");
                Invoke(window, "StartButton"); await Flush();
                Require(!Button(window, "StepButton").IsEnabled && Button(window, "PauseButton").IsEnabled, "Running button states");
                await WaitUntil(() => vm.Tick >= 2);
                Invoke(window, "PauseButton"); await Flush();
                int pausedTick = vm.Tick; await Task.Delay(1250);
                Require(vm.Tick == pausedTick && Text(window, "StatusText") == "Paused", "Real timer stops on pause");
                SaveImage(window, "aegis-map-preview.png");
                Invoke(window, "StartButton");
                await WaitUntil(() => vm.Status == "Completed"); await Flush();
                Require(Text(window, "TickText") == "6" && vm.DeliveredTotal == 48, "Timer completes run");
                Require(!Button(window, "StartButton").IsEnabled && !Button(window, "StepButton").IsEnabled, "Completed buttons");
                Require(((DataGrid)window.FindName("EventGrid")).Items.Count == 6, "History binding");
                Require(map.Nodes!.All(node => node.Remaining == 0) && vm.SelectedShelterName == "Shelter-A", "Map completion and preserved selection");
                SaveImage(window, "aegis-wpf-smoke.png");
                window.Width = window.MinWidth; window.Height = window.MinHeight; await Flush();
                Require(map.ActualWidth >= 500 && map.ActualHeight >= 250, "Minimum window map area");
                SaveImage(window, "aegis-map-minimum.png");
                Invoke(window, "ResetButton"); await Flush();
                Require(vm.Tick == 0 && vm.Remaining == 48 && vm.Events.Count == 0 && map.LastDelivery == null && map.Nodes![0].Remaining == 25 && Text(window, "StatusText") == "Ready", "Reset bindings");
                Invoke(window, "StepButton"); await Flush();
                var completedSource = vm.Document;
                vm.LoadReplay(completedSource); vm.ReplayTick = 0; await Flush();
                Require(vm.Tick == 0 && !Button(window, "StepButton").IsEnabled, "Read-only replay at tick zero");
                vm.ReplayTick = 1; await Flush(); Require(vm.Tick == 1, "Replay slider updates rendered state");
                Invoke(window, "ResetButton"); await Flush();
                window.Width = 1320; window.Height = 1040; await Flush();
                Invoke(window, "LogisticsDemoButton"); await Flush();
                Require(vm.UsesLogistics && map.HasVehicle, "Logistics demo opens in actual window");
                Invoke(window, "StepButton"); await Flush();
                Invoke(window, "StepButton"); await Flush();
                Require(vm.DeliveredTotal == 0 && map.VehicleX > 170, "Cargo moves before delivery");
                SaveImage(window, "aegis-logistics-transit.png");
                while (vm.Status != "SupplyExhausted" && vm.Tick < 30) { Invoke(window, "StepButton"); await Flush(); }
                Require(vm.Tick == 16 && vm.DeliveredTotal == 30 && Button(window, "RestockButton").IsEnabled && !Button(window, "StepButton").IsEnabled, "Shortage controls and inventory");
                SaveImage(window, "aegis-logistics-shortage.png");
                var shortageSource = vm.Document;
                Invoke(window, "RestockButton"); await Flush();
                while (vm.Status != "Completed" && vm.Tick < 40) { Invoke(window, "StepButton"); await Flush(); }
                Require(vm.Tick == 24 && vm.DeliveredTotal == 48, "Restock completes finite supply exercise");
                SaveImage(window, "aegis-logistics-completed.png");
                vm.LoadReplay(shortageSource); vm.ReplayTick = 2; await Flush();
                Require(vm.DeliveredTotal == 0 && !Button(window, "RestockButton").IsEnabled, "Logistics replay is read-only");
                vm.NewScenario(Aegis.Simulation.ScenarioDefinition.FloodDemo()); await Flush();
                int commandsBeforePreview = vm.Document.Commands.Length;
                ((IInvokeProvider)new MenuItemAutomationPeer((MenuItem)window.FindName("PreviewMenu")).GetPattern(PatternInterface.Invoke)).Invoke(); await Flush();
                var preview = app.Windows.OfType<PreviewWindow>().Single();
                await WaitUntil(() => ((TextBlock)preview.FindName("ProjectionText")).Text.Contains("Outcome:"));
                Require(((TextBlock)preview.FindName("ProjectionText")).Text.Contains("Projected tick: 6") && vm.Document.Commands.Length == commandsBeforePreview, "Background preview leaves live history unchanged");
                SaveImage(preview, "aegis-completion-preview.png");
                preview.Close();
                ((IInvokeProvider)new MenuItemAutomationPeer((MenuItem)window.FindName("LayersMenu")).GetPattern(PatternInterface.Invoke)).Invoke(); await Flush();
                var layers = app.Windows.OfType<LayerWindow>().Single();
                string sourceRoot = AppContext.BaseDirectory;
                while (!File.Exists(Path.Combine(sourceRoot, "Aegis.Sim.slnx"))) sourceRoot = Directory.GetParent(sourceRoot)?.FullName ?? throw new Exception("Repository not found");
                Require(layers.TryLoad(Path.Combine(sourceRoot, "assets", "maps", "synthetic-region.geojson")), "Offline GeoJSON load"); await Flush();
                var geoMap = (SyntheticMap)layers.FindName("GeoMap");
                ((ListBox)layers.FindName("FeatureList")).SelectedIndex = 4; await Flush();
                Require(geoMap.Layer!.Features.Count == 7 && geoMap.SelectedFeatureId == "depot-01", "GIS feature inspection selects the right vector feature");
                SaveImage(layers, "aegis-geojson-viewer.png");
                var goodLayer = geoMap.Layer;
                Require(!layers.TryLoad(Path.Combine(sourceRoot, "assets", "scenarios", "finite-stock-travel.json")) && ReferenceEquals(geoMap.Layer, goodLayer), "Failed map import preserves loaded layer");
                geoMap.Zoom(1.2); geoMap.Fit(); layers.Close();
                var beforeEditing = vm.Document;
                ((IInvokeProvider)new MenuItemAutomationPeer((MenuItem)window.FindName("EditorMenu")).GetPattern(PatternInterface.Invoke)).Invoke(); await Flush();
                var editor = app.Windows.OfType<ScenarioEditorWindow>().Single();
                Require(!((TextBox)editor.FindName("DepotStockInput")).IsEnabled, "Legacy editor disables stock fields");
                ((IToggleProvider)new CheckBoxAutomationPeer((CheckBox)editor.FindName("LogisticsMode")).GetPattern(PatternInterface.Toggle)).Toggle(); await Flush();
                Require(editor.Draft.UsesLogistics && ((TextBox)editor.FindName("DepotStockInput")).IsEnabled, "Mode toggle enables stock inputs through binding");
                ((TextBox)editor.FindName("CapacityInput")).Text = "bad";
                InvokeButton(editor, "StartScenarioButton"); await Flush();
                Require(editor.IsVisible && vm.Document.Id == beforeEditing.Id && ((TextBlock)editor.FindName("EditorStatus")).Text.Contains("Capacity"), "Invalid editor input cannot create a run");
                ((TextBox)editor.FindName("CapacityInput")).Text = "12";
                ((TextBox)editor.FindName("NameInput")).Text = "Edited training exercise";
                InvokeButton(editor, "AddShelterButton"); await Flush();
                Require(editor.Draft.Shelters.Count == 4, "Editor add row");
                ((DataGrid)editor.FindName("ShelterEditorGrid")).SelectedItem = editor.Draft.Shelters[^1];
                InvokeButton(editor, "RemoveShelterButton"); await Flush(); Require(editor.Draft.Shelters.Count == 3, "Editor remove selected row");
                string editedPath = Path.Combine(Path.GetTempPath(), "aegis-ui-edited-" + Guid.NewGuid().ToString("N") + ".xml");
                Require(editor.SaveTo(editedPath) && Aegis.Infrastructure.ScenarioFiles.Load(editedPath).Capacity == 12, "Editor saves validated XML");
                InvokeButton(editor, "ValidateScenarioButton"); await Flush();
                SaveImage(editor, "aegis-scenario-editor.png");
                InvokeButton(editor, "StartScenarioButton"); await Flush();
                Require(!editor.IsVisible && vm.Document.Id != beforeEditing.Id && vm.Tick == 0 && vm.Document.Scenario.Capacity == 12 && beforeEditing.Scenario.Capacity == 10, "Start edited scenario creates a separate saved run");
                var createdId = vm.Document.Id;
                ((IInvokeProvider)new MenuItemAutomationPeer((MenuItem)window.FindName("EditorMenu")).GetPattern(PatternInterface.Invoke)).Invoke(); await Flush();
                var cancelledEditor = app.Windows.OfType<ScenarioEditorWindow>().Single();
                ((TextBox)cancelledEditor.FindName("CapacityInput")).Text = "99";
                InvokeButton(cancelledEditor, "CloseEditorButton"); await Flush();
                Require(vm.Document.Id == createdId && vm.Document.Scenario.Capacity == 12, "Closing draft discards unapplied changes");
                var failedEditor = new ScenarioEditorWindow(vm.Document.Scenario, _ => "simulated storage failure") { Owner = window }; failedEditor.Show(); await Flush();
                InvokeButton(failedEditor, "StartScenarioButton"); await Flush();
                Require(failedEditor.IsVisible && ((TextBlock)failedEditor.FindName("EditorStatus")).Text.Contains("storage failure"), "Failed creation preserves editor draft"); failedEditor.Close();
                vm.NewScenario(Aegis.Simulation.ScenarioDefinition.FloodDemo()); await Flush();
                await NetworkWindowSmoke.Run(SaveImage);
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
    static void InvokeButton(Window window, string name)
    {
        var button = (Button)window.FindName(name); Require(button.IsEnabled, name + " must be enabled");
        ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
    }
    static void SaveImage(Window window, string name)
    {
        var content = (FrameworkElement)window.Content;
        int width = (int)Math.Ceiling(content.ActualWidth + content.Margin.Left + content.Margin.Right);
        int height = (int)Math.Ceiling(content.ActualHeight + content.Margin.Top + content.Margin.Bottom);
        var image = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        image.Render(window);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        string path = Path.Combine(Path.GetTempPath(), name);
        using (var file = File.Create(path)) encoder.Save(file);
        Console.WriteLine($"SCREENSHOT: {path}");
    }
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
