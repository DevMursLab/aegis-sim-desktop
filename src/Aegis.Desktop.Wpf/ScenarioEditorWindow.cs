using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Aegis.Infrastructure;
using Aegis.Presentation;
using Aegis.Simulation;
using Microsoft.Win32;

namespace Aegis.Desktop.Wpf;

public sealed class ScenarioEditorWindow : Window
{
    public ScenarioDraft Draft { get; }
    private readonly DataGrid grid;
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new(0, 12, 0, 0), Foreground = Brushes.DarkSlateGray };
    private readonly Func<ScenarioDefinition, string?> start;

    public ScenarioEditorWindow(ScenarioDefinition source, Func<ScenarioDefinition, string?> start)
    {
        NameScope.SetNameScope(this, new NameScope());
        Draft = new(source); this.start = start; DataContext = Draft;
        Title = "Scenario editor / local draft"; Width = 1060; Height = 760; MinWidth = 900; MinHeight = 650;
        FontFamily = new FontFamily("Segoe UI"); Background = new SolidColorBrush(Color.FromRgb(243, 245, 247)); WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new DockPanel { Margin = new(24) };
        var top = new StackPanel(); DockPanel.SetDock(top, Dock.Top);
        top.Children.Add(new TextBlock { Text = "SCENARIO EDITOR", FontSize = 26, FontWeight = FontWeights.SemiBold, Foreground = Brushes.DarkSlateGray });
        top.Children.Add(new TextBlock { Text = "Edit a separate local draft. Starting creates a new run; saved history stays available.", TextWrapping = TextWrapping.Wrap, Margin = new(0, 6, 0, 14) });
        var fields = new WrapPanel();
        AddField(fields, "Scenario name", nameof(ScenarioDraft.Name), 300);
        AddField(fields, "Vehicle capacity", nameof(ScenarioDraft.Capacity), 150);
        var logistics = new CheckBox { Content = "Finite stock + travel", Margin = new(0, 18, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        logistics.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(ScenarioDraft.UsesLogistics)) { Mode = BindingMode.TwoWay });
        RegisterName("LogisticsMode", logistics);
        fields.Children.Add(logistics); top.Children.Add(fields);
        var multi = new CheckBox { Content = "Multiple vehicles (schema 3)", Margin = new(0, 8, 0, 0) };
        multi.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(ScenarioDraft.UsesFleet)) { Mode = BindingMode.TwoWay });
        RegisterName("FleetMode", multi); top.Children.Add(multi);
        var inventory = new WrapPanel { Margin = new(0, 10, 0, 8) };
        inventory.SetBinding(IsEnabledProperty, nameof(ScenarioDraft.UsesLogistics));
        AddField(inventory, "Depot stock", nameof(ScenarioDraft.DepotStock), 150);
        AddField(inventory, "Outbound ticks", nameof(ScenarioDraft.TravelTicks), 150);
        AddField(inventory, "Return ticks", nameof(ScenarioDraft.ReturnTicks), 150);
        AddField(inventory, "Vehicles (1–8)", nameof(ScenarioDraft.FleetSize), 150);
        ((TextBox)FindName("FleetSizeInput")).SetBinding(IsEnabledProperty, nameof(ScenarioDraft.UsesFleet));
        top.Children.Add(inventory);
        top.Children.Add(new TextBlock { Text = "Local X 50–1050, Y 70–700. Demand 0–10000; max 64 shelters / 10000 packets. Travel override is optional. Priority 0–9 is used in fleet mode; higher goes first, ties keep list order.", TextWrapping = TextWrapping.Wrap, Margin = new(0, 0, 0, 12), Foreground = Brushes.DarkSlateGray });
        panel.Children.Add(top);
        var bottom = new StackPanel { Margin = new(0, 14, 0, 0) }; DockPanel.SetDock(bottom, Dock.Bottom);
        var actions = new WrapPanel();
        AddButton(actions, "Add shelter", "AddShelterButton", () => { Commit(); if (!Draft.AddShelter()) Feedback("Maximum 64 shelters reached.", false); });
        AddButton(actions, "Remove selected", "RemoveShelterButton", () => { Commit(); if (grid!.SelectedItem is ShelterDraft row) Draft.Shelters.Remove(row); });
        AddButton(actions, "Validate", "ValidateScenarioButton", () => ValidateDraft(out _));
        AddButton(actions, "Save JSON/XML", "SaveScenarioButton", () =>
        {
            if (!ValidateDraft(out _)) return;
            var picker = new SaveFileDialog { Filter = "Scenario JSON|*.json|Scenario XML|*.xml", FileName = "my-scenario.json" };
            if (picker.ShowDialog(this) == true) SaveTo(picker.FileName);
        });
        AddButton(actions, "Start as new run", "StartScenarioButton", () =>
        {
            if (!ValidateDraft(out var scenario)) return;
            try
            {
                string? error = start(scenario!);
                if (error != null) { Feedback("Run not created: " + error, false); return; }
                Close();
            }
            catch (Exception error) { Feedback("Run not created: " + error.Message, false); }
        });
        AddButton(actions, "Close draft", "CloseEditorButton", Close);
        bottom.Children.Add(actions); bottom.Children.Add(status); panel.Children.Add(bottom);
        grid = new DataGrid { ItemsSource = Draft.Shelters, AutoGenerateColumns = false, CanUserAddRows = false, CanUserDeleteRows = false, SelectionMode = DataGridSelectionMode.Single, HeadersVisibility = DataGridHeadersVisibility.Column, RowHeight = 34, BorderBrush = Brushes.LightGray };
        RegisterName("ShelterEditorGrid", grid); RegisterName("EditorStatus", status);
        foreach (var (header, property, width) in new[] { ("Shelter name", "Name", 210), ("Demand", "Demand", 90), ("X", "X", 90), ("Y", "Y", 90), ("Travel override", "TravelTicks", 140), ("Priority (fleet)", "Priority", 110) })
            grid.Columns.Add(new DataGridTextColumn { Header = header, Binding = new Binding(property) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }, Width = width });
        panel.Children.Add(grid); Content = panel;
        Feedback("Draft copied from the selected scenario. Numbers use a dot for decimals. Close discards unsaved edits.", true);
    }

    public bool SaveTo(string path)
    {
        if (!ValidateDraft(out var scenario)) return false;
        try { ScenarioFiles.Save(path, scenario!); Feedback("Scenario saved: " + path, true); return true; }
        catch (Exception error) { Feedback("Save failed; draft preserved. " + error.Message, false); return false; }
    }
    private bool ValidateDraft(out ScenarioDefinition? scenario)
    {
        Commit(); bool valid = Draft.TryBuild(out scenario, out string message); Feedback(message, valid); return valid;
    }
    private void Commit() { grid.CommitEdit(DataGridEditingUnit.Cell, true); grid.CommitEdit(DataGridEditingUnit.Row, true); }
    private void Feedback(string text, bool valid) { status.Text = text; status.Foreground = valid ? Brushes.DarkSlateGray : Brushes.Firebrick; }
    private void AddField(Panel parent, string label, string property, double width)
    {
        var field = new StackPanel { Width = width, Margin = new(0, 0, 16, 0) };
        field.Children.Add(new TextBlock { Text = label, Margin = new(0, 0, 0, 5) });
        var input = new TextBox { Padding = new(8, 6, 8, 6) };
        input.SetBinding(TextBox.TextProperty, new Binding(property) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        RegisterName(property + "Input", input); field.Children.Add(input); parent.Children.Add(field);
    }
    private void AddButton(Panel panel, string label, string name, Action action)
    {
        var button = new Button { Content = label, Padding = new(12, 8, 12, 8), Margin = new(0, 0, 10, 6) };
        button.Click += (_, _) => action(); RegisterName(name, button); panel.Children.Add(button);
    }
}
