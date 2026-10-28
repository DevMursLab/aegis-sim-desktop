using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Aegis.Simulation;

public sealed record ForecastRow(string Group, double[] Features, double Ticks);
public sealed record ForecastModel(int Neighbours, double[] Means, double[] Scales, ForecastRow[] Training);
public sealed record ForecastPrediction(string Group, double Actual, double Predicted, double Baseline);
public sealed record ForecastEvaluation(int TrainingGroups, int HoldoutGroups, double Mae, double Rmse,
    double BaselineMae, double BaselineRmse, double WorstError);
public sealed record ForecastExperiment(int Format, string Algorithm, string Engine, int Seed, string DatasetHash,
    string Domain, ForecastModel Model, ForecastPrediction[] Holdout, ForecastEvaluation Evaluation);

// A small evaluated synthetic-data experiment, separate from exact completion preview.
public static class ForecastLab
{
    public static double[] Features(ScenarioDefinition scenario)
    {
        scenario.Validate();
        if (scenario.SchemaVersion != 2 || scenario.Capacity is < 4 or > 25 || scenario.Shelters.Length is < 1 or > 5 ||
            scenario.TravelTicks is < 1 or > 6 || scenario.ReturnTicks is < 0 or > 4 ||
            scenario.Shelters.Any(s => s.Demand is < 1 or > 30 || s.TravelTicks != null) || scenario.DepotStock != scenario.Shelters.Sum(s => s.Demand))
            throw new ArgumentException("Forecast lab supports only generated schema-2 starts: 1-5 shelters, demands 1-30, capacity 4-25, travel 1-6, return 0-4 and stock equal to demand. No incidents, fleet or road histories.");
        return new double[] { scenario.Shelters.Sum(s => s.Demand), scenario.Capacity, scenario.Shelters.Length,
            scenario.TravelTicks, scenario.ReturnTicks, scenario.Shelters.Max(s => s.Demand) };
    }

    public static ForecastExperiment TrainAndEvaluate(CancellationToken cancellationToken = default)
    {
        const int seed = 314159;
        var random = new Random(seed);
        var rows = new Dictionary<string, ForecastRow>(StringComparer.Ordinal);
        while (rows.Count < 800)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int count = random.Next(1, 6), capacity = random.Next(4, 26), travel = random.Next(1, 7), back = random.Next(0, 5);
            var shelters = Enumerable.Range(0, count).Select(i => new ShelterDefinition("Synthetic-" + i,
                random.Next(1, 31), 200 + i * 160, 250)).ToArray();
            var scenario = new ScenarioDefinition(2, "Synthetic forecast sample", capacity, shelters, shelters.Sum(s => s.Demand), travel, back);
            var features = Features(scenario);
            // Identical feature vectors always belong to the same fold, including different demand permutations.
            string group = string.Join("/", features.Select(x => x.ToString("R", CultureInfo.InvariantCulture)));
            if (rows.ContainsKey(group)) continue;
            var session = new SimulationSession(scenario);
            while (session.Status != RunStatus.Completed)
            {
                cancellationToken.ThrowIfCancellationRequested(); session.Step();
            }
            rows.Add(group, new(group, features, session.Tick));
        }
        var ordered = rows.Values.OrderBy(r => r.Group, StringComparer.Ordinal).ToArray();
        bool IsHoldout(ForecastRow row) => SHA256.HashData(Encoding.UTF8.GetBytes(row.Group))[0] % 5 == 0;
        var training = ordered.Where(r => !IsHoldout(r)).ToArray();
        var holdout = ordered.Where(IsHoldout).ToArray();
        var means = Enumerable.Range(0, 6).Select(i => training.Average(r => r.Features[i])).ToArray();
        var scales = Enumerable.Range(0, 6).Select(i => Math.Max(1, Math.Sqrt(training.Average(r => Math.Pow(r.Features[i] - means[i], 2))))).ToArray();
        var model = new ForecastModel(7, means, scales, training);
        double baseline = training.Average(r => r.Ticks);
        var predictions = holdout.Select(r =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new ForecastPrediction(r.Group, r.Ticks, PredictFeatures(model, r.Features, cancellationToken), baseline);
        }).ToArray();
        var evaluation = new ForecastEvaluation(training.Length, holdout.Length,
            predictions.Average(p => Math.Abs(p.Actual - p.Predicted)), Math.Sqrt(predictions.Average(p => Math.Pow(p.Actual - p.Predicted, 2))),
            predictions.Average(p => Math.Abs(p.Actual - p.Baseline)), Math.Sqrt(predictions.Average(p => Math.Pow(p.Actual - p.Baseline, 2))),
            predictions.Max(p => Math.Abs(p.Actual - p.Predicted)));
        return new(1, "Standardized inverse-distance 7-nearest-neighbour regression", RunHistory.EngineVersion, seed,
            Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(ordered))),
            "Synthetic schema-2 initial states only. Hash-grouped holdout; scaling and constant baseline fit on training rows only. Not a real-world forecast. Use exact completion preview for known exercise histories.",
            model, predictions, evaluation);
    }

    public static double Predict(ForecastModel model, ScenarioDefinition scenario, CancellationToken cancellationToken = default) =>
        PredictFeatures(model, Features(scenario), cancellationToken);

    private static double PredictFeatures(ForecastModel model, double[] features, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (model.Neighbours is < 1 or > 50 || model.Training.Length is < 50 or > 1000 || model.Means.Length != 6 || model.Scales.Length != 6 ||
            model.Means.Any(x => !double.IsFinite(x)) || model.Scales.Any(x => !double.IsFinite(x) || x <= 0) ||
            model.Training.Any(r => r.Features.Length != 6 || r.Features.Any(x => !double.IsFinite(x)) || !double.IsFinite(r.Ticks) || r.Ticks < 0))
            throw new ArgumentException("Invalid forecast model.");
        var neighbours = model.Training.Select(r =>
        {
            token.ThrowIfCancellationRequested();
            return (Row: r, Distance: Math.Sqrt(Enumerable.Range(0, 6).Sum(i => Math.Pow((features[i] - r.Features[i]) / model.Scales[i], 2))));
        }).OrderBy(r => r.Distance).ThenBy(r => r.Row.Group, StringComparer.Ordinal).Take(model.Neighbours).ToArray();
        if (neighbours[0].Distance == 0) return neighbours[0].Row.Ticks;
        return neighbours.Sum(r => r.Row.Ticks / r.Distance) / neighbours.Sum(r => 1 / r.Distance);
    }
}
