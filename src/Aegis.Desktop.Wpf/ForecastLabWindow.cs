using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Aegis.Simulation;
using Microsoft.Win32;

namespace Aegis.Desktop.Wpf;

public sealed class ForecastLabWindow : Window
{
    private readonly CancellationTokenSource cancellation = new();
    public ForecastExperiment? Result { get; private set; }
    public ForecastLabWindow()
    {
        NameScope.SetNameScope(this, new NameScope());
        Title = "Synthetic forecast / learning experiment"; Width = 740; Height = 590; MinWidth = 600; MinHeight = 500; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new(24) };
        panel.Children.Add(new TextBlock { Text = "SYNTHETIC FORECAST LAB", FontSize = 23, FontWeight = FontWeights.SemiBold });
        var output = new TextBlock { Text = "Generating synthetic examples and fitting a model in the background...", TextWrapping = TextWrapping.Wrap, Margin = new(0, 18, 0, 18), FontSize = 15 };
        RegisterName("ForecastResultText", output); panel.Children.Add(output);
        panel.Children.Add(new TextBlock { Text = "Learning experiment only: 800 generated schema-2 starts, grouped holdout, training-only scaling, 7-nearest-neighbour regression. MAE/RMSE compare predicted completion ticks against separately simulated holdout labels and a training-mean baseline.\n\nNo field data, incidents, roads, fleet or mid-run prediction is supported. Results do not establish real-world accuracy. Exact completion preview remains the tool for your current exercise. This window does not read or change your run.", TextWrapping = TextWrapping.Wrap });
        bool closed = false, started = false;
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new(0, 20, 0, 0) };
        var cancel = new Button { Content = "Cancel", Padding = new(16, 8, 16, 8) }; cancel.Click += (_, _) => { cancellation.Cancel(); cancel.IsEnabled = false; };
        var export = new Button { Content = "Export evaluation + model", Padding = new(16, 8, 16, 8), Margin = new(10, 0, 0, 0), IsEnabled = false };
        RegisterName("ForecastExportButton", export);
        export.Click += async (_, _) =>
        {
            var dialog = new SaveFileDialog { Filter = "Evaluation JSON|*.json", FileName = "synthetic-forecast-evaluation.json" };
            if (Result == null || dialog.ShowDialog(this) != true) return;
            export.IsEnabled = false;
            try { string json = JsonSerializer.Serialize(Result, new JsonSerializerOptions { WriteIndented = true }); await System.IO.File.WriteAllTextAsync(dialog.FileName, json); }
            catch (Exception error) { if (!closed) MessageBox.Show(this, error.Message, "Export failed"); }
            finally { if (!closed) export.IsEnabled = Result != null; }
        };
        buttons.Children.Add(cancel); buttons.Children.Add(export); panel.Children.Add(buttons); Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Closed += (_, _) => { closed = true; cancellation.Cancel(); };
        Loaded += async (_, _) =>
        {
            if (started) return; started = true;
            try
            {
                var result = await Task.Run(() => ForecastLab.TrainAndEvaluate(cancellation.Token));
                if (closed || cancellation.IsCancellationRequested) return;
                Result = result; var e = result.Evaluation;
                output.Text = FormattableString.Invariant($"Training groups: {e.TrainingGroups} / Holdout groups: {e.HoldoutGroups}\nModel MAE: {e.Mae:0.00} ticks / RMSE: {e.Rmse:0.00}\nTraining-mean baseline MAE: {e.BaselineMae:0.00} / RMSE: {e.BaselineRmse:0.00}\nWorst holdout error: {e.WorstError:0.00} ticks\nDataset SHA-256: {result.DatasetHash}");
                export.IsEnabled = true;
            }
            catch (OperationCanceledException) { if (!closed) output.Text = "Experiment cancelled. No run history was changed."; }
            catch (Exception error) { if (!closed) output.Text = "Experiment failed: " + error.Message; }
            finally { cancel.IsEnabled = false; }
        };
    }
}
