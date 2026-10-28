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
    public static void Export(string directory, RunDocument document, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(directory);
        var replay = RunHistory.Replay(document, cancellationToken: cancellationToken);
        var metrics = RunMetrics.Calculate(replay, document.Commands.Count(c => c.Command.Type is "CloseRoute" or "CloseRoad"));
        // Generate every format privately before touching any existing report.
        string staging = Path.Combine(Path.GetFullPath(directory), ".aegis-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        bool published = false;
        try
        {
            WriteVerified(staging, document, replay, metrics, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Publish(staging, Path.GetFullPath(directory));
            published = true;
        }
        finally
        {
            string backups = Path.Combine(staging, "previous");
            // Preserve recoverable originals if rollback itself encounters an I/O failure.
            if (published || !Directory.Exists(backups) || !Directory.EnumerateFiles(backups).Any())
                Directory.Delete(staging, recursive: true);
        }
    }

    private static void Publish(string staging, string directory)
    {
        var published = new List<string>();
        var backups = new List<(string Target, string Backup)>();
        string backupDirectory = Path.Combine(staging, "previous");
        Directory.CreateDirectory(backupDirectory);
        try
        {
            foreach (string source in Directory.GetFiles(staging).Order(StringComparer.Ordinal))
            {
                string target = Path.Combine(directory, Path.GetFileName(source));
                if (File.Exists(target))
                {
                    string backup = Path.Combine(backupDirectory, Path.GetFileName(source));
                    File.Move(target, backup);
                    backups.Add((target, backup));
                }
                File.Move(source, target);
                published.Add(target);
            }
        }
        catch (Exception publicationError)
        {
            var errors = new List<Exception> { publicationError };
            foreach (string target in published.AsEnumerable().Reverse())
            {
                try { File.Delete(target); }
                catch (Exception error) { errors.Add(error); }
            }
            foreach (var (target, backup) in backups.AsEnumerable().Reverse())
            {
                try { File.Move(backup, target); }
                catch (Exception error) { errors.Add(error); }
            }
            if (errors.Count > 1)
                throw new IOException($"Report publication and rollback failed. Recoverable originals are in {backupDirectory}", new AggregateException(errors));
            throw;
        }
    }

    private static void WriteVerified(string directory, RunDocument document, ReconstructedRun replay, RunMetrics metrics, CancellationToken cancellationToken)
    {
        string stem = Path.Combine(directory, "run-" + document.Id.ToString("N"));
        PdfReport.SaveVerified(stem + ".pdf", document, replay, metrics, cancellationToken);
        var csv = new StringBuilder("tick,shelter,packets,kind\r\n");
        foreach (var e in replay.Events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            csv.AppendLine($"{e.Tick.ToString(CultureInfo.InvariantCulture)},{Cell(e.Shelter)},{e.Amount.ToString(CultureInfo.InvariantCulture)},{Cell(e.Kind)}");
        }
        File.WriteAllText(stem + ".csv", csv.ToString(), new UTF8Encoding(true));
        if (replay.Session.UsesFleet)
        {
            bool priority = replay.Session.Shelters.Any(s => s.Priority != 0) || replay.Session.RoadClosures.Count > 0 || replay.Session.JunctionRoutingEnabled;
            var detail = new StringBuilder("tick,vehicle,shelter,packets,kind" + (priority ? ",reason" : "") + "\r\n");
            foreach (var e in replay.Events)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var action in e.Actions!) detail.AppendLine($"{e.Tick},{Cell(action.VehicleId)},{Cell(action.Shelter)},{action.Amount},{Cell(action.Kind)}" + (priority ? "," + Cell(action.Reason ?? "") : ""));
            }
            File.WriteAllText(stem + "-vehicles.csv", detail.ToString(), new UTF8Encoding(true));
        }
        string rows = string.Join("", replay.Session.Shelters.Select(s => $"<tr><td>{WebUtility.HtmlEncode(s.Name)}</td><td>{s.InitialDemand}</td><td>{s.Remaining}</td><td>{s.Priority}</td></tr>"));
        string routingPolicy = replay.Session.JunctionRoutingEnabled ?
            "Junction routing enabled: entered roads finish even if closed; shortest open paths are chosen at nodes in both directions. No path means waiting at the node. One road maximum per tick; unused tick budget is discarded at nodes, so each edge rounds travel time upward separately. Return paths can differ from outbound." :
            "Assigned-trip routing: return follows the same route and duration. Road closures change new dispatch paths; assigned trips wait without changing position. Return uses the assigned outbound path.";
        string geographic = document.Scenario.Geography is { } map ? "<h2>Road-derived timing</h2><p>Undirected shortest paths; spherical road lengths rounded upward per road. " +
            map.MetersPerTick + " meters per tick. Shelter closures freeze outbound service. " + routingPolicy + " Source SHA-256: <code>" + map.SourceHash + "</code></p><ul>" +
            string.Join("", map.Routes.Select(r => "<li>" + WebUtility.HtmlEncode(r.Shelter) + ": " + r.Meters + " m</li>")) + "</ul>" : "";
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
            <p>Model schema: {{document.Scenario.SchemaVersion}} / Depot stock: {{(metrics.DepotStockRemaining.HasValue ? metrics.DepotStockRemaining.Value.ToString(CultureInfo.InvariantCulture) : "unlimited (legacy)")}} / In transit: {{metrics.InTransit}} / Restocked: {{metrics.RestockedTotal}}</p>
            <p>Vehicles: {{replay.Session.Vehicles.Count}} / Outbound vehicle-ticks (including arrival and blocked-route delays): {{metrics.OutboundTicks}} / Return ticks: {{metrics.ReturnTicks}}</p>
            </div>
            <table><tr><th>Shelter</th><th>Initial</th><th>Remaining</th><th>Priority</th></tr>{{rows}}</table>
            {{geographic}}
            {{(replay.Session.RoadClosures.Count == 0 ? "" : "<h2>Road incidents</h2><ul>" + string.Join("", replay.Session.RoadClosures.Select(c => "<li>" + WebUtility.HtmlEncode(c.RoadId) + ": closed through tick " + c.BlockedUntil + "</li>")) + "</ul>")}}
            <h2>Reproducibility</h2><p>Verified ordered commands: {{document.Commands.Length}}. Final state SHA-256:</p>
            <code>{{RunHistory.Hash(replay.Session)}}</code>
            <h2>Methods and limitations</h2>
            <p>All demand is available at tick zero. Mean service tick is packet-weighted delivery tick; it is not measured real-world waiting time.
            Active tick percentage is delivery ticks divided by elapsed ticks; zero elapsed ticks give zero active percentage.
            Schema 1 uses instant visits and unlimited reload supply. Schemas 2 and 3 reserve finite stock at dispatch and deliver after configured outbound ticks;
            schema 3 advances all vehicles on a shared tick, in stable vehicle-ID order. Outbound/return counts sum vehicle-ticks; waiting counts elapsed ticks with any route wait.
            Fleet dispatch chooses the highest-priority open shelter with unreserved demand; ties preserve shelter-list order. Existing cargo is not reassigned. Priority is static per scenario, and no starvation-freedom or real-world urgency claim is made.
            it waits for the final return to complete. Travel ticks are scenario settings, not distance/speed physics. Route closure freezes outbound progress;
            shelter closures leave return travel unaffected. Shared road closures can pause both directions. {{(replay.Session.JunctionRoutingEnabled ? "Junction routing recomputes paths at reached nodes in both directions; entered edges finish before diversion." : "New dispatches recompute shortest open paths; assigned trips retain their path.")}} PDF, HTML and CSV derive from verified replay. No real-world effectiveness claim.</p></html>
            """;
        File.WriteAllText(stem + ".html", html);
    }
    private static string Cell(string value)
    {
        if (value.Length > 0 && "=+-@\t\r".Contains(value[0])) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
