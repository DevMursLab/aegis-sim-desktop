using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aegis.Infrastructure;
using Aegis.Presentation;
using Aegis.Simulation;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Data.Sqlite;

int passed = 0;
string directory = Path.Combine(Path.GetTempPath(), "aegis-release-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
string sample = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "road-exercise.geojson"));
ScenarioDefinition Demo() => GeoExercise.Parse(sample);
Check("Road GeoJSON compiles self-contained routes and distance-derived ticks", () =>
{
    var s = Demo(); Require(s.SchemaVersion == 4 && s.Geography!.Roads.Length == 6 && s.Geography.Routes.Select(r => r.Meters).SequenceEqual(new[] { 890, 890, 1335 }));
    Require(s.Shelters.Select(x => x.TravelTicks).SequenceEqual(new int?[] { 3, 3, 5 }));
});
Check("Shortest paths bypass the deliberately longer detour", () =>
{
    var route = Demo().Geography!.Routes[0]; Require(route.RoadIds.SequenceEqual(new[] { "road-01", "road-02" }) && !route.RoadIds.Contains("road-99"));
});
Check("Undirected roads support reversed stored geometry", () =>
{
    var root = JsonNode.Parse(sample)!; var road = root["features"]!.AsArray().Single(f => f!["id"]!.GetValue<string>() == "road-01")!;
    road["properties"]!["from"] = "junction"; road["properties"]!["to"] = "depot";
    var coordinates = road["geometry"]!["coordinates"]!.AsArray(); var reversed = new JsonArray(coordinates.Reverse().Select(c => c!.DeepClone()).ToArray()); road["geometry"]!["coordinates"] = reversed;
    Require(GeoExercise.Parse(root.ToJsonString()).Geography!.Routes[0].Meters == 890);
});
Check("Equal-length route choice is independent of road-feature order", () =>
{
    var root = JsonNode.Parse(sample)!; var array = root["features"]!.AsArray(); root["features"] = new JsonArray(array.Reverse().Select(n => n!.DeepClone()).ToArray());
    var expected = Demo().Geography!.Routes.Single(r => r.Shelter == "Road Shelter-C"); var actual = GeoExercise.Parse(root.ToJsonString()).Geography!.Routes.Single(r => r.Shelter == expected.Shelter);
    Require(actual.RoadIds.SequenceEqual(expected.RoadIds));
});
Check("Spherical distance uses meters and positive segment lengths", () =>
{
    Require(Math.Abs(GeoExercise.Distance(0, 0, 0.001, 0) - 111.195) < 0.1);
    Require(GeoExercise.Distance(0, 0, 0, 0) == 0);
});
Check("Route distance controls both outward and return duration", () =>
{
    var s = new SimulationSession(Demo()); while (s.Status != RunStatus.Completed) { Require(s.Tick < 50); s.Step(); Conserved(s); }
    Require(s.Tick == 14 && s.DeliveredTotal == 30 && s.Vehicles.All(v => v.State.Phase == VehiclePhase.AtDepot));
});
Check("Map vehicles follow the road polyline rather than a depot-to-shelter chord", () =>
{
    var vm = new RunViewModel(new SimulationSession(Demo())); vm.StepCommand.Execute(null); vm.StepCommand.Execute(null);
    var first = vm.MapVehicles[0]; Require(first.Destination == "Road Shelter-B" && Math.Abs(first.Position.Y - vm.DepotY) < 0.0001 && first.Position.X > vm.DepotX);
    Require(!vm.CanEditScenario && vm.SelectedDetail.Length > 0);
});
Check("Unknown endpoints and unreachable shelters fail before starting a run", () =>
{
    Reject(() => GeoExercise.Parse(sample.Replace("\"from\": \"depot\"", "\"from\": \"unknown\"")));
    var root = JsonNode.Parse(sample)!; var a = root["features"]!.AsArray();
    foreach (var f in a.Where(f => f!["id"]!.GetValue<string>() is "road-01" or "road-99").ToArray()) a.Remove(f);
    Reject(() => GeoExercise.Parse(root.ToJsonString()));
});
Check("Geometry endpoint mismatches and missing exercise metadata are rejected", () =>
{
    Reject(() => GeoExercise.Parse(sample.Replace("[[0,0],[0.004,0]]", "[[0.001,0],[0.004,0]]")));
    var root = JsonNode.Parse(sample)!; root.AsObject().Remove("exercise"); Reject(() => GeoExercise.Parse(root.ToJsonString()));
});
Check("Invalid timing and route metadata cannot silently change model behavior", () =>
{
    Reject(() => GeoExercise.Parse(sample.Replace("\"metersPerTick\": 300", "\"metersPerTick\": 1")));
    var s = Demo(); var map = s.Geography!;
    Reject(() => (s with { Geography = map with { Routes = map.Routes.Select((r, i) => i == 0 ? r with { Meters = r.Meters + 1 } : r).ToArray() } }).Validate());
    Reject(() => RunHistory.Replay(RunHistory.New(s) with { EngineVersion = "2.5.0" }));
});
Check("Road metadata is deep-copied and enters the state hash", () =>
{
    var source = Demo(); var run = RunHistory.New(source); var before = RunHistory.Hash(RunHistory.Replay(run).Session);
    source.Geography!.Roads[0].Points[0] = new(50, 70); Require(RunHistory.Hash(RunHistory.Replay(run).Session) == before);
    var changed = run.Scenario with { Geography = run.Scenario.Geography! with { SourceHash = new string('0', 64) } };
    Require(RunHistory.Hash(new SimulationSession(changed)) != before);
});
Check("Compiled geographical JSON/XML round-trip without external map files", () =>
{
    var s = Demo();
    foreach (string extension in new[] { ".json", ".xml" })
    {
        string path = Path.Combine(directory, "geographical" + extension); ScenarioFiles.Save(path, s); var loaded = ScenarioFiles.Load(path);
        Require(JsonSerializer.Serialize(loaded) == JsonSerializer.Serialize(s) && CompletionPreview.Calculate(RunHistory.New(loaded)).EndTick == 14);
    }
});
Check("Geographical SQL checkpoints, replay and reports preserve source provenance", () =>
{
    var repo = new RunRepository(Path.Combine(directory, "geo.db")); var run = RunHistory.New(Demo());
    for (int i = 0; i < 14; i++) { run = RunHistory.Append(run, new(Guid.NewGuid(), "Step")); repo.Save(run); }
    var loaded = repo.Load(run.Id); Require(RunHistory.Replay(loaded).Session.Tick == 14);
    Reports.Export(directory, loaded); Require(File.ReadAllText(Path.Combine(directory, "run-" + run.Id.ToString("N") + ".html")).Contains("890 m"));
});
Check("Original engine 2.5 priority archive still loads normalized SQL payloads", () =>
{
    using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "legacy-v25-priority-run.json")));
    var f = fixture.RootElement; var old = f.GetProperty("run").Deserialize<RunDocument>(ScenarioFiles.JsonOptions)!; string hash = f.GetProperty("expectedHash").GetString()!;
    var repo = new RunRepository(Path.Combine(directory, "legacy.db"));
    using (var db = new RunDatabase(repo.DatabasePath))
    {
        db.Runs.Add(new StoredRun { Id = old.Id, ScenarioName = old.Scenario.Name, CreatedAt = old.CreatedAt.ToString("O"), DocumentJson = f.GetProperty("run").GetRawText(), Version = old.Commands.Length, Tick = 4, Status = "SupplyExhausted", StateHash = hash });
        foreach (var item in f.GetProperty("commandJson").EnumerateArray()) { string text = item.GetString()!; var c = JsonSerializer.Deserialize<RecordedCommand>(text, ScenarioFiles.JsonOptions)!; db.Commands.Add(new StoredCommand { RunId = old.Id, Sequence = c.Sequence, CommandId = c.Command.Id, Json = text }); }
        int tick = 0; foreach (var item in f.GetProperty("eventJson").EnumerateArray()) db.Events.Add(new StoredEvent { RunId = old.Id, Tick = ++tick, Json = item.GetString()! });
        db.Snapshots.Add(new StoredSnapshot { RunId = old.Id, Sequence = old.Commands.Length, Tick = 4, Hash = hash, StateJson = f.GetProperty("checkpointJson").GetString()! }); db.SaveChanges();
    }
    Require(RunHistory.Hash(RunHistory.Replay(repo.Load(old.Id)).Session) == hash);
});
Check("Database upgrade makes a consistent backup and preserves saved runs", () =>
{
    string path = Path.Combine(directory, "upgrade.db"); var repo = new RunRepository(path); var run = RunHistory.New(ScenarioDefinition.FloodDemo()); repo.Save(run);
    Sql(path, "PRAGMA user_version=0; DROP TABLE SchemaMigrations;");
    var upgraded = new RunRepository(path); Require(upgraded.UpgradeBackupPath != null && File.Exists(upgraded.UpgradeBackupPath));
    Require(Scalar(path, "PRAGMA user_version") == 1 && Scalar(upgraded.UpgradeBackupPath!, "PRAGMA user_version") == 0 && upgraded.Load(run.Id).Id == run.Id);
});
Check("Repeated initialization is idempotent and does not create repeated backups", () =>
{
    string path = Path.Combine(directory, "upgrade.db"); var repo = new RunRepository(path);
    Require(repo.UpgradeBackupPath == null && Scalar(path, "SELECT COUNT(*) FROM SchemaMigrations") == 1 && Directory.GetFiles(directory, "upgrade.db.backup-*.sqlite").Length == 1);
});
Check("Failed legacy backfill rolls back the schema upgrade", () =>
{
    string path = Path.Combine(directory, "failed-upgrade.db"); var repo = new RunRepository(path); repo.Save(RunHistory.New(ScenarioDefinition.FloodDemo()));
    Sql(path, "PRAGMA user_version=0; DROP TABLE SchemaMigrations; DROP TABLE Events; UPDATE Runs SET DocumentJson='{';");
    Reject(() => new RunRepository(path));
    Require(Scalar(path, "PRAGMA user_version") == 0 && Scalar(path, "SELECT COUNT(*) FROM sqlite_master WHERE name='Events'") == 0);
});
Check("Newer and unrelated database schemas are refused without rewriting them", () =>
{
    string path = Path.Combine(directory, "future.db"); _ = new RunRepository(path); Sql(path, "PRAGMA user_version=99;"); Reject(() => new RunRepository(path)); Require(Scalar(path, "PRAGMA user_version") == 99);
    string other = Path.Combine(directory, "other.db"); Sql(other, "CREATE TABLE Other (Id INTEGER);"); Reject(() => new RunRepository(other)); Require(Scalar(other, "SELECT COUNT(*) FROM sqlite_master WHERE name='Runs'") == 0);
});
Check("Service closure freezes the assigned trip without rewriting shortest paths", () =>
{
    var s = new SimulationSession(Demo()); s.Step(); var before = s.Vehicle; s.CloseRoute("Road Shelter-B", 2); s.Step();
    Require(s.Vehicle.TicksRemaining == before.TicksRemaining && s.Vehicles[1].State.TicksRemaining == 4); Conserved(s);
});

