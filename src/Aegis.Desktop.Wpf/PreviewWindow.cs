using System.Windows;
using System.Windows.Controls;
using Aegis.Simulation;

namespace Aegis.Desktop.Wpf;

public sealed class PreviewWindow : Window
{
    private readonly CancellationTokenSource cancellation = new();
    public PreviewWindow(RunDocument document, int? replayTick)
    {
        NameScope.SetNameScope(this, new NameScope());
        Title = "Planning / deterministic continuation"; Width = 660; Height = 510; MinWidth = 560; MinHeight = 450; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new(26) };
        panel.Children.Add(new TextBlock { Text = "COMPLETION PREVIEW", FontSize = 24, FontWeight = FontWeights.SemiBold });
        var output = new TextBlock { Text = "Calculating on a separate copy...", TextWrapping = TextWrapping.Wrap, FontSize = 15, Margin = new(0, 20, 0, 20) }; panel.Children.Add(output);
        RegisterName("ProjectionText", output);
        panel.Children.Add(new TextBlock { Text = "Assumptions: no new incidents or restocks. Existing closures expire normally. Current dispatch order and travel settings continue. This is an exact model continuation under those assumptions, not AI or a real-world forecast.\n\nYour run, command history and database remain unchanged.", TextWrapping = TextWrapping.Wrap });
        Content = panel;
        Closed += (_, _) => cancellation.Cancel();
        Loaded += async (_, _) =>
        {
            try
            {
                var result = await Task.Run(() => CompletionPreview.Calculate(document, replayTick, cancellation.Token));
                if (cancellation.IsCancellationRequested) return;
                output.Text = $"Run {document.Id.ToString()[..8]} / engine {document.EngineVersion}\nStarting at tick {result.StartTick}\nOutcome: {result.Outcome}\nProjected tick: {result.EndTick} (+{result.AdditionalTicks})\nDelivered: {result.Delivered} / Pending: {result.Remaining}\nDepot: {result.Stock?.ToString() ?? "unlimited"} / Cargo: {result.Cargo}\nAdditional supply needed: {result.AdditionalSupplyRequired} packets";
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { output.Text = "Preview could not be calculated: " + e.Message; }
        };
    }
}
