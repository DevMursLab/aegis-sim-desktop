using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Aegis.Infrastructure;
using Aegis.Presentation;
using Aegis.Simulation;
using Microsoft.AspNetCore.SignalR.Client;

int passed = 0;
string directory = Path.Combine(Path.GetTempPath(), "aegis-fleet-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
Check("Concurrent dispatch reserves shared demand and stock exactly once", () =>
{
    var s = Demo(); var tick = s.Step();
    Require(s.InTransit == 25 && s.DepotStockRemaining == 23 && tick.Amount == 0 && tick.Actions!.Length == 3);
    Require(s.Vehicles.Select(v => v.State.Load).SequenceEqual(new[] { 10, 10, 5 })); Conserved(s);
});
Check("Simultaneous arrivals deliver once and retain per-vehicle provenance", () =>
{
    var s = Demo(); s.Step(); s.Step(); var e = s.Step();
    Require(e.Amount == 25 && e.Actions!.All(a => a.Kind == "Delivery" && a.Shelter == "Shelter-A"));
    Require(e.Actions!.Select(a => a.VehicleId).Distinct().Count() == 3 && s.Shelters[0].Remaining == 0 && s.Status != RunStatus.Completed); Conserved(s);
});
Check("Three vehicles finish 48 packets at tick eight after returning", () =>
{
    var s = Demo(); Finish(s); Require(s.Tick == 8 && s.DeliveredTotal == 48 && s.DepotStockRemaining == 0 && s.Vehicles.All(v => v.State.Phase == VehiclePhase.AtDepot));
});
Check("Exhaustion waits for every vehicle; restock resumes without duplication", () =>
{
    var s = new SimulationSession(ScenarioDefinition.FleetDemo() with { DepotStock = 30 }); Finish(s);
    Require(s.Status == RunStatus.SupplyExhausted && s.Tick == 8 && s.DeliveredTotal == 30);
    s.Restock(20); Require(s.Status == RunStatus.Paused); Finish(s);
    Require(s.Tick == 12 && s.DeliveredTotal == 48 && s.DepotStockRemaining == 2); Conserved(s);
});
Check("Closed shelter is skipped before fleet dispatch", () =>
{
    var s = Demo(); s.CloseRoute("Shelter-A", 5); var e = s.Step();
    Require(e.Actions!.All(a => a.Shelter != "Shelter-A") && s.InTransit == 23 && s.DepotStockRemaining == 25); Conserved(s);
});
Check("Mid-trip closure freezes all affected vehicles without losing cargo", () =>
{
    var s = Demo(); s.Step(); s.CloseRoute("Shelter-A", 3); var e = s.Step();
    Require(e.Actions!.All(a => a.Kind == "RouteWait") && s.Vehicles.All(v => v.State.TicksRemaining == 2) && s.InTransit == 25);
    Finish(s); Require(s.Status == RunStatus.Completed && s.Tick == 11); Conserved(s);
    var mixed = new SimulationSession(ScenarioDefinition.FleetDemo() with { FleetSize = 2, Shelters = new[] { new ShelterDefinition("A", 5, 400, 200), new ShelterDefinition("B", 20, 700, 200) } });
    mixed.Step(); mixed.CloseRoute("A", 3); mixed.Step(); var unaffected = mixed.Step();
    Require(unaffected.Actions![0].Kind == "RouteWait" && unaffected.Actions[1].Kind == "Delivery" && unaffected.Amount == 10); Conserved(mixed);
});
Check("Mixed travel overrides advance independently", () =>
{
    var scenario = ScenarioDefinition.FleetDemo() with { FleetSize = 2, TravelTicks = 1, Shelters = new[] { new ShelterDefinition("A", 5, 400, 200, 3), new ShelterDefinition("B", 20, 700, 200) } };
    var s = new SimulationSession(scenario); s.Step(); var e = s.Step();
    Require(e.Amount == 10 && s.Vehicles[0].State.TicksRemaining == 2 && s.Vehicles[1].State.Phase == VehiclePhase.Returning); Finish(s); Conserved(s);
});
Check("Zero return still cannot dispatch twice within one tick", () =>
{
    var s = new SimulationSession(ScenarioDefinition.FleetDemo() with { FleetSize = 1, TravelTicks = 1, ReturnTicks = 0 });
    s.Step(); var arrival = s.Step(); Require(arrival.Actions![0].Kind == "Delivery" && s.InTransit == 0 && s.DepotStockRemaining == 38);
    Require(s.Step().Actions![0].Kind == "Dispatched"); Conserved(s);
});
Check("Eight vehicles cannot reserve more than a tiny shelter needs", () =>
{
    var s = new SimulationSession(ScenarioDefinition.FleetDemo() with { FleetSize = 8, Shelters = new[] { new ShelterDefinition("A", 5, 400, 200) } });
    var e = s.Step(); Require(s.InTransit == 5 && e.Actions!.Count(a => a.Kind == "Standby") == 7); Finish(s); Require(s.DeliveredTotal == 5); Conserved(s);
});
Check("One fleet vehicle preserves schema two delivery and lifecycle boundaries", () =>
{
    var old = new SimulationSession(ScenarioDefinition.LogisticsDemo() with { DepotStock = 48 });
    var fleet = new SimulationSession(ScenarioDefinition.FleetDemo() with { FleetSize = 1 });
    while (old.Status != RunStatus.Completed)
    {
        var a = old.Step(); var b = fleet.Step();
        Require(a.Amount == b.Amount && a.Kind == b.Actions![0].Kind && old.Vehicle == fleet.Vehicle && old.Status == fleet.Status && old.DepotStockRemaining == fleet.DepotStockRemaining);
    }
    Require(fleet.Tick == 24);
});
Check("Invalid fleet counts and old engine/schema combinations fail", () =>
{
    foreach (int? count in new int?[] { null, 0, 9 }) Reject(() => new SimulationSession(ScenarioDefinition.FleetDemo() with { FleetSize = count }));
    Reject(() => new SimulationSession(ScenarioDefinition.LogisticsDemo() with { FleetSize = 3 }));
    Reject(() => RunHistory.Replay(RunHistory.New(ScenarioDefinition.FleetDemo()) with { EngineVersion = "2.1.0" }));
});
Check("Reset restores every vehicle and shared inventory", () =>
{
    var s = Demo(); s.Step(); s.Restock(10); s.CloseRoute("Shelter-A", 4); s.Reset();
    Require(s.Tick == 0 && s.DepotStockRemaining == 48 && s.RestockedTotal == 0 && s.Vehicles.All(v => v.State.Phase == VehiclePhase.AtDepot && v.State.Load == 0)); Conserved(s);
});
Check("Bounded fleet/stock/timing matrix conserves every packet at every tick", () =>
{
    foreach (int size in Enumerable.Range(1, 8)) foreach (int stock in new[] { 0, 7, 30, 48 }) foreach (int travel in new[] { 1, 2, 3 })
    {
        var s = new SimulationSession(ScenarioDefinition.FleetDemo() with { FleetSize = size, DepotStock = stock, TravelTicks = travel, ReturnTicks = size % 3 });
        Conserved(s); Finish(s); Conserved(s);
    }
});
Check("Fleet JSON/XML and editor draft round-trip all settings", () =>
{
    var scenario = ScenarioDefinition.FleetDemo(); var draft = new ScenarioDraft(scenario);
    Require(draft.UsesFleet && draft.TryBuild(out var edited, out _) && edited!.FleetSize == 3);
    foreach (string extension in new[] { ".json", ".xml" })
    {
        string path = Path.Combine(directory, "fleet" + extension); ScenarioFiles.Save(path, scenario);
        Require(JsonSerializer.Serialize(ScenarioFiles.Load(path)) == JsonSerializer.Serialize(scenario));
    }
});
Check("SQLite verifies fleet actions and checkpoints at intermediate ticks", () =>
{
    var run = RunHistory.New(ScenarioDefinition.FleetDemo()); var repo = new RunRepository(Path.Combine(directory, "fleet.db")); repo.Save(run);
    for (int i = 0; i < 8; i++)
    {
        run = RunHistory.Append(run, new(Guid.NewGuid(), "Step")); repo.Save(run); var loaded = repo.Load(run.Id);
        Require(RunHistory.Hash(RunHistory.Replay(loaded).Session) == RunHistory.Hash(RunHistory.Replay(run).Session));
        Require(RunHistory.Replay(loaded).Events.All(e => e.Actions!.Length == 3));
    }
});
Check("Original engine 2.1 finite-stock SQL still loads byte-compatible checkpoints", () =>
{
    using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "legacy-v21-finite-run.json")));
    var f = fixture.RootElement; var old = f.GetProperty("run").Deserialize<RunDocument>(ScenarioFiles.JsonOptions)!;
    string hash = f.GetProperty("expectedHash").GetString()!;
    var repo = new RunRepository(Path.Combine(directory, "legacy.db"));
    using (var db = new RunDatabase(repo.DatabasePath))
    {
        db.Runs.Add(new StoredRun { Id = old.Id, ScenarioName = old.Scenario.Name, CreatedAt = old.CreatedAt.ToString("O"), DocumentJson = f.GetProperty("run").GetRawText(), Version = old.Commands.Length, Tick = 24, Status = "Completed", StateHash = hash });
        foreach (var json in f.GetProperty("commandJson").EnumerateArray())
        {
            string text = json.GetString()!; var c = JsonSerializer.Deserialize<RecordedCommand>(text, ScenarioFiles.JsonOptions)!;
            db.Commands.Add(new StoredCommand { RunId = old.Id, Sequence = c.Sequence, CommandId = c.Command.Id, Json = text });
        }
        int tick = 0; foreach (var json in f.GetProperty("eventJson").EnumerateArray()) db.Events.Add(new StoredEvent { RunId = old.Id, Tick = ++tick, Json = json.GetString()! });
        db.Snapshots.Add(new StoredSnapshot { RunId = old.Id, Sequence = old.Commands.Length, Tick = 24, Hash = hash, StateJson = f.GetProperty("checkpointJson").GetString()! }); db.SaveChanges();
    }
    Require(RunHistory.Hash(RunHistory.Replay(repo.Load(old.Id)).Session) == hash);
});
Check("Fleet hash includes all vehicles and returned snapshots cannot mutate the fleet", () =>
{
    var a = Demo(); var b = new SimulationSession(ScenarioDefinition.FleetDemo() with { FleetSize = 4 }); a.Step(); b.Step();
    Require(a.Vehicle == b.Vehicle && RunHistory.Hash(a) != RunHistory.Hash(b));
    var copy = a.Vehicles.ToArray(); copy[1] = copy[1] with { State = new(VehiclePhase.AtDepot, null, 0, 0, 0) };
    Require(a.Vehicles[1].State.Load == 10);
    var previous = System.Globalization.CultureInfo.CurrentCulture;
    try
    {
        System.Globalization.CultureInfo.CurrentCulture = new("ar-SA");
        var localized = Demo(); localized.Step(); Require(RunHistory.Hash(localized) == RunHistory.Hash(a));
    }
    finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
});
Check("ViewModel map/history and preview expose the whole fleet; replay stays read-only", () =>
{
    var vm = new RunViewModel(Demo()); vm.StepCommand.Execute(null); vm.StepCommand.Execute(null);
    Require(vm.MapVehicles.Count == 3 && vm.EventRows.Count == 6 && vm.StockLabel.Contains("In transit: 25") && vm.MapVehicles.All(v => v.Position.X > 170));
    Require(CompletionPreview.Calculate(vm.Document).EndTick == 8);
    vm.LoadReplay(vm.Document); vm.ReplayTick = 0; Require(vm.EventRows.Count == 0 && !vm.StepCommand.CanExecute(null));
});
Check("Fleet metrics and reports distinguish elapsed ticks from vehicle-ticks", () =>
{
    var run = RunHistory.New(ScenarioDefinition.FleetDemo()); for (int i = 0; i < 8; i++) run = RunHistory.Append(run, new(Guid.NewGuid(), "Step"));
    var metrics = RunMetrics.Calculate(run); Require(metrics.Ticks == 8 && metrics.Delivered == 48 && metrics.OutboundTicks == 12 && metrics.ReturnTicks == 6 && metrics.ActiveTickPercent == 25);
    Reports.Export(directory, run);
    string csv = File.ReadAllText(Path.Combine(directory, "run-" + run.Id.ToString("N") + "-vehicles.csv"));
    Require(csv.Contains("Vehicle-03") && csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length == 25);
    using var diagnostic = JsonDocument.Parse(Diagnostics.Create(run));
    Require(diagnostic.RootElement.GetProperty("Vehicles").GetArrayLength() == 3);
});

