using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Aegis.Infrastructure;
using Aegis.Presentation;
using Aegis.Simulation;
using Microsoft.AspNetCore.SignalR.Client;

int passed = 0;
string directory = Path.Combine(Path.GetTempPath(), "aegis-logistics-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
Check("Dispatch reserves finite stock without inventing delivery", () =>
{
    var s = new SimulationSession(ScenarioDefinition.LogisticsDemo()); var e = s.Step();
    Require(e.Kind == "Dispatched" && e.Amount == 0 && s.DeliveredTotal == 0 && s.Shelters[0].Remaining == 25);
    Require(s.DepotStockRemaining == 20 && s.Vehicle.Load == 10 && s.Vehicle.Phase == VehiclePhase.Outbound && s.Vehicle.TicksRemaining == 2);
    Conserved(s);
});
Check("Delivery occurs on arrival; completion waits for return", () =>
{
    var s = new SimulationSession(Scenario(5, 5, 2, 1));
    Require(s.Step().Kind == "Dispatched"); Require(s.Step().Kind == "Travelling"); Require(s.DeliveredTotal == 0);
    Require(s.Step().Amount == 5 && s.Vehicle.Phase == VehiclePhase.Returning && s.Status != RunStatus.Completed);
    Require(s.Step().Kind == "Returned" && s.Status == RunStatus.Completed && s.Tick == 4);
    Expect(() => s.Step()); Conserved(s);
});
Check("Partial final load never exceeds stock or demand", () =>
{
    var s = new SimulationSession(Scenario(3, 9, 1, 0));
    s.Step(); Require(s.Vehicle.Load == 3 && s.DepotStockRemaining == 6);
    s.Step(); Require(s.DeliveredTotal == 3 && s.Shelters[0].Remaining == 0 && s.Status == RunStatus.Completed);
    Require(s.DepotStockRemaining == 6); Conserved(s);
});
Check("Shortage stops at tick 16; restock completes at tick 24", () =>
{
    var s = new SimulationSession(ScenarioDefinition.LogisticsDemo());
    for (int i = 0; i < 16; i++) s.Step();
    Require(s.Status == RunStatus.SupplyExhausted && s.DeliveredTotal == 30 && s.DepotStockRemaining == 0 && s.Vehicle.Load == 0);
    Expect(() => s.Step()); Require(s.Tick == 16);
    s.Restock(20); Require(s.Status == RunStatus.Paused);
    for (int i = 0; i < 8; i++) s.Step();
    Require(s.Status == RunStatus.Completed && s.Tick == 24 && s.DeliveredTotal == 48 && s.DepotStockRemaining == 2);
    Conserved(s);
});
Check("Closed route cannot dispatch or consume stock", () =>
{
    var s = new SimulationSession(Scenario(5, 5, 1, 0)); s.CloseRoute("A", 2);
    Require(s.Step().Kind == "Waiting" && s.Step().Kind == "Waiting");
    Require(s.DepotStockRemaining == 5 && s.Vehicle.Load == 0);
    Require(s.Step().Kind == "Dispatched"); Conserved(s);
});
Check("Mid-trip closure freezes travel without losing cargo", () =>
{
    var s = new SimulationSession(Scenario(5, 5, 2, 1)); s.Step(); s.CloseRoute("A", 2);
    Require(s.Step().Kind == "RouteWait" && s.Step().Kind == "RouteWait" && s.Vehicle.TicksRemaining == 2);
    Require(s.Vehicle.Load == 5 && s.DeliveredTotal == 0); Conserved(s);
    s.Step(); Require(s.Step().Amount == 5); s.Step(); Require(s.Status == RunStatus.Completed && s.Tick == 6);
});
Check("Shelter travel override and zero return obey exact boundaries", () =>
{
    var scenario = Scenario(5, 5, 4, 0) with { Shelters = new[] { new ShelterDefinition("A", 5, 400, 200, 1) } };
    var s = new SimulationSession(scenario); s.Step(); s.Step();
    Require(s.Tick == 2 && s.Status == RunStatus.Completed && s.Vehicle.Phase == VehiclePhase.AtDepot);
});
Check("Empty demand completes; zero stock with demand is exhausted", () =>
{
    var done = new SimulationSession(Scenario(0, 0, 1, 1)); Require(done.Status == RunStatus.Completed && done.Tick == 0);
    var empty = new SimulationSession(Scenario(5, 0, 1, 1)); Require(empty.Status == RunStatus.SupplyExhausted);
    Expect(() => empty.Start()); empty.Restock(5); empty.Start(); Require(empty.Status == RunStatus.Running);
});
Check("Reset restores original inventory, trip and restock state", () =>
{
    var s = new SimulationSession(ScenarioDefinition.LogisticsDemo()); s.Restock(20); s.Step(); s.Step();
    s.Reset(); Require(s.Tick == 0 && s.DepotStockRemaining == 30 && s.RestockedTotal == 0 && s.Vehicle.Phase == VehiclePhase.AtDepot);
    Require(s.Shelters[0].Remaining == 25 && s.Status == RunStatus.Ready); Conserved(s);
});
Check("Many bounded stock/travel configurations conserve every packet", () =>
{
    foreach (int capacity in new[] { 1, 3, 10 }) foreach (int travel in new[] { 1, 2, 4 }) foreach (int back in new[] { 0, 1, 3 })
    {
        var s = new SimulationSession(Scenario(13, 13, travel, back) with { Capacity = capacity });
        int attempts = 0;
        while (s.Status != RunStatus.Completed && attempts++ < 150) { s.Step(); Conserved(s); }
        Require(s.Status == RunStatus.Completed && s.DeliveredTotal == 13 && s.DepotStockRemaining == 0);
    }
});
Check("Invalid schema settings cannot silently change legacy rules", () =>
{
    foreach (var bad in new[] {
        ScenarioDefinition.FloodDemo() with { DepotStock = 10 }, Scenario(5, 5, 0, 1), Scenario(5, 5, 21, 1),
        Scenario(5, 5, 1, -1), Scenario(5, -1, 1, 1), Scenario(5, 5, 1, 1) with { DepotStock = null },
        Scenario(5, 5, 1, 1) with { Shelters = new[] { new ShelterDefinition("A", 5, 400, 200, 0) } }
    }) Expect(() => bad.Validate());
});
Check("Schema 2 JSON/XML round-trip preserves route timing", () =>
{
    var scenario = Scenario(5, 9, 3, 2) with { Shelters = new[] { new ShelterDefinition("A", 5, 400, 200, 4) } };
    foreach (string extension in new[] { ".json", ".xml" })
    {
        string path = Path.Combine(directory, "scenario" + extension); ScenarioFiles.Save(path, scenario);
        Require(JsonSerializer.Serialize(ScenarioFiles.Load(path)) == JsonSerializer.Serialize(scenario));
    }
});
using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "legacy-v2-run.json")));
RunDocument legacy = fixture.RootElement.GetProperty("run").Deserialize<RunDocument>(ScenarioFiles.JsonOptions)!;
string legacyHash = fixture.RootElement.GetProperty("expectedHash").GetString()!;
Check("Exact v2.0 release archive retains its original state hash", () =>
{
    var s = RunHistory.Replay(legacy).Session;
    Require(s.Tick == 6 && s.DeliveredTotal == 48 && RunHistory.Hash(s) == legacyHash);
    Require(!s.UsesLogistics && s.DepotStockRemaining == null);
});
Check("Normalized v2.0 SQL commands, events and checkpoints still load", () =>
{
    var repo = new RunRepository(Path.Combine(directory, "legacy.db"));
    using (var db = new RunDatabase(repo.DatabasePath))
    {
        db.Runs.Add(new StoredRun
        {
            Id = legacy.Id,
            ScenarioName = legacy.Scenario.Name,
            CreatedAt = legacy.CreatedAt.ToString("O"),
            DocumentJson = fixture.RootElement.GetProperty("run").GetRawText(),
            Version = legacy.Commands.Length,
            Tick = 6,
            StateHash = legacyHash,
            Status = "Completed"
        });
        int sequence = 0;
        foreach (var item in fixture.RootElement.GetProperty("commandJson").EnumerateArray())
        {
            string text = item.GetString()!; var record = JsonSerializer.Deserialize<RecordedCommand>(text, ScenarioFiles.JsonOptions)!;
            db.Commands.Add(new StoredCommand { RunId = legacy.Id, Sequence = ++sequence, CommandId = record.Command.Id, Json = text });
        }
        int tick = 0;
        foreach (var item in fixture.RootElement.GetProperty("eventJson").EnumerateArray()) db.Events.Add(new StoredEvent { RunId = legacy.Id, Tick = ++tick, Json = item.GetString()! });
        db.Snapshots.Add(new StoredSnapshot { RunId = legacy.Id, Sequence = sequence, Tick = 6, Hash = legacyHash, StateJson = fixture.RootElement.GetProperty("checkpointJson").GetString()! });
        db.SaveChanges();
    }
    Require(RunHistory.Hash(RunHistory.Replay(repo.Load(legacy.Id)).Session) == legacyHash);
});
RunDocument completed = RunHistory.New(ScenarioDefinition.LogisticsDemo());
for (int i = 0; i < 16; i++) completed = RunHistory.Append(completed, new(Guid.NewGuid(), "Step"));
completed = RunHistory.Append(completed, new(Guid.NewGuid(), "Restock", Amount: 20));
for (int i = 0; i < 8; i++) completed = RunHistory.Append(completed, new(Guid.NewGuid(), "Step"));
Check("SQL restores stock, cargo and return phase at intermediate ticks", () =>
{
    var repo = new RunRepository(Path.Combine(directory, "logistics.db")); repo.Save(completed);
    var loaded = repo.Load(completed.Id);
    var outbound = RunHistory.Replay(loaded, 2).Session; Require(outbound.Vehicle.Load == 10 && outbound.Vehicle.TicksRemaining == 1 && outbound.DepotStockRemaining == 20);
    var returning = RunHistory.Replay(loaded, 3).Session; Require(returning.Vehicle.Phase == VehiclePhase.Returning && returning.DeliveredTotal == 10);
    Require(RunHistory.Hash(RunHistory.Replay(loaded).Session) == RunHistory.Hash(RunHistory.Replay(completed).Session));
});
Check("Cargo and restock are part of replay checksum validation", () =>
{
    var commands = completed.Commands.ToArray(); int index = Array.FindIndex(commands, c => c.Command.Type == "Restock");
    commands[index] = commands[index] with { Command = commands[index].Command with { Amount = 19 } };
    Expect(() => RunHistory.Replay(completed with { Commands = commands }));
    Expect(() => RunHistory.Replay(completed with { EngineVersion = "2.0.0" }));
});
Check("ViewModel shows transit and blocks writes in replay", () =>
{
    var vm = new RunViewModel(new SimulationSession(ScenarioDefinition.LogisticsDemo()));
    vm.StepCommand.Execute(null); var depot = vm.VehiclePosition; vm.StepCommand.Execute(null);
    Require(vm.StockLabel.Contains("20 packets") && vm.StockLabel.Contains("In transit: 10") && vm.VehicleLabel.Contains("Outbound"));
    Require(vm.VehiclePosition.X > depot.X && vm.DeliveredTotal == 0);
    vm.LoadReplay(completed); Require(!vm.RestockCommand.CanExecute(null));
    vm.ReplayTick = 2; Require(vm.StockLabel.Contains("In transit: 10"));
});
Check("Reports include finite inventory and actual travel metrics", () =>
{
    var metrics = RunMetrics.Calculate(completed);
    Require(metrics.Ticks == 24 && metrics.Delivered == 48 && metrics.DepotStockRemaining == 2 && metrics.RestockedTotal == 20);
    Require(metrics.OutboundTicks == 12 && metrics.ReturnTicks == 6);
    Reports.Export(directory, completed);
    string html = File.ReadAllText(Path.Combine(directory, "run-" + completed.Id.ToString("N") + ".html"));
    Require(html.Contains("Depot stock: 2") && html.Contains("Restocked: 20") && html.Contains("Return ticks: 6"));
});
Check("Invalid restock and completed-run restock leave state unchanged", () =>
{
    var s = new SimulationSession(Scenario(5, 5, 1, 0)); string before = RunHistory.Hash(s);
    foreach (int amount in new[] { -1, 0, 10000 }) Expect(() => s.Restock(amount));
    Require(RunHistory.Hash(s) == before); s.Step(); s.Step(); Expect(() => s.Restock(1));
    var old = SimulationSession.CreateDemo(); Expect(() => old.Restock(1));
});

