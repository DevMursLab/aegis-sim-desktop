using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Aegis.Infrastructure;
using Aegis.Presentation;
using Aegis.Simulation;
using Microsoft.AspNetCore.SignalR.Client;

int passed = 0;
string directory = Path.Combine(Path.GetTempPath(), "aegis-priority-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
Check("Highest priority receives scarce stock first with an explicit dispatch trace", () =>
{
    var s = Demo(); var e = s.Step();
    Require(e.Actions!.Select(a => a.Shelter).SequenceEqual(new[] { "Shelter-C", "Shelter-C", "Shelter-B" }));
    Require(s.Vehicles.Select(v => v.State.Load).SequenceEqual(new[] { 10, 8, 2 }) && s.DepotStockRemaining == 0);
    Require(e.Actions![0].Reason!.Contains("Priority 9") && e.Actions![0].Reason!.Contains("loaded 10")); Conserved(s);
});
Check("Scarcity protects urgent demand; restock eventually serves lower priorities", () =>
{
    var s = Demo(); Finish(s);
    Require(s.Tick == 4 && s.Status == RunStatus.SupplyExhausted && s.Shelters[2].Remaining == 0 && s.Shelters[1].Remaining == 3 && s.Shelters[0].Remaining == 25);
    s.Restock(28); Finish(s); Require(s.Tick == 12 && s.DeliveredTotal == 48 && s.DepotStockRemaining == 0); Conserved(s);
});
Check("Zero priority keeps the original fleet dispatch order and omits trace fields", () =>
{
    var s = new SimulationSession(ScenarioDefinition.FleetDemo()); var e = s.Step();
    Require(e.Actions!.All(a => a.Shelter == "Shelter-A" && a.Reason == null));
    Require(!JsonSerializer.Serialize(e, ScenarioFiles.JsonOptions).Contains("reason"));
});
Check("Equal priority ties preserve source-list order", () =>
{
    var scenario = ScenarioDefinition.FleetDemo() with { Shelters = new[] { new ShelterDefinition("Z", 5, 400, 200, Priority: 5), new ShelterDefinition("A", 5, 700, 200, Priority: 5), new ShelterDefinition("C", 10, 400, 500) } };
    var s = new SimulationSession(scenario); Require(s.Step().Actions!.Select(a => a.Shelter).SequenceEqual(new[] { "Z", "A", "C" }));
});
Check("Closed urgent route is excluded without blocking available lower priorities", () =>
{
    var s = Demo(); s.CloseRoute("Shelter-C", 3); var e = s.Step();
    Require(e.Actions!.All(a => a.Shelter != "Shelter-C") && e.Actions![0].Shelter == "Shelter-B" && s.InTransit == 20); Conserved(s);
});
Check("Reserved urgent cargo is deducted before the next vehicle chooses its target", () =>
{
    var scenario = ScenarioDefinition.PriorityDemo() with { Shelters = new[] { new ShelterDefinition("Low", 25, 400, 200), new ShelterDefinition("Medium", 5, 700, 200, Priority: 5), new ShelterDefinition("High", 5, 400, 500, Priority: 9) } };
    var s = new SimulationSession(scenario); Require(s.Step().Actions!.Select(a => a.Shelter).SequenceEqual(new[] { "High", "Medium", "Low" })); Conserved(s);
});
Check("A newly available urgent shelter cannot redirect cargo already en route", () =>
{
    var scenario = ScenarioDefinition.PriorityDemo() with { FleetSize = 1, Shelters = new[] { new ShelterDefinition("Low", 10, 400, 200), new ShelterDefinition("High", 5, 700, 200, Priority: 9) } };
    var s = new SimulationSession(scenario); s.CloseRoute("High", 2); Require(s.Step().Actions![0].Shelter == "Low"); s.Step();
    var arrival = s.Step(); Require(arrival.Actions![0].Shelter == "Low" && arrival.Amount == 10); s.Step();
    Require(s.Step().Actions![0].Shelter == "High"); Conserved(s);
});
Check("Invalid ranks and unsupported legacy schema/engine priority combinations fail", () =>
{
    foreach (int rank in new[] { -1, 10 }) Reject(() => new SimulationSession(ScenarioDefinition.FleetDemo() with { Shelters = new[] { new ShelterDefinition("A", 5, 400, 200, Priority: rank) } }));
    Reject(() => new SimulationSession(ScenarioDefinition.FloodDemo() with { Shelters = new[] { new ShelterDefinition("A", 5, 400, 200, Priority: 1) } }));
    Reject(() => RunHistory.Replay(RunHistory.New(ScenarioDefinition.PriorityDemo()) with { EngineVersion = "2.4.0" }));
});
Check("Priority is part of the state hash before dispatch and survives reset", () =>
{
    var scenario = ScenarioDefinition.FleetDemo(); var changed = scenario with { Shelters = scenario.Shelters.Select((s, i) => s with { Priority = i }).ToArray() };
    var a = new SimulationSession(scenario); var b = new SimulationSession(changed);
    Require(RunHistory.Hash(a) != RunHistory.Hash(b)); string initial = RunHistory.Hash(b); b.Step(); b.Reset(); Require(initial == RunHistory.Hash(b));
});
Check("Editor preserves, validates and explicitly drops priority outside fleet mode", () =>
{
    var draft = new ScenarioDraft(ScenarioDefinition.PriorityDemo()); Require(draft.TryBuild(out var built, out _) && built!.Shelters[2].Priority == 9);
    draft.Shelters[0].Priority = "abc"; Require(!draft.TryBuild(out _, out _)); draft.Shelters[0].Priority = "10"; Require(!draft.TryBuild(out _, out _));
    draft.UsesFleet = false; Require(draft.TryBuild(out built, out _) && built!.Shelters.All(s => s.Priority == 0));
});
Check("Priority JSON/XML round-trip and missing ranks default to zero", () =>
{
    foreach (string extension in new[] { ".json", ".xml" })
    {
        string path = Path.Combine(directory, "priority" + extension); ScenarioFiles.Save(path, ScenarioDefinition.PriorityDemo());
        var loaded = ScenarioFiles.Load(path); Require(loaded.Shelters.Select(s => s.Priority).SequenceEqual(new[] { 0, 5, 9 }));
    }
    var plain = ScenarioFiles.Parse(JsonSerializer.Serialize(ScenarioDefinition.FleetDemo(), ScenarioFiles.JsonOptions), false);
    Require(plain.Shelters.All(s => s.Priority == 0));
});
Check("SQL reload preserves dispatch reasons and detects priority metadata tampering", () =>
{
    var run = RunHistory.New(ScenarioDefinition.PriorityDemo()); run = RunHistory.Append(run, new(Guid.NewGuid(), "Step"));
    var repo = new RunRepository(Path.Combine(directory, "priority.db")); repo.Save(run);
    Require(RunHistory.Replay(repo.Load(run.Id)).Events[0].Actions![0].Reason!.Contains("Priority 9"));
    using (var db = new RunDatabase(repo.DatabasePath))
    {
        var row = db.Runs.Single(r => r.Id == run.Id);
        row.DocumentJson = JsonSerializer.Serialize(run with { Scenario = run.Scenario with { Shelters = run.Scenario.Shelters.Select(s => s with { Priority = 0 }).ToArray() } }, ScenarioFiles.JsonOptions); db.SaveChanges();
    }
    Reject(() => repo.Load(run.Id));
});
Check("Original engine 2.4 SQL fleet events, hashes and checkpoints remain compatible", () =>
{
    using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "legacy-v24-fleet-run.json")));
    var f = fixture.RootElement; var old = f.GetProperty("run").Deserialize<RunDocument>(ScenarioFiles.JsonOptions)!; string hash = f.GetProperty("expectedHash").GetString()!;
    var repo = new RunRepository(Path.Combine(directory, "legacy.db"));
    using (var db = new RunDatabase(repo.DatabasePath))
    {
        db.Runs.Add(new StoredRun { Id = old.Id, ScenarioName = old.Scenario.Name, CreatedAt = old.CreatedAt.ToString("O"), DocumentJson = f.GetProperty("run").GetRawText(), Version = old.Commands.Length, Tick = 8, Status = "Completed", StateHash = hash });
        foreach (var json in f.GetProperty("commandJson").EnumerateArray())
        {
            string text = json.GetString()!; var c = JsonSerializer.Deserialize<RecordedCommand>(text, ScenarioFiles.JsonOptions)!;
            db.Commands.Add(new StoredCommand { RunId = old.Id, Sequence = c.Sequence, CommandId = c.Command.Id, Json = text });
        }
        int tick = 0; foreach (var json in f.GetProperty("eventJson").EnumerateArray()) db.Events.Add(new StoredEvent { RunId = old.Id, Tick = ++tick, Json = json.GetString()! });
        db.Snapshots.Add(new StoredSnapshot { RunId = old.Id, Sequence = old.Commands.Length, Tick = 8, Hash = hash, StateJson = f.GetProperty("checkpointJson").GetString()! }); db.SaveChanges();
    }
    Require(RunHistory.Hash(RunHistory.Replay(repo.Load(old.Id)).Session) == hash);
});
Check("ViewModel, preview and exports expose priority and decision provenance", () =>
{
    var vm = new RunViewModel(Demo()); vm.StepCommand.Execute(null); vm.SelectedShelterName = "Shelter-C";
    Require(vm.EventRows[0].Reason!.Contains("Priority 9") && vm.MapNodes[2].Priority == 9 && vm.SelectedDetail.Contains("priority 9"));
    var preview = CompletionPreview.Calculate(vm.Document); Require(preview.EndTick == 4 && preview.Remaining == 28 && preview.AdditionalSupplyRequired == 28);
    Reports.Export(directory, vm.Document); string stem = Path.Combine(directory, "run-" + vm.Document.Id.ToString("N"));
    Require(File.ReadAllText(stem + "-vehicles.csv").Contains("Priority 9") && File.ReadAllText(stem + ".html").Contains("<th>Priority</th>"));
});
Check("Fleet/stock/culture matrix keeps stable priority decisions and conserves supplies", () =>
{
    var previous = CultureInfo.CurrentCulture;
    try
    {
        foreach (int size in Enumerable.Range(1, 8)) foreach (int stock in new[] { 7, 20, 48 })
        {
            var scenario = ScenarioDefinition.PriorityDemo() with { FleetSize = size, DepotStock = stock };
            CultureInfo.CurrentCulture = new("en-US"); var a = new SimulationSession(scenario);
            CultureInfo.CurrentCulture = new("ar-SA"); var b = new SimulationSession(scenario);
            while (a.Status is not (RunStatus.Completed or RunStatus.SupplyExhausted))
            {
                Require(a.Tick < 100); var x = a.Step(); var y = b.Step(); Require(x.Actions!.SequenceEqual(y.Actions!));
                Conserved(a); Require(RunHistory.Hash(a) == RunHistory.Hash(b));
            }
        }
    }
    finally { CultureInfo.CurrentCulture = previous; }
});