string root = AppContext.BaseDirectory; while (!File.Exists(Path.Combine(root, "Aegis.Sim.slnx"))) root = Directory.GetParent(root)?.FullName ?? throw new Exception("Repository not found");
var socket = new TcpListener(IPAddress.Loopback, 0); socket.Start(); int port = ((IPEndPoint)socket.LocalEndpoint).Port; socket.Stop();
string hostData = Path.Combine(directory, "host"); string scenarioFile = Path.Combine(directory, "network.json"); ScenarioFiles.Save(scenarioFile, ScenarioDefinition.FleetDemo());
var start = new ProcessStartInfo(Path.Combine(root, "src", "Aegis.SyncHost", "bin", "Release", "net10.0", "Aegis.SyncHost.exe")) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
foreach (string arg in new[] { "--urls", $"http://127.0.0.1:{port}", "--data-dir", hostData, "--scenario", scenarioFile }) start.ArgumentList.Add(arg);
using var host = Process.Start(start)!; host.OutputDataReceived += (_, _) => { }; host.ErrorDataReceived += (_, _) => { }; host.BeginOutputReadLine(); host.BeginErrorReadLine();
try
{
    using var http = new HttpClient(); var watch = Stopwatch.StartNew();
    while (true) { try { if ((await http.GetAsync($"http://127.0.0.1:{port}/health")).IsSuccessStatusCode) break; } catch { } if (watch.Elapsed > TimeSpan.FromSeconds(25)) throw new TimeoutException(); await Task.Delay(100); }
    using var secrets = JsonDocument.Parse(File.ReadAllText(Path.Combine(hostData, "session-secrets.json")));
    HubConnection Connect(string role) => new HubConnectionBuilder().WithUrl($"http://127.0.0.1:{port}/exercise", o => o.AccessTokenProvider = () => Task.FromResult<string?>(secrets.RootElement.GetProperty(role).GetString())).Build();
    await using var controller = Connect("Controller"); await using var observer = Connect("Observer"); await controller.StartAsync(); await observer.StartAsync();
    var state = await controller.InvokeAsync<RoomSnapshot>("Snapshot");
    for (int i = 0; i < 8; i++) state = await controller.InvokeAsync<RoomSnapshot>("Submit", new HostCommandRequest(state.Run.Id, state.Run.Commands.Length, new(Guid.NewGuid(), "Step")));
    var seen = await observer.InvokeAsync<RoomSnapshot>("Snapshot"); var result = RunHistory.Replay(seen.Run);
    Require(result.Session.Tick == 8 && result.Session.DeliveredTotal == 48 && result.Session.Vehicles.Count == 3 && result.Events.All(e => e.Actions!.Length == 3));
    Require(RunHistory.Hash(result.Session) == RunHistory.Hash(RunHistory.Replay(state.Run).Session));
    Pass("Real authoritative host and observer agree on every vehicle and fleet history");
}
finally { if (!host.HasExited) host.Kill(true); }
Console.WriteLine($"All {passed} fleet checks passed. Evidence: {directory}");