string rootPath = AppContext.BaseDirectory; while (!File.Exists(Path.Combine(rootPath, "Aegis.Sim.slnx"))) rootPath = Directory.GetParent(rootPath)?.FullName ?? throw new Exception("Repository not found");
var socket = new TcpListener(IPAddress.Loopback, 0); socket.Start(); int port = ((IPEndPoint)socket.LocalEndpoint).Port; socket.Stop();
string hostData = Path.Combine(directory, "host"); string scenarioPath = Path.Combine(directory, "network.geojson"); File.WriteAllText(scenarioPath, sample);
var start = new ProcessStartInfo(Path.Combine(rootPath, "src", "Aegis.SyncHost", "bin", "Release", "net10.0", "Aegis.SyncHost.exe")) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
foreach (string arg in new[] { "--urls", $"http://127.0.0.1:{port}", "--data-dir", hostData, "--scenario", scenarioPath }) start.ArgumentList.Add(arg);
using var host = Process.Start(start)!; host.OutputDataReceived += (_, _) => { }; host.ErrorDataReceived += (_, _) => { }; host.BeginOutputReadLine(); host.BeginErrorReadLine();
try
{
    using var http = new HttpClient(); var watch = Stopwatch.StartNew();
    while (true) { try { if ((await http.GetAsync($"http://127.0.0.1:{port}/health")).IsSuccessStatusCode) break; } catch { } if (watch.Elapsed > TimeSpan.FromSeconds(25)) throw new TimeoutException(); await Task.Delay(100); }
    using var secrets = JsonDocument.Parse(File.ReadAllText(Path.Combine(hostData, "session-secrets.json")));
    HubConnection Connect(string role) => new HubConnectionBuilder().WithUrl($"http://127.0.0.1:{port}/exercise", o => o.AccessTokenProvider = () => Task.FromResult<string?>(secrets.RootElement.GetProperty(role).GetString())).Build();
    await using var controller = Connect("Controller"); await using var observer = Connect("Observer"); await controller.StartAsync(); await observer.StartAsync();
    var state = await controller.InvokeAsync<RoomSnapshot>("Snapshot"); for (int i = 0; i < 14; i++) state = await controller.InvokeAsync<RoomSnapshot>("Submit", new HostCommandRequest(state.Run.Id, state.Run.Commands.Length, new(Guid.NewGuid(), "Step")));
    var seen = await observer.InvokeAsync<RoomSnapshot>("Snapshot"); var result = RunHistory.Replay(seen.Run);
    Require(result.Session.Status == RunStatus.Completed && result.Session.Tick == 14 && seen.Run.Scenario.Geography!.Roads.Length == 6 && RunHistory.Hash(result.Session) == RunHistory.Hash(RunHistory.Replay(state.Run).Session));
    Pass("Authoritative host and observer reconstruct the same road-derived exercise");
}
finally { if (!host.HasExited) host.Kill(true); }
Console.WriteLine($"All {passed} release checks passed. Evidence: {directory}");

static void Conserved(SimulationSession s) => Require(s.DeliveredTotal + s.InTransit + s.DepotStockRemaining == s.Scenario.DepotStock + s.RestockedTotal && s.Shelters.All(n => n.Remaining >= 0));
static void Require(bool value) { if (!value) throw new Exception("Check failed"); }
static void Reject(Action action) { try { action(); } catch (Exception e) when (e is ArgumentException or InvalidDataException or JsonException) { return; } throw new Exception("Expected rejection"); }
static void Sql(string path, string text) { using var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString()); db.Open(); using var c = db.CreateCommand(); c.CommandText = text; c.ExecuteNonQuery(); }
static long Scalar(string path, string text) { using var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString()); db.Open(); using var c = db.CreateCommand(); c.CommandText = text; return Convert.ToInt64(c.ExecuteScalar()); }
void Check(string name, Action action) { action(); Pass(name); }
void Pass(string name) { passed++; Console.WriteLine("PASS: " + name); }
