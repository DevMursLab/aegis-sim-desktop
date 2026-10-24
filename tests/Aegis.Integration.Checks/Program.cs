using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Xml;
using Aegis.Infrastructure;
using Aegis.Presentation;
using Aegis.Simulation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

if (args.Length > 0 && args[0] == "--probe")
{
    var saved = new RunRepository(args[1]).Load(Guid.Parse(args[2]));
    if (RunHistory.Hash(RunHistory.Replay(saved).Session) != args[3]) throw new Exception("Restarted process hash mismatch.");
    Console.WriteLine("Child process restored the same SQL history and final hash."); return;
}
int passed = 0;
string directory = Path.Combine(Path.GetTempPath(), "aegis-integration-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
var repository = new RunRepository(Path.Combine(directory, "runs.db"));
var scenario = ScenarioDefinition.FloodDemo();
RunDocument baseline = Finish(RunHistory.New(scenario));
RunDocument changed = Finish(RunHistory.Append(RunHistory.New(scenario), new(Guid.NewGuid(), "CloseRoute", "Shelter-A", 5)));

Check("Closure changes observed completion metrics", () =>
{
    Require(RunHistory.Replay(baseline).Session.Tick == 6 && RunHistory.Replay(changed).Session.Tick == 8);
    Require(RunHistory.Replay(changed).Events.Count(e => e.Kind == "Waiting") == 2);
    Require(Reports.Compare(baseline, changed).Contains("+2 ticks"));
});
Check("JSON and XML round-trip preserve coordinates and scenario", () =>
{
    foreach (string extension in new[] { ".json", ".xml" })
    {
        string file = Path.Combine(directory, "scenario" + extension); ScenarioFiles.Save(file, scenario);
        Require(JsonSerializer.Serialize(ScenarioFiles.Load(file)) == JsonSerializer.Serialize(scenario));
    }
});
Check("Malformed, oversized, unknown-version and external-entity scenarios fail", () =>
{
    Expect(() => ScenarioFiles.Parse("{broken", false));
    Expect(() => ScenarioFiles.Parse(new string('x', 262145), false));
    Expect(() => ScenarioFiles.Parse("<!DOCTYPE Scenario [<!ENTITY x SYSTEM 'file:///not-allowed'>]><Scenario version='1' name='x' capacity='1'><Shelter name='&x;' demand='1' x='100' y='100'/></Scenario>", true));
    Expect(() => ScenarioFiles.Parse("{\"schemaVersion\":9,\"name\":\"x\",\"capacity\":10,\"shelters\":[]}", false));
    Expect(() => ScenarioFiles.Parse("{\"schemaVersion\":1,\"name\":\"x\",\"capacity\":10,\"shelters\":[],\"unknown\":true}", false));
});
Check("Duplicate command is idempotent; tampered hashes fail", () =>
{
    var empty = RunHistory.New(scenario); var command = new ExerciseCommand(Guid.NewGuid(), "Step");
    var once = RunHistory.Append(empty, command); Require(RunHistory.Append(once, command).Commands.Length == 1);
    var tampered = once with { Commands = new[] { once.Commands[0] with { StateHash = "tampered" } } };
    Expect(() => RunHistory.Replay(tampered));
});
Check("SQL transactions save normalized commands and checkpoints", () =>
{
    repository.Save(baseline); repository.Save(changed);
    using var db = new RunDatabase(repository.DatabasePath);
    Require(db.Commands.Count(c => c.RunId == baseline.Id) == baseline.Commands.Length);
    Require(db.Snapshots.Any(s => s.RunId == changed.Id && s.Tick == 8));
    Require(db.Events.Count(e => e.RunId == changed.Id) == 8);
    Require(RunHistory.Hash(RunHistory.Replay(repository.Load(changed.Id)).Session) == RunHistory.Hash(RunHistory.Replay(changed).Session));
});
Check("Metrics are computed from delivery provenance", () =>
{
    var metrics = RunMetrics.Calculate(changed);
    Require(metrics.Delivered == 48 && metrics.Remaining == 0 && metrics.CompletionPercent == 100);
    Require(metrics.WaitingTicks == 2 && metrics.ActiveTickPercent == 75 && metrics.Closures == 1);
    Require(Math.Abs(metrics.MeanServiceTick - 219.0 / 48) < 0.00001);
});
Check("Command ID reuse with a different action is rejected", () =>
{
    var command = new ExerciseCommand(Guid.NewGuid(), "Step"); var run = RunHistory.Append(RunHistory.New(scenario), command);
    Expect(() => RunHistory.Append(run, command with { Type = "Start" }));
});
Check("PDF reports are readable and paginate larger scenarios", () =>
{
    var many = new ScenarioDefinition(1, "Pagination check", 100, Enumerable.Range(1, 64).Select(i => new ShelterDefinition("Shelter-" + i, 1, 100, 100)).ToArray());
    var run = Finish(RunHistory.New(many));
    string path = Path.Combine(directory, "pagination.pdf"); PdfReport.Save(path, run);
    using var pdf = PdfSharp.Pdf.IO.PdfReader.Open(path, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
    Require(pdf.PageCount == 4);
});
Check("History cannot be overwritten or truncated", () =>
{
    Expect(() => repository.Save(changed with { Commands = Array.Empty<RecordedCommand>() }));
    Require(repository.Load(changed.Id).Commands.Length == changed.Commands.Length);
});
Check("Every replay tick conserves state; replay is read-only", () =>
{
    var vm = new RunViewModel(SimulationSession.CreateDemo()); vm.LoadReplay(changed);
    for (int i = 0; i <= 8; i++)
    {
        vm.ReplayTick = i; Require(vm.Tick == i && vm.DeliveredTotal + vm.Remaining == 48);
        Require(!vm.StepCommand.CanExecute(null) && !vm.StartCommand.CanExecute(null));
    }
    vm.ResetCommand.Execute(null); Require(!vm.IsReplay && vm.Tick == 0 && vm.Document.Id != changed.Id);
});
Check("A failed persistence commit never publishes a successful delivery", () =>
{
    var vm = new RunViewModel(SimulationSession.CreateDemo()) { Persist = _ => throw new IOException("Injected storage failure") };
    vm.StepCommand.Execute(null);
    Require(vm.Tick == 0 && vm.Events.Count == 0 && vm.Status == "Storage error" && !vm.StepCommand.CanExecute(null));
});
Check("Reports derive facts from replay and escape spreadsheet/HTML input", () =>
{
    Reports.Export(directory, changed);
    string stem = Path.Combine(directory, "run-" + changed.Id.ToString("N"));
    Require(File.ReadAllText(stem + ".csv").Contains("Waiting"));
    Require(File.ReadAllText(stem + ".html").Contains(RunHistory.Hash(RunHistory.Replay(changed).Session)));
    var unsafeScenario = new ScenarioDefinition(1, "<script>bad</script>", 1, new[] { new ShelterDefinition("=1+1", 1, 100, 100) });
    var unsafeRun = Finish(RunHistory.New(unsafeScenario)); Reports.Export(directory, unsafeRun);
    string unsafeStem = Path.Combine(directory, "run-" + unsafeRun.Id.ToString("N"));
    Require(!File.ReadAllText(unsafeStem + ".html").Contains("<script>bad"));
    Require(File.ReadAllText(unsafeStem + ".csv").Contains("'=1+1"));
});
Check("SQL tampering is detected on load", () =>
{
    var document = Finish(RunHistory.New(scenario)); repository.Save(document);
    using (var db = new RunDatabase(repository.DatabasePath))
    {
        var row = db.Commands.First(c => c.RunId == document.Id); row.Json = "{}"; db.SaveChanges();
    }
    Expect(() => repository.Load(document.Id));
});
Check("Independent process restores the same saved state", () =>
{
    var info = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
    if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase)) info.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
    foreach (string arg in new[] { "--probe", repository.DatabasePath, baseline.Id.ToString(), RunHistory.Hash(RunHistory.Replay(baseline).Session) }) info.ArgumentList.Add(arg);
    using var child = Process.Start(info)!;
    Require(child.WaitForExit(15000) && child.ExitCode == 0);
});