string root = AppContext.BaseDirectory; while (!File.Exists(Path.Combine(root, "Aegis.Sim.slnx"))) root = Directory.GetParent(root)?.FullName ?? throw new Exception("Repository not found");
var socket = new TcpListener(IPAddress.Loopback, 0); socket.Start(); int port = ((IPEndPoint)socket.LocalEndpoint).Port; socket.Stop();
string hostData = Path.Combine(directory, "host"); string scenarioPath = Path.Combine(directory, "network.json"); ScenarioFiles.Save(scenarioPath, ScenarioDefinition.PriorityDemo());
var start = new ProcessStartInfo(Path.Combine(root, "src", "Aegis.SyncHost", "bin", "Release", "net10.0", "Aegis.SyncHost.exe")) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
foreach (string arg in new[] { "--urls", $"http://127.0.0.1:{port}", "--data-dir", hostData, "--scenario", scenarioPath }) start.ArgumentList.Add(arg);
using var host = Process.Start(start)!; host.OutputDataReceived += (_, _) => { }; host.ErrorDataReceived += (_, _) => { }; host.BeginOutputReadLine(); host.BeginErrorReadLine();
try
{
    using var http = new HttpClient(); var watch = Stopwatch.StartNew();
    while (true) { try { if ((await http.GetAsync($"http://127.0.0.1:{port}/health")).IsSuccessStatusCode) break; } catch { } if (watch.Elapsed > TimeSpan.FromSeconds(25)) throw new TimeoutException(); await Task.Delay(100); }
    using var secrets = JsonDocument.Parse(File.ReadAllText(Path.Combine(hostData, "session-secrets.json")));
    HubConnection Connect(string role) => new HubConnectionBuilder().WithUrl($"http://127.0.0.1:{port}/exercise", o => o.AccessTokenProvider = () => Task.FromResult<string?>(secrets.RootElement.GetProperty(role).GetString())).Build();
    await using var controller = Connect("Controller"); await using var observer = Connect("Observer"); await controller.StartAsync(); await observer.StartAsync();
    var state = await controller.InvokeAsync<RoomSnapshot>("Snapshot");
    for (int i = 0; i < 4; i++) state = await controller.InvokeAsync<RoomSnapshot>("Submit", new HostCommandRequest(state.Run.Id, state.Run.Commands.Length, new(Guid.NewGuid(), "Step")));
    var seen = await observer.InvokeAsync<RoomSnapshot>("Snapshot"); var replay = RunHistory.Replay(seen.Run);
    Require(replay.Session.Shelters[2].Remaining == 0 && replay.Session.Status == RunStatus.SupplyExhausted && replay.Events[0].Actions![0].Reason!.Contains("Priority 9"));
    state = await controller.InvokeAsync<RoomSnapshot>("Submit", new HostCommandRequest(state.Run.Id, state.Run.Commands.Length, new(Guid.NewGuid(), "Restock", Amount: 28)));
    for (int i = 0; i < 8; i++) state = await controller.InvokeAsync<RoomSnapshot>("Submit", new HostCommandRequest(state.Run.Id, state.Run.Commands.Length, new(Guid.NewGuid(), "Step")));
    seen = await observer.InvokeAsync<RoomSnapshot>("Snapshot"); Require(RunHistory.Replay(seen.Run).Session.Status == RunStatus.Completed && RunHistory.Hash(RunHistory.Replay(seen.Run).Session) == RunHistory.Hash(RunHistory.Replay(state.Run).Session));
    Pass("Authoritative host and observer agree on priority, scarcity, restock and completion");
}
finally { if (!host.HasExited) host.Kill(true); }
Console.WriteLine($"All {passed} priority checks passed. Evidence: {directory}");

static SimulationSession Demo() => new(ScenarioDefinition.PriorityDemo());
static void Finish(SimulationSession s) { while (s.Status is not (RunStatus.Completed or RunStatus.SupplyExhausted)) { Require(s.Tick < 100); s.Step(); Conserved(s); } }
static void Conserved(SimulationSession s)
{
    Require(s.DeliveredTotal + s.InTransit + s.DepotStockRemaining == s.Scenario.DepotStock + s.RestockedTotal && s.DepotStockRemaining >= 0);
    foreach (var shelter in s.Shelters) Require(shelter.Remaining >= s.Vehicles.Where(v => v.State.Phase == VehiclePhase.Outbound && v.State.Destination == shelter.Name).Sum(v => v.State.Load));
}
static void Require(bool value) { if (!value) throw new Exception("Check failed"); }
static void Reject(Action action) { try { action(); } catch (ArgumentException) { return; } catch (InvalidDataException) { return; } throw new Exception("Expected rejection"); }
void Check(string name, Action action) { action(); Pass(name); }
void Pass(string name) { passed++; Console.WriteLine("PASS: " + name); }