// Real host started with schema 2; both clients must agree on conserved inventory.
string root = AppContext.BaseDirectory;
while (!File.Exists(Path.Combine(root, "Aegis.Sim.slnx"))) root = Directory.GetParent(root)?.FullName ?? throw new Exception("Repository not found.");
var socket = new TcpListener(IPAddress.Loopback, 0); socket.Start(); int port = ((IPEndPoint)socket.LocalEndpoint).Port; socket.Stop();
string hostDirectory = Path.Combine(directory, "host"); string scenarioPath = Path.Combine(directory, "network-scenario.json");
ScenarioFiles.Save(scenarioPath, ScenarioDefinition.LogisticsDemo());
var start = new ProcessStartInfo(Path.Combine(root, "src", "Aegis.SyncHost", "bin", "Release", "net10.0", "Aegis.SyncHost.exe")) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
foreach (string arg in new[] { "--urls", $"http://127.0.0.1:{port}", "--data-dir", hostDirectory, "--scenario", scenarioPath }) start.ArgumentList.Add(arg);
using var host = Process.Start(start)!; host.OutputDataReceived += (_, _) => { }; host.ErrorDataReceived += (_, _) => { }; host.BeginOutputReadLine(); host.BeginErrorReadLine();
try
{
    using var http = new HttpClient(); var watch = Stopwatch.StartNew();
    while (true)
    {
        try { if ((await http.GetAsync($"http://127.0.0.1:{port}/health")).IsSuccessStatusCode) break; } catch { }
        if (watch.Elapsed > TimeSpan.FromSeconds(25)) throw new TimeoutException(); await Task.Delay(100);
    }
    using var secrets = JsonDocument.Parse(File.ReadAllText(Path.Combine(hostDirectory, "session-secrets.json")));
    HubConnection Connect(string role) => new HubConnectionBuilder().WithUrl($"http://127.0.0.1:{port}/exercise", options =>
        options.AccessTokenProvider = () => Task.FromResult<string?>(secrets.RootElement.GetProperty(role).GetString())).Build();
    await using var controller = Connect("Controller"); await using var observer = Connect("Observer");
    await controller.StartAsync(); await observer.StartAsync();
    var state = await controller.InvokeAsync<RoomSnapshot>("Snapshot");
    for (int i = 0; i < 16; i++) state = await controller.InvokeAsync<RoomSnapshot>("Submit", new HostCommandRequest(state.Run.Id, state.Run.Commands.Length, new(Guid.NewGuid(), "Step")));
    Require(RunHistory.Replay(state.Run).Session.Status == RunStatus.SupplyExhausted);
    state = await controller.InvokeAsync<RoomSnapshot>("Submit", new HostCommandRequest(state.Run.Id, state.Run.Commands.Length, new(Guid.NewGuid(), "Restock", Amount: 20)));
    for (int i = 0; i < 8; i++) state = await controller.InvokeAsync<RoomSnapshot>("Submit", new HostCommandRequest(state.Run.Id, state.Run.Commands.Length, new(Guid.NewGuid(), "Step")));
    var observed = await observer.InvokeAsync<RoomSnapshot>("Snapshot");
    var a = RunHistory.Replay(state.Run).Session; var b = RunHistory.Replay(observed.Run).Session;
    Require(a.Status == RunStatus.Completed && a.Tick == 24 && b.DepotStockRemaining == 2 && RunHistory.Hash(a) == RunHistory.Hash(b));
    Pass("Authoritative host and observer agree on stock exhaustion, restock and completed travel");
}
finally { if (!host.HasExited) host.Kill(true); }
Console.WriteLine($"All {passed} logistics checks passed. Evidence: {directory}");

static ScenarioDefinition Scenario(int demand, int stock, int travel, int back) => new(2, "Logistics checks", 10, new[] { new ShelterDefinition("A", demand, 400, 200) }, stock, travel, back);
static void Require(bool condition) { if (!condition) throw new Exception("Check failed."); }
static void Expect(Action action) { try { action(); } catch (ArgumentException) { return; } catch (InvalidOperationException) { return; } catch (InvalidDataException) { return; } throw new Exception("Expected invalid operation."); }
static void Conserved(SimulationSession s)
{
    Require(s.DepotStockRemaining >= 0 && s.Vehicle.Load >= 0 && s.Shelters.All(x => x.Remaining >= 0));
    Require(s.Scenario.DepotStock + s.RestockedTotal == s.DepotStockRemaining + s.Vehicle.Load + s.DeliveredTotal);
    Require(s.DeliveredTotal + s.Shelters.Sum(x => x.Remaining) == s.InitialDemand);
}
void Check(string name, Action action) { action(); Pass(name); }
void Pass(string name) { passed++; Console.WriteLine("PASS: " + name); }
