using System.Text.Json;
using System.Security.Cryptography;
using System.Globalization;
using Aegis.Simulation;
using Aegis.Infrastructure;

internal static class ForecastChecks
{
    public static int Run(string directory)
    {
        int passed = 0;
        void Require(bool value) { if (!value) throw new Exception("Forecast check failed"); }
        void Check(string name, Action action) { action(); passed++; Console.WriteLine("PASS: " + name); }
        void Reject(Action action)
        {
            try { action(); } catch (ArgumentException) { return; } throw new Exception("Expected unsupported forecast input");
        }
        var result = ForecastLab.TrainAndEvaluate();
        var e = result.Evaluation;
        Console.WriteLine(JsonSerializer.Serialize(e));
        Check("Forecast split groups are disjoint with no shared input vectors", () =>
        {
            var training = result.Model.Training.Select(r => r.Group).ToHashSet(StringComparer.Ordinal);
            Require(e.TrainingGroups + e.HoldoutGroups == 800 && e.HoldoutGroups > 100 && e.TrainingGroups > 500);
            Require(result.Holdout.All(p => !training.Contains(p.Group)) && result.Holdout.Select(p => p.Group).Distinct().Count() == e.HoldoutGroups);
        });
        Check("Forecast scaling and baseline fit only training groups", () =>
        {
            for (int i = 0; i < 6; i++) Require(result.Model.Means[i] == result.Model.Training.Average(r => r.Features[i]));
            Require(result.Holdout.All(p => p.Baseline == result.Model.Training.Average(r => r.Ticks)));
        });
        Check("Forecast MAE RMSE and dataset provenance recompute from exported rows", () =>
        {
            Require(Math.Abs(e.Mae - result.Holdout.Average(p => Math.Abs(p.Actual - p.Predicted))) < 1e-9);
            Require(Math.Abs(e.Rmse - Math.Sqrt(result.Holdout.Average(p => Math.Pow(p.Actual - p.Predicted, 2)))) < 1e-9);
            var rows = result.Model.Training.Concat(result.Holdout.Select(p => new ForecastRow(p.Group,
                p.Group.Split('/').Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToArray(), p.Actual))).OrderBy(r => r.Group, StringComparer.Ordinal).ToArray();
            Require(result.DatasetHash == Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(rows))));
        });
        Check("Synthetic holdout regression beats the declared training-mean baseline", () =>
        {
            Require(e.Mae < e.BaselineMae && e.Rmse < e.BaselineRmse && result.Holdout.All(p => double.IsFinite(p.Predicted) && p.Predicted >= 0));
        });
        Check("Synthetic training and inference repeat deterministically", () =>
        {
            var repeat = ForecastLab.TrainAndEvaluate(); Require(JsonSerializer.Serialize(result) == JsonSerializer.Serialize(repeat));
        });
        Check("Forecast refuses unsupported fleet roads states and invalid models", () =>
        {
            Reject(() => ForecastLab.Predict(result.Model, ScenarioDefinition.FloodDemo()));
            Reject(() => ForecastLab.Predict(result.Model, ScenarioDefinition.FleetDemo()));
            var supported = new ScenarioDefinition(2, "Forecast input", 10, new[] { new ShelterDefinition("A", 20, 400, 250) }, 20, 2, 1);
            Require(double.IsFinite(ForecastLab.Predict(result.Model, supported)));
            Reject(() => ForecastLab.Predict(result.Model, supported with { DepotStock = 10 }));
            Reject(() => ForecastLab.Predict(result.Model with { Scales = new double[6] }, supported));
        });
        Check("Forecast cancellation interrupts before generating or publishing a result", () =>
        {
            using var source = new CancellationTokenSource(); source.Cancel(); bool cancelled = false;
            try { ForecastLab.TrainAndEvaluate(source.Token); } catch (OperationCanceledException) { cancelled = true; }
            Require(cancelled);
        });
        Check("Forecast JSON retains model predictions after serialization", () =>
        {
            string path = Path.Combine(directory, "forecast-evaluation.json"); File.WriteAllText(path, JsonSerializer.Serialize(result));
            var loaded = JsonSerializer.Deserialize<ForecastExperiment>(File.ReadAllText(path))!;
            var supported = new ScenarioDefinition(2, "Forecast input", 10, new[] { new ShelterDefinition("A", 20, 400, 250) }, 20, 2, 1);
            Require(ForecastLab.Predict(loaded.Model, supported) == ForecastLab.Predict(result.Model, supported));
        });
        Check("Cancelled report leaves existing output byte-identical and allows retry", () =>
        {
            var run = RunHistory.New(ScenarioDefinition.FloodDemo());
            string output = Path.Combine(directory, "cancelled-report"); Directory.CreateDirectory(output);
            string csv = Path.Combine(output, "run-" + run.Id.ToString("N") + ".csv"); File.WriteAllText(csv, "existing report");
            using var source = new CancellationTokenSource(); source.Cancel(); bool cancelled = false;
            try { Reports.Export(output, run, source.Token); } catch (OperationCanceledException) { cancelled = true; }
            Require(cancelled && File.ReadAllText(csv) == "existing report" && Directory.GetDirectories(output).Length == 0);
            Reports.Export(output, run); Require(File.ReadAllText(csv).StartsWith("tick,shelter"));
        });
        Check("Report cancellation during long replay publishes no staged files", () =>
        {
            var run = RunHistory.New(ScenarioDefinition.FloodDemo()); var session = new SimulationSession(run.Scenario);
            var commands = new RecordedCommand[10000];
            for (int i = 0; i < commands.Length; i++)
            {
                string type = i % 2 == 0 ? "Start" : "Pause"; if (type == "Start") session.Start(); else session.Pause();
                commands[i] = new(i + 1, new(Guid.NewGuid(), type), RunHistory.Hash(session));
            }
            run = run with { Commands = commands };
            string output = Path.Combine(directory, "cancel-long-replay"); using var source = new CancellationTokenSource(); source.CancelAfter(1);
            bool cancelled = false;
            try { Reports.Export(output, run, source.Token); } catch (OperationCanceledException) { cancelled = true; }
            Require(cancelled && (!Directory.Exists(output) || !Directory.EnumerateFileSystemEntries(output).Any()));
        });
        Check("Playback speed changes pacing without changing recorded simulation history", () =>
        {
            var vm = new Aegis.Presentation.RunViewModel(SimulationSession.CreateDemo()); var original = vm.Document;
            vm.PlaybackSpeed = 4; Require(vm.PlaybackSpeed == 4 && vm.Document == original);
            Reject(() => vm.PlaybackSpeed = 3);
            vm.LoadReplay(original); vm.PlaybackSpeed = 2;
            Require(!vm.CanChangePlaybackSpeed && vm.PlaybackSpeed == 4);
        });
        return passed;
    }
}