// Actual HTTP/WebSocket host, two independent SignalR connections, persisted restart.
string root = AppContext.BaseDirectory;
while (!File.Exists(Path.Combine(root, "Aegis.Sim.slnx"))) root = Directory.GetParent(root)?.FullName ?? throw new Exception("Repository root not found.");
var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); int port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
string hostDirectory = Path.Combine(directory, "host"); string url = $"http://127.0.0.1:{port}";
Process StartHost()
{
    var start = new ProcessStartInfo(Path.Combine(root, "src", "Aegis.SyncHost", "bin", "Release", "net10.0", "Aegis.SyncHost.exe")) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (string arg in new[] { "--urls", url, "--data-dir", hostDirectory }) start.ArgumentList.Add(arg);
    var process = Process.Start(start)!; process.OutputDataReceived += (_, _) => { }; process.ErrorDataReceived += (_, _) => { }; process.BeginOutputReadLine(); process.BeginErrorReadLine(); return process;
}
using var host = StartHost();
try
{
    using var http = new HttpClient();
    await Until(async () => { try { return (await http.GetAsync(url + "/health")).IsSuccessStatusCode; } catch { return false; } });
    using var secrets = JsonDocument.Parse(File.ReadAllText(Path.Combine(hostDirectory, "session-secrets.json")));
    string controllerToken = secrets.RootElement.GetProperty("Controller").GetString()!;
    string observerToken = secrets.RootElement.GetProperty("Observer").GetString()!;
    HubConnection Connect(string token) => new HubConnectionBuilder().WithUrl(url + "/exercise", options => options.AccessTokenProvider = () => Task.FromResult<string?>(token)).WithAutomaticReconnect().Build();
    await using var controller = Connect(controllerToken); await using var observer = Connect(observerToken);
    await controller.StartAsync(); await observer.StartAsync();
    var initial = await controller.InvokeAsync<RoomSnapshot>("Snapshot");
    var received = new TaskCompletionSource<RoomSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
    observer.On<RoomSnapshot>("State", state => received.TrySetResult(state));
    var command = new ExerciseCommand(Guid.NewGuid(), "CloseRoute", "Shelter-A", 5);
    var request = new HostCommandRequest(initial.Run.Id, initial.Run.Commands.Length, command);
    var closed = await controller.InvokeAsync<RoomSnapshot>("Submit", request);
    var pushed = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
    Require(pushed.Run.Commands.Length == closed.Run.Commands.Length); Pass("Two clients receive the same committed SignalR event");
    var duplicate = await controller.InvokeAsync<RoomSnapshot>("Submit", request);
    Require(duplicate.Revision == closed.Revision && duplicate.Run.Commands.Length == closed.Run.Commands.Length); Pass("Network duplicate command cannot apply twice");
    await ExpectAsync(() => observer.InvokeAsync<RoomSnapshot>("Submit", new HostCommandRequest(closed.Run.Id, closed.Run.Commands.Length, new(Guid.NewGuid(), "Step"))));
    Require((await observer.InvokeAsync<RoomSnapshot>("Snapshot")).Revision == closed.Revision); Pass("Observer write command rejected by server");
    await ExpectAsync(() => controller.InvokeAsync<RoomSnapshot>("Submit", new HostCommandRequest(initial.Run.Id, 0, new(Guid.NewGuid(), "Step")))); Pass("Stale controller version rejected");
    Require((await http.GetAsync(url + "/snapshot")).StatusCode == HttpStatusCode.Unauthorized); Pass("Unauthenticated client cannot read exercise data");
    await observer.StopAsync();
    var stepped = await controller.InvokeAsync<RoomSnapshot>("Submit", new HostCommandRequest(closed.Run.Id, closed.Run.Commands.Length, new(Guid.NewGuid(), "Step")));
    await observer.StartAsync();
    var caughtUp = await observer.InvokeAsync<RoomSnapshot>("Snapshot");
    Require(caughtUp.Run.Commands.SequenceEqual(stepped.Run.Commands)); Pass("Disconnected observer catches up from authoritative snapshot");
    host.Kill(true); await host.WaitForExitAsync();
    await controller.StopAsync(); await observer.StopAsync();
    using var restarted = StartHost();
    try
    {
        await Until(async () => { try { return (await http.GetAsync(url + "/health")).IsSuccessStatusCode; } catch { return false; } });
        await controller.StartAsync();
        var restored = await controller.InvokeAsync<RoomSnapshot>("Snapshot");
        Require(restored.Run.Id == stepped.Run.Id && restored.Run.Commands.SequenceEqual(stepped.Run.Commands));
        Pass("Restarted host restores the authoritative SQL run");
        var resetCommand = new ExerciseCommand(Guid.NewGuid(), "Reset");
        var resetRequest = new HostCommandRequest(restored.Run.Id, restored.Run.Commands.Length, resetCommand);
        var reset = await controller.InvokeAsync<RoomSnapshot>("Submit", resetRequest);
        var repeatedReset = await controller.InvokeAsync<RoomSnapshot>("Submit", resetRequest);
        Require(reset.Run.Id != restored.Run.Id && reset.Run.Commands.Length == 0 && repeatedReset.Run.Id == reset.Run.Id);
        Require(new RunRepository(Path.Combine(hostDirectory, "runs.db")).Load(restored.Run.Id).Commands.Length == restored.Run.Commands.Length);
        Pass("Controller reset is idempotent and preserves the previous SQL run");
    }
    finally { await controller.StopAsync(); if (!restarted.HasExited) restarted.Kill(true); }
}
finally { if (!host.HasExited) host.Kill(true); }
Console.WriteLine($"All {passed} integration checks passed. Temporary evidence: {directory}");

static RunDocument Finish(RunDocument document)
{
    for (int i = 0; i < 200; i++)
    {
        if (RunHistory.Replay(document).Session.Status == RunStatus.Completed) return document;
        document = RunHistory.Append(document, new(Guid.NewGuid(), "Step"));
    }
    throw new Exception("Exercise did not complete.");
}
static void Require(bool result) { if (!result) throw new Exception("Check failed."); }
static void Expect(Action action) { try { action(); } catch { return; } throw new Exception("Expected invalid input to fail."); }
static async Task ExpectAsync(Func<Task> action) { try { await action(); } catch (HubException) { return; } throw new Exception("Expected server rejection."); }
static async Task Until(Func<Task<bool>> condition) { var watch = Stopwatch.StartNew(); while (!await condition()) { if (watch.Elapsed > TimeSpan.FromSeconds(25)) throw new TimeoutException("Host did not become healthy."); await Task.Delay(100); } }
void Check(string name, Action action) { action(); Pass(name); }
void Pass(string name) { passed++; Console.WriteLine("PASS: " + name); }
