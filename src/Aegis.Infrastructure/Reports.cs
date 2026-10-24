using Aegis.Simulation;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Aegis.Infrastructure;

public static class Reports
{
    public static string Compare(RunDocument baseline, RunDocument changed)
    {
        if (JsonSerializer.Serialize(baseline.Scenario) != JsonSerializer.Serialize(changed.Scenario))
            throw new ArgumentException("Compare runs of the same scenario and capacity.");
        var a = RunMetrics.Calculate(baseline); var b = RunMetrics.Calculate(changed);
        return FormattableString.Invariant($"Baseline {baseline.Id.ToString()[..8]}: {a.Ticks} ticks, {a.Delivered} packets\nChanged {changed.Id.ToString()[..8]}: {b.Ticks} ticks, {b.Delivered} packets\nDelta (changed - baseline): {b.Ticks - a.Ticks:+0;-0;0} ticks, {b.Delivered - a.Delivered:+0;-0;0} packets.\nMean service tick: {a.MeanServiceTick:0.00} -> {b.MeanServiceTick:0.00}\nActive ticks: {a.ActiveTickPercent:0.0}% -> {b.ActiveTickPercent:0.0}%\nWaiting ticks: {a.WaitingTicks} -> {b.WaitingTicks}\nCompare completed runs for a completion-time interpretation. Synthetic model only.");
    }
    public static void Export(string directory, RunDocument document)
    {
        Directory.CreateDirectory(directory);
        var replay = RunHistory.Replay(document);
        var metrics = RunMetrics.Calculate(document);
        string stem = Path.Combine(directory, "run-" + document.Id.ToString("N"));
        PdfReport.Save(stem + ".pdf", document);
        var csv = new StringBuilder("tick,shelter,packets,kind\r\n");
        foreach (var e in replay.Events)
            csv.AppendLine($"{e.Tick.ToString(CultureInfo.InvariantCulture)},{Cell(e.Shelter)},{e.Amount.ToString(CultureInfo.InvariantCulture)},{Cell(e.Kind)}");
        File.WriteAllText(stem + ".csv", csv.ToString(), new UTF8Encoding(true));
        string rows = string.Join("", replay.Session.Shelters.Select(s => $"<tr><td>{WebUtility.HtmlEncode(s.Name)}</td><td>{s.InitialDemand}</td><td>{s.Remaining}</td></tr>"));
        string html = $$"""
            <!doctype html>
            <html lang="en"><meta charset="utf-8"><title>AEGIS-SIM Run Report</title>
            <style>
            body{font:16px Segoe UI,sans-serif;max-width:900px;margin:48px auto;color:#173c4f}
            table{border-collapse:collapse;width:100%}td,th{padding:12px;border:1px solid #ccd8de;text-align:left}
            code{overflow-wrap:anywhere}.metrics{padding:20px;background:#edf5f1}@media print{body{margin:12mm} }
            </style>
            <h1>AEGIS-SIM / Exercise report</h1><p>SYNTHETIC CIVILIAN TRAINING DATA</p>
            <h2>{{WebUtility.HtmlEncode(document.Scenario.Name)}}</h2>
            <p>Run {{document.Id}}<br>Created {{document.CreatedAt:O}}<br>Engine {{document.EngineVersion}} / Schema {{document.SchemaVersion}}</p>
            <div class="metrics">
            <p>Status: {{replay.Session.Status}} / Tick: {{metrics.Ticks}} / Delivered: {{metrics.Delivered}} / Remaining: {{metrics.Remaining}}</p>
            <p>Completion: {{metrics.CompletionPercent:0.0}}% / Mean service tick: {{metrics.MeanServiceTick:0.00}} / Active ticks: {{metrics.ActiveTickPercent:0.0}}%</p>
            <p>Waiting ticks: {{metrics.WaitingTicks}} / Closure commands: {{metrics.Closures}}</p>
            </div>
            <table><tr><th>Shelter</th><th>Initial</th><th>Remaining</th></tr>{{rows}}</table>
            <h2>Reproducibility</h2><p>Verified ordered commands: {{document.Commands.Length}}. Final state SHA-256:</p>
            <code>{{RunHistory.Hash(replay.Session)}}</code>
            <h2>Methods and limitations</h2>
            <p>All demand is available at tick zero. Mean service tick is packet-weighted delivery tick; it is not measured real-world waiting time.
            Active tick percentage is delivery ticks divided by elapsed ticks; zero elapsed ticks give zero active percentage.
            Fixed-step synthetic model, fresh load per visit, no travel-time physics or real-world effectiveness claim.
            Route closure makes a shelter unavailable for logical ticks. PDF, HTML and CSV derive from verified replay.</p></html>
            """;
        File.WriteAllText(stem + ".html", html);
    }
    private static string Cell(string value)
    {
        if (value.Length > 0 && "=+-@\t\r".Contains(value[0])) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
