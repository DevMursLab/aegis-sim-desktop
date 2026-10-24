using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Aegis.Infrastructure;
using Aegis.Presentation;
using Aegis.Simulation;

namespace Aegis.Desktop.Wpf;

public sealed class DesktopWorkspace(MainWindow window, RunViewModel model, RunRepository repository)
{
    public void Execute(string action)
    {
        try
        {
            if (model.IsRunning && action is "Logistics" or "Import" or "History") model.PauseCommand.Execute(null);
            switch (action)
            {
                case "Layers": new LayerWindow { Owner = window }.Show(); break;
                case "Preview": new PreviewWindow(model.Document, model.IsReplay ? model.ReplayTick : null) { Owner = window }.Show(); break;
                case "Diagnostics":
                    var diagnostics = new SaveFileDialog { Filter = "Diagnostics JSON|*.json", FileName = "aegis-diagnostics.json" };
                    if (diagnostics.ShowDialog(window) == true) { System.IO.File.WriteAllText(diagnostics.FileName, Diagnostics.Create(model.Document)); model.Notify("Diagnostics exported (no credentials or file paths)."); }
                    break;
                case "Logistics": model.NewScenario(ScenarioDefinition.LogisticsDemo()); break;
                case "Import":
                    var open = new OpenFileDialog { Filter = "Scenario JSON/XML|*.json;*.xml" };
                    if (open.ShowDialog(window) == true) model.NewScenario(ScenarioFiles.Load(open.FileName));
                    break;
                case "Export":
                    var save = new SaveFileDialog { Filter = "JSON scenario|*.json|XML scenario|*.xml", FileName = "synthetic-scenario.json" };
                    if (save.ShowDialog(window) == true) { ScenarioFiles.Save(save.FileName, model.Document.Scenario); model.Notify("Scenario exported: " + save.FileName); }
                    break;
                case "History":
                    var history = new HistoryWindow(repository.List()) { Owner = window };
                    if (history.ShowDialog() == true)
                    {
                        if (history.CompareId is { } second)
                            MessageBox.Show(window, Reports.Compare(repository.Load(history.SelectedId), repository.Load(second)), "Verified A/B comparison");
                        else model.LoadReplay(repository.Load(history.SelectedId));
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
    public Guid SelectedId { get; private set; }
    public Guid? CompareId { get; private set; }
    public HistoryWindow(StoredRun[] runs)
    {
        Title = "Saved exercises / verified replay and comparison";
        Width = 920; Height = 500; MinWidth = 700; MinHeight = 400; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new DockPanel { Margin = new Thickness(20) };
        var header = new TextBlock { Text = "Select a saved run for read-only replay. Choose two compatible runs to compare.", Margin = new Thickness(0, 0, 0, 15), TextWrapping = TextWrapping.Wrap };
        DockPanel.SetDock(header, Dock.Top); panel.Children.Add(header);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 15, 0, 0) };
        DockPanel.SetDock(actions, Dock.Bottom); panel.Children.Add(actions);
        var list = new ListBox { ItemsSource = runs, SelectionMode = SelectionMode.Extended };
        var replay = new Button { Content = "Open replay", Padding = new Thickness(15, 8, 15, 8), Margin = new Thickness(0, 0, 12, 0) };
        var compare = new Button { Content = "Compare selected two", Padding = new Thickness(15, 8, 15, 8) };
        replay.Click += (_, _) => { if (list.SelectedItem is StoredRun run) { SelectedId = run.Id; DialogResult = true; } };
        compare.Click += (_, _) =>
        {
            if (list.SelectedItems.Count != 2) { MessageBox.Show(this, "Select exactly two runs with Ctrl+click. First selected is baseline."); return; }
            SelectedId = ((StoredRun)list.SelectedItems[0]!).Id; CompareId = ((StoredRun)list.SelectedItems[1]!).Id; DialogResult = true;
        };
        actions.Children.Add(replay); actions.Children.Add(compare); panel.Children.Add(list); Content = panel;
    }
}