static SimulationSession Demo() => new(ScenarioDefinition.FleetDemo());
static void Finish(SimulationSession s) { while (s.Status is not (RunStatus.Completed or RunStatus.SupplyExhausted)) { Require(s.Tick < 500); s.Step(); Conserved(s); } }
static void Conserved(SimulationSession s)
{
    Require(s.DepotStockRemaining >= 0 && s.DeliveredTotal + s.InTransit + s.DepotStockRemaining == s.Scenario.DepotStock + s.RestockedTotal);
    Require(s.Vehicles.All(v => v.State.Load >= 0 && v.State.Load <= s.Capacity && (v.State.Phase == VehiclePhase.Outbound || v.State.Load == 0)));
    foreach (var shelter in s.Shelters) Require(shelter.Remaining >= s.Vehicles.Where(v => v.State.Phase == VehiclePhase.Outbound && v.State.Destination == shelter.Name).Sum(v => v.State.Load));
}
static void Require(bool value) { if (!value) throw new Exception("Check failed"); }
static void Reject(Action action) { try { action(); } catch (ArgumentException) { return; } catch (InvalidDataException) { return; } throw new Exception("Expected rejection"); }
void Check(string name, Action action) { action(); Pass(name); }
void Pass(string name) { passed++; Console.WriteLine("PASS: " + name); }
