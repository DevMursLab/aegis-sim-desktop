using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Aegis.Infrastructure;
using Aegis.Presentation;
using Aegis.Simulation;

namespace Aegis.Desktop.Wpf;

public sealed class DesktopWorkspace(MainWindow window, RunViewModel model, RunRepository repository)
{
    public async void Execute(string action)
    {
        try
        {
            if (model.IsRunning && action is "Logistics" or "Import" or "History") model.PauseCommand.Execute(null);
            switch (action)
            {
                case "Fleet": model.NewScenario(ScenarioDefinition.FleetDemo()); break;
                case "Priority": model.NewScenario(ScenarioDefinition.PriorityDemo()); break;
                case "GeoDemo": model.NewScenario(GeoExercise.Load(System.IO.Path.Combine(AppContext.BaseDirectory, "road-exercise.geojson"))); break;
                case "Editor":
                    if (!model.CanEditScenario) throw new InvalidOperationException("Edit geographical exercises in their road GeoJSON; shared scenario editing is unavailable.");
                    new ScenarioEditorWindow(model.Document.Scenario, scenario =>
                    {
                        Guid previous = model.Document.Id; model.NewScenario(scenario);
                        return model.Document.Id != previous ? null : model.Message;
                    }) { Owner = window }.Show();
                    break;
                case "Layers": new LayerWindow { Owner = window }.Show(); break;
                case "Preview": new PreviewWindow(model.Document, model.IsReplay ? model.ReplayTick : null) { Owner = window }.Show(); break;
                case "Diagnostics":
                    var diagnostics = new SaveFileDialog { Filter = "Diagnostics JSON|*.json", FileName = "aegis-diagnostics.json" };
                    if (diagnostics.ShowDialog(window) == true) { System.IO.File.WriteAllText(diagnostics.FileName, Diagnostics.Create(model.Document)); model.Notify("Diagnostics exported (no credentials or file paths)."); }
                    break;
                case "Logistics": model.NewScenario(ScenarioDefinition.LogisticsDemo()); break;
                case "Import":
                    var open = new OpenFileDialog { Filter = "Scenario JSON/XML or road GeoJSON exercise|*.json;*.xml;*.geojson" };
                    if (open.ShowDialog(window) == true) model.NewScenario(ScenarioFiles.Load(open.FileName));
                    break;
                case "Export":
                    var save = new SaveFileDialog { Filter = "JSON scenario|*.json|XML scenario|*.xml", FileName = "synthetic-scenario.json" };
                    if (save.ShowDialog(window) == true) { ScenarioFiles.Save(save.FileName, model.Document.Scenario); model.Notify("Scenario exported: " + save.FileName); }
                    break;
                case "History":
                    var history = new HistoryWindow(repository) { Owner = window };
                    if (history.ShowDialog() == true)
                    {
                        if (history.Comparison is { } comparison)
                            MessageBox.Show(window, comparison, "Verified A/B comparison");
                        else if (history.SelectedDocument is { } saved) await model.LoadReplayAsync(saved);
                    }
                    break;
                case "Report":
                    var folder = new OpenFolderDialog { Title = "Choose a folder for verified PDF, HTML and CSV reports" };
                    if (folder.ShowDialog(window) == true) { Reports.Export(folder.FolderName, model.Document); model.Notify("Verified full-run PDF/HTML/CSV reports exported to " + folder.FolderName); }
                    break;
            }
        }
        catch (Exception error) { model.Notify("Action failed safely: " + error.Message); }
    }
}

public sealed class HistoryWindow : Window
{
    public RunDocument? SelectedDocument { get; private set; }
    public string? Comparison { get; private set; }
    public Guid SelectedId { get; private set; }
    public Guid? CompareId { get; private set; }
    public HistoryWindow(RunRepository repository)
    {
        Title = "Saved exercises / verified replay and comparison";
        Width = 920; Height = 500; MinWidth = 700; MinHeight = 400; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new DockPanel { Margin = new Thickness(20) };
        var header = new TextBlock { Text = "Select a saved run for read-only replay. Choose two compatible runs to compare.", Margin = new Thickness(0, 0, 0, 15), TextWrapping = TextWrapping.Wrap };
        DockPanel.SetDock(header, Dock.Top); panel.Children.Add(header);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 15, 0, 0) };
        DockPanel.SetDock(actions, Dock.Bottom); panel.Children.Add(actions);
        var list = new ListBox { SelectionMode = SelectionMode.Extended };
        var replay = new Button { Content = "Open replay", Padding = new Thickness(15, 8, 15, 8), Margin = new Thickness(0, 0, 12, 0) };
        var compare = new Button { Content = "Compare selected two", Padding = new Thickness(15, 8, 15, 8) };
        bool closed = false, loading = false;
        Closed += (_, _) => closed = true;
        async Task Work(Func<Task> work)
        {
            if (loading || closed) return;
            loading = true; replay.IsEnabled = compare.IsEnabled = list.IsEnabled = false;
            header.Text = "Loading and verifying saved history... You can close this window to discard the result.";
            try { await work(); }
            catch (Exception error) { if (!closed) header.Text = "History could not be opened: " + error.Message; }
            finally { loading = false; if (!closed) replay.IsEnabled = compare.IsEnabled = list.IsEnabled = true; }
        }
        Loaded += async (_, _) => await Work(async () =>
        {
            var runs = await Task.Run(repository.List);
            if (closed) return; list.ItemsSource = runs;
            header.Text = "Select a saved run for read-only replay. Choose two compatible runs to compare.";
        });
        replay.Click += async (_, _) =>
        {
            if (list.SelectedItem is not StoredRun run) return;
            await Work(async () =>
            {
                var saved = await Task.Run(() => repository.Load(run.Id));
                if (closed) return; SelectedId = run.Id; SelectedDocument = saved; DialogResult = true;
            });
        };
        compare.Click += async (_, _) =>
        {
            if (list.SelectedItems.Count != 2) { MessageBox.Show(this, "Select exactly two runs with Ctrl+click. First selected is baseline."); return; }
            var first = ((StoredRun)list.SelectedItems[0]!).Id; var second = ((StoredRun)list.SelectedItems[1]!).Id;
            await Work(async () =>
            {
                var result = await Task.Run(() => Reports.Compare(repository.Load(first), repository.Load(second)));
                if (closed) return; SelectedId = first; CompareId = second; Comparison = result; DialogResult = true;
            });
        };
        actions.Children.Add(replay); actions.Children.Add(compare); panel.Children.Add(list); Content = panel;
    }
}
