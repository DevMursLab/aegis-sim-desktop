using System.Text.Json;
using Aegis.Infrastructure;
using Aegis.Simulation;

int passed = 0;
string sample = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "synthetic-region.geojson"));
Check("Offline sample loads seven mixed geometry features", () => Require(GeoJsonLayer.Parse(sample).Features.Count == 7));
Check("Projection fits the world canvas and keeps north up", () =>
{
    var layer = GeoJsonLayer.Parse(sample);
    var points = layer.Features.SelectMany(f => f.Parts.SelectMany(p => p)).ToArray();
    Require(points.All(p => p.X >= 80 && p.X <= 1120 && p.Y >= 80 && p.Y <= 720));
    Require(layer.Features.Single(f => f.Id == "shelter-a").Parts[0][0].Y < layer.Features.Single(f => f.Id == "shelter-b").Parts[0][0].Y);
});
Check("Single point has finite centered display position", () =>
{
    var point = GeoJsonLayer.Parse(Collection("Point", new[] { 20.0, 30.0 })).Features[0].Parts[0][0];
    Require(point == new LayerPoint(600, 400));
});
Check("Reject planar meters and invalid latitude", () =>
{
    Reject(() => GeoJsonLayer.Parse(Collection("Point", new[] { 1200.0, 800.0 })));
    Reject(() => GeoJsonLayer.Parse(Collection("Point", new[] { 20.0, 90.0 })));
});
Check("Reject legacy CRS and duplicate JSON members", () =>
{
    Reject(() => GeoJsonLayer.Parse(sample.Replace("\"features\":", "\"crs\":{},\"features\":")));
    Reject(() => GeoJsonLayer.Parse(sample.Replace("\"type\": \"FeatureCollection\"", "\"type\":\"FeatureCollection\",\"type\":\"FeatureCollection\"")));
});
Check("Reject unknown geometry, null geometry and malformed input", () =>
{
    Reject(() => GeoJsonLayer.Parse(Collection("MultiPoint", new[] { new[] { 0.0, 0.0 } })));
    Reject(() => GeoJsonLayer.Parse("{\"type\":\"FeatureCollection\",\"features\":[{\"type\":\"Feature\",\"id\":\"x\",\"properties\":{\"name\":\"x\"},\"geometry\":null}]}"));
    Reject(() => GeoJsonLayer.Parse("{"));
});
Check("Reject duplicate IDs and oversized payload", () =>
{
    Reject(() => GeoJsonLayer.Parse(sample.Replace("road-02", "road-01")));
    Reject(() => GeoJsonLayer.Parse(new string(' ', 524289)));
});
Check("Reject open, degenerate and self-crossing polygon rings", () =>
{
    foreach (var ring in new[] {
        new[] { new[] {0.0,0.0}, new[] {1.0,0.0}, new[] {1.0,1.0}, new[] {0.0,1.0} },
        new[] { new[] {0.0,0.0}, new[] {0.5,0.0}, new[] {1.0,0.0}, new[] {0.0,0.0} },
        new[] { new[] {0.0,0.0}, new[] {1.0,1.0}, new[] {0.0,1.0}, new[] {1.0,0.0}, new[] {0.0,0.0} } })
        Reject(() => GeoJsonLayer.Parse(Collection("Polygon", new[] { ring })));
});
Check("Reject distant regions and antimeridian crossings", () =>
{
    Reject(() => GeoJsonLayer.Parse(Collection("LineString", new[] { new[] { 0.0, 0.0 }, new[] { 10.0, 1.0 } })));
    Reject(() => GeoJsonLayer.Parse(Collection("LineString", new[] { new[] { -179.9, 0.0 }, new[] { 179.9, 0.0 } })));
});
Check("Reject excess vertices and empty collections", () =>
{
    Reject(() => GeoJsonLayer.Parse(Collection("LineString", Enumerable.Range(0, 4097).Select(i => new[] { i * 0.0001, 0.0 }).ToArray())));
    Reject(() => GeoJsonLayer.Parse("{\"type\":\"FeatureCollection\",\"features\":[]}"));
});
var old = RunHistory.New(ScenarioDefinition.FloodDemo());
Check("Legacy preview completes at the real six-tick boundary", () =>
{
    var projection = CompletionPreview.Calculate(old);
    Require(projection.Outcome == "Completed" && projection.EndTick == 6 && projection.Delivered == 48 && projection.Remaining == 0);
});
Check("Preview accounts for route closure expiry", () =>
{
    var changed = RunHistory.Append(old, new(Guid.NewGuid(), "CloseRoute", "Shelter-A", 5));
    Require(CompletionPreview.Calculate(changed).EndTick == 8);
});
var stock = RunHistory.New(ScenarioDefinition.LogisticsDemo());
Check("Finite stock projection explains unmet supply", () =>
{
    var result = CompletionPreview.Calculate(stock);
    Require(result.Outcome == "SupplyExhausted" && result.EndTick == 16 && result.AdditionalSupplyRequired == 18 && result.Stock == 0);
});
Check("Restocked projection matches direct simulation hash", () =>
{
    var run = stock;
    for (int i = 0; i < 16; i++) run = RunHistory.Append(run, new(Guid.NewGuid(), "Step"));
    run = RunHistory.Append(run, new(Guid.NewGuid(), "Restock", Amount: 20));
    var result = CompletionPreview.Calculate(run);
    var direct = RunHistory.Replay(run).Session;
    while (direct.Status != RunStatus.Completed) direct.Step();
    Require(result.EndTick == 24 && result.Stock == 2 && result.FinalHash == RunHistory.Hash(direct));
});
Check("Preview does not append commands or mutate the source", () =>
{
    string before = JsonSerializer.Serialize(stock); CompletionPreview.Calculate(stock);
    Require(before == JsonSerializer.Serialize(stock) && stock.Commands.Length == 0);
});
Check("Replay prefix preview ignores later user interventions", () =>
{
    var run = RunHistory.Append(old, new(Guid.NewGuid(), "Step"));
    run = RunHistory.Append(run, new(Guid.NewGuid(), "CloseRoute", "Shelter-A", 5));
    run = RunHistory.Append(run, new(Guid.NewGuid(), "Step"));
    var result = CompletionPreview.Calculate(run, 0);
    Require(result.StartTick == 0 && result.EndTick == 6);
});
Check("Cancellation and tampered history fail without a result", () =>
{
    var cancellation = new CancellationTokenSource(); cancellation.Cancel();
    try { CompletionPreview.Calculate(old, cancellationToken: cancellation.Token); throw new Exception("Cancellation ignored"); } catch (OperationCanceledException) { }
    var run = RunHistory.Append(old, new(Guid.NewGuid(), "Step"));
    Reject(() => CompletionPreview.Calculate(run with { Commands = new[] { run.Commands[0] with { StateHash = "bad" } } }));
});
Check("Tick limit is reported instead of looping forever", () =>
{
    var large = new ScenarioDefinition(2, "Bounded projection", 1, new[] { new ShelterDefinition("A", 10000, 400, 400) }, 10000, 20, 20);
    var result = CompletionPreview.Calculate(RunHistory.New(large));
    Require(result.Outcome == "TickLimit" && result.EndTick == 20000 && result.Remaining > 0);
});
Check("Diagnostics contain verified identity and exclude scenario text", () =>
{
    var source = RunHistory.New(ScenarioDefinition.FloodDemo() with { Name = "PRIVATE-SCENARIO-CANARY" });
    string text = Diagnostics.Create(source);
    using var json = JsonDocument.Parse(text);
    Require(json.RootElement.GetProperty("StateHash").GetString() == RunHistory.Hash(RunHistory.Replay(source).Session));
    Require(!text.Contains("PRIVATE-SCENARIO-CANARY") && !text.Contains("session-secrets") && json.RootElement.GetProperty("Commands").GetInt32() == 0);
});
Check("Completed and zero-demand projections do not invent extra ticks", () =>
{
    var run = old;
    for (int i = 0; i < 6; i++) run = RunHistory.Append(run, new(Guid.NewGuid(), "Step"));
    Require(CompletionPreview.Calculate(run).AdditionalTicks == 0);
    var empty = ScenarioDefinition.FloodDemo() with { Shelters = Array.Empty<ShelterDefinition>() };
    Require(CompletionPreview.Calculate(RunHistory.New(empty)).EndTick == 0);
});
Console.WriteLine($"All {passed} quality checks passed.");

static string Collection(string type, object coordinates) => JsonSerializer.Serialize(new { type = "FeatureCollection", features = new[] { new { type = "Feature", id = "x", properties = new { name = "Synthetic feature" }, geometry = new { type, coordinates } } } });
static void Require(bool value) { if (!value) throw new Exception("Check failed"); }
static void Reject(Action action) { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Expected rejection"); }
void Check(string name, Action action) { action(); passed++; Console.WriteLine("PASS: " + name); }
