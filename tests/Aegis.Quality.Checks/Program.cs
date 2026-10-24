using System.Text.Json;
using Aegis.Infrastructure;
using Aegis.Simulation;
using Aegis.Presentation;

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
Check("Editor copies both scenario schemas without changing their settings", () =>
{
    foreach (var scenario in new[] { ScenarioDefinition.FloodDemo(), ScenarioDefinition.LogisticsDemo() })
    {
        var draft = new ScenarioDraft(scenario); Require(draft.TryBuild(out var built, out _));
        Require(JsonSerializer.Serialize(scenario) == JsonSerializer.Serialize(built));
    }
});
Check("Draft edits do not mutate source or prior validated snapshots", () =>
{
    var source = ScenarioDefinition.LogisticsDemo(); var draft = new ScenarioDraft(source);
    Require(draft.TryBuild(out var first, out _)); draft.Shelters[0].Demand = "1";
    Require(source.Shelters[0].Demand == 25 && first!.Shelters[0].Demand == 25);
    Require(draft.TryBuild(out var second, out _) && second!.Shelters[0].Demand == 1);
});
Check("Editor reports partial and malformed numeric fields without throwing", () =>
{
    foreach (string value in new[] { "", "-", "1.5", "abc", "2147483648" })
    {
        var draft = new ScenarioDraft(ScenarioDefinition.FloodDemo()) { Capacity = value };
        Require(!draft.TryBuild(out var built, out string error) && built == null && error.Contains("Capacity"));
    }
});
Check("Editor rejects duplicate shelter names and total demand overflow", () =>
{
    var draft = new ScenarioDraft(ScenarioDefinition.FloodDemo()); draft.Shelters[1].Name = " shelter-a ";
    Require(!draft.TryBuild(out _, out _)); draft.Shelters[1].Name = "B"; draft.Shelters[0].Demand = "10000";
    Require(!draft.TryBuild(out _, out _));
});
Check("Editor rejects nonfinite and out-of-bounds coordinates", () =>
{
    foreach (string coordinate in new[] { "NaN", "Infinity", "49", "1051", "1,000" })
    {
        var draft = new ScenarioDraft(ScenarioDefinition.FloodDemo()); draft.Shelters[0].X = coordinate;
        Require(!draft.TryBuild(out _, out _));
    }
});
Check("Editor preserves timing overrides and validates their bounds", () =>
{
    var draft = new ScenarioDraft(ScenarioDefinition.LogisticsDemo()); draft.Shelters[0].TravelTicks = "3";
    Require(draft.TryBuild(out var built, out _) && built!.Shelters[0].TravelTicks == 3);
    draft.Shelters[0].TravelTicks = "21"; Require(!draft.TryBuild(out _, out _));
});
Check("Legacy mode ignores disabled logistics fields without changing old rules", () =>
{
    var draft = new ScenarioDraft(ScenarioDefinition.LogisticsDemo()) { UsesLogistics = false, DepotStock = "bad", TravelTicks = "bad" };
    Require(draft.TryBuild(out var built, out _) && built!.SchemaVersion == 1 && built.DepotStock == null && built.TravelTicks == 1 && built.ReturnTicks == 0);
});
Check("Adding shelters uses unique names and respects the 64-row limit", () =>
{
    var draft = new ScenarioDraft(ScenarioDefinition.FloodDemo());
    while (draft.Shelters.Count < 64) Require(draft.AddShelter());
    Require(!draft.AddShelter() && draft.Shelters.Select(s => s.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 64);
});
Check("Editor accepts empty demand, zero stock and return zero", () =>
{
    var draft = new ScenarioDraft(ScenarioDefinition.LogisticsDemo()) { DepotStock = "0", ReturnTicks = "0" };
    draft.Shelters.Clear(); Require(draft.TryBuild(out var built, out _));
    Require(new SimulationSession(built!).Status == RunStatus.Completed);
});
Check("Editor parsing is invariant under a comma-decimal user culture", () =>
{
    var previous = System.Globalization.CultureInfo.CurrentCulture;
    try
    {
        System.Globalization.CultureInfo.CurrentCulture = new("de-DE");
        var draft = new ScenarioDraft(ScenarioDefinition.FloodDemo()); draft.Shelters[0].X = "480.5";
        Require(draft.TryBuild(out var built, out _) && built!.Shelters[0].X == 480.5);
    }
    finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
});
Check("Edited scenario JSON/XML saves reload with the same deterministic outcome", () =>
{
    string folder = Path.Combine(Path.GetTempPath(), "aegis-editor-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
    var draft = new ScenarioDraft(ScenarioDefinition.LogisticsDemo()) { DepotStock = "48" };
    Require(draft.TryBuild(out var built, out _));
    foreach (string extension in new[] { ".json", ".xml" })
    {
        string path = Path.Combine(folder, "edited" + extension); ScenarioFiles.Save(path, built!);
        var loaded = ScenarioFiles.Load(path); Require(CompletionPreview.Calculate(RunHistory.New(loaded)).EndTick == 24);
    }
});
Check("Editor reports invalid names and notifies model-mode changes", () =>
{
    var draft = new ScenarioDraft(ScenarioDefinition.FloodDemo()); bool notified = false;
    draft.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(ScenarioDraft.UsesLogistics)) notified = true; };
    draft.UsesLogistics = true; Require(notified); draft.Name = "bad\nname"; Require(!draft.TryBuild(out _, out _));
});
Console.WriteLine($"All {passed} quality checks passed.");

static string Collection(string type, object coordinates) => JsonSerializer.Serialize(new { type = "FeatureCollection", features = new[] { new { type = "Feature", id = "x", properties = new { name = "Synthetic feature" }, geometry = new { type, coordinates } } } });
static void Require(bool value) { if (!value) throw new Exception("Check failed"); }
static void Reject(Action action) { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Expected rejection"); }
void Check(string name, Action action) { action(); passed++; Console.WriteLine("PASS: " + name); }
