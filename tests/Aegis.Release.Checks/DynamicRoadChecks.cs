using System.Text.Json;
using Aegis.Infrastructure;
using Aegis.Presentation;
using Aegis.Simulation;
using Microsoft.Data.Sqlite;

internal static class DynamicRoadChecks
{
    public static int Run(ScenarioDefinition scenario, string directory)
    {
        int passed = 0;
        void Require(bool value) { if (!value) throw new Exception("Dynamic road check failed"); }
        void Check(string name, Action action) { action(); passed++; Console.WriteLine("PASS: " + name); }
        void Reject(Action action)
        {
            try { action(); } catch (Exception e) when (e is ArgumentException or InvalidDataException or InvalidOperationException) { return; }
            throw new Exception("Expected road command rejection");
        }
        RunDocument Close(RunDocument run, string road, int ticks) => RunHistory.Append(run, new(Guid.NewGuid(), "CloseRoad", road, ticks));
        RunDocument Step(RunDocument run) => RunHistory.Append(run, new(Guid.NewGuid(), "Step"));
        void Conserved(SimulationSession s) => Require(s.DeliveredTotal + s.InTransit + s.DepotStockRemaining == scenario.DepotStock && s.Shelters.All(x => x.Remaining >= 0));
        Check("New dispatch avoids a closed shared edge with deterministic longer geometry", () =>
        {
            var s = new SimulationSession(scenario); s.CloseRoad("road-01", 5); var e = s.Step();
            foreach (var vehicle in s.Vehicles)
            {
                var route = vehicle.State.Route!;
                Require(!route.RoadIds.Contains("road-01") && route.RoadIds[0] == "road-99");
                Require(vehicle.State.Duration == (route.Meters + 299) / 300);
                Require(e.Actions!.Single(a => a.VehicleId == vehicle.Id).Reason!.Contains("Open road path"));
            }
            Conserved(s);
        });
        Check("Disconnected depot waits without consuming cargo and resumes after exact expiry", () =>
        {
            var s = new SimulationSession(scenario); s.CloseRoad("road-01", 2); s.CloseRoad("road-99", 2);
            for (int i = 0; i < 2; i++) { Require(s.Step().Actions!.All(a => a.Kind == "Waiting")); Require(s.InTransit == 0 && s.DepotStockRemaining == 30); }
            s.Step(); Require(s.InTransit == 20 && s.Vehicle.Route!.RoadIds.Contains("road-01")); Conserved(s);
        });
        Check("Unreachable urgent demand is skipped without reserving its cargo", () =>
        {
            var s = new SimulationSession(scenario); s.CloseRoad("road-03", 3); s.CloseRoad("road-04", 3);
            var e = s.Step(); Require(e.Actions!.Select(a => a.Shelter).SequenceEqual(new[] { "Road Shelter-C", "Road Shelter-A" }));
            Require(s.Shelters.Single(x => x.Name == "Road Shelter-B").Remaining == 10 && s.InTransit == 20); Conserved(s);
        });
        Check("In-flight shared road closure freezes every assigned outbound trip", () =>
        {
            var s = new SimulationSession(scenario); s.Step(); s.Step();
            var before = s.Vehicles.Select(v => v.State.TicksRemaining).ToArray();
            s.CloseRoad("road-01", 2);
            for (int i = 0; i < 2; i++) { Require(s.Step().Actions!.All(a => a.Kind == "RoadWait")); Require(s.Vehicles.Select(v => v.State.TicksRemaining).SequenceEqual(before)); Conserved(s); }
            s.Step(); Require(s.Vehicle.TicksRemaining < before[0]);
        });
        Check("Road closure also freezes return trips without duplicate delivery", () =>
        {
            var s = new SimulationSession(scenario); for (int i = 0; i < 4; i++) s.Step();
            Require(s.Vehicle.Phase == VehiclePhase.Returning); var before = s.Vehicle;
            s.CloseRoad("road-03", 2); var e = s.Step();
            Require(e.Actions![0].Kind == "ReturnRoadWait" && s.Vehicle.TicksRemaining == before.TicksRemaining && s.DeliveredTotal == 10);
            while (s.Status != RunStatus.Completed && s.Tick < 60) { s.Step(); Conserved(s); }
            Require(s.Status == RunStatus.Completed && s.DeliveredTotal == 30);
        });
        Check("Closure renewal cannot shorten an incident and reset clears all road state", () =>
        {
            var s = new SimulationSession(scenario); s.CloseRoad("road-01", 8); s.CloseRoad("road-01", 1);
            Require(s.RoadClosures.Single().BlockedUntil == 8); s.Step(); s.Reset();
            Require(s.RoadClosures.Count == 0 && s.Tick == 0 && s.InTransit == 0); s.Step(); Require(s.Vehicle.Route == null);
        });
        Check("Invalid roads durations and legacy command archives fail before mutation", () =>
        {
            var run = RunHistory.New(scenario); var s = new SimulationSession(scenario);
            foreach (int duration in new[] { 0, 101 }) Reject(() => s.CloseRoad("road-01", duration));
            Reject(() => s.CloseRoad("missing", 5)); Reject(() => new SimulationSession(ScenarioDefinition.FloodDemo()).CloseRoad("road-01", 5));
            Reject(() => Close(run with { EngineVersion = "3.0.0" }, "road-01", 5));
            Reject(() => RunHistory.Replay(Close(run, "road-01", 5) with { EngineVersion = "3.0.0" }));
            Require(s.RoadClosures.Count == 0);
            while (s.Status != RunStatus.Completed) s.Step(); Reject(() => s.CloseRoad("road-01", 5));
        });
        Check("Road routing tie order survives shuffled input and reversed road geometry", () =>
        {
            var map = scenario.Geography!;
            var shuffled = map with { Roads = map.Roads.Reverse().Select(r => r with { From = r.To, To = r.From, Points = r.Points.Reverse().ToArray() }).ToArray() };
            foreach (var shelter in scenario.Shelters)
            {
                var a = RoadRouting.Find(map, shelter.Name, new[] { "road-01" })!;
                var b = RoadRouting.Find(shuffled, shelter.Name, new[] { "road-01" })!;
                Require(a.Meters == b.Meters && a.RoadIds.SequenceEqual(b.RoadIds) && a.Points.SequenceEqual(b.Points));
            }
        });
        Check("Returned dynamic vehicle paths cannot mutate authoritative state", () =>
        {
            var s = new SimulationSession(scenario); s.CloseRoad("road-01", 5); s.Step(); string hash = RunHistory.Hash(s);
            s.Vehicles[0].State.Route!.RoadIds[0] = "tampered"; s.Vehicle.Route!.Points[0] = new(0, 0);
            Require(hash == RunHistory.Hash(s));
        });
        Check("Dynamic replay preview metrics and road traces agree through completion", () =>
        {
            var run = Step(Close(RunHistory.New(scenario), "road-01", 5));
            var projected = CompletionPreview.Calculate(run);
            while (RunHistory.Replay(run).Session.Status != RunStatus.Completed) run = Step(run);
            var replay = RunHistory.Replay(run); Conserved(replay.Session);
            Require(projected.FinalHash == RunHistory.Hash(replay.Session) && projected.EndTick == replay.Session.Tick);
            Require(RunMetrics.Calculate(run).Closures == 1);
            Reports.Export(Path.Combine(directory, "dynamic-reports"), run);
            string stem = Path.Combine(directory, "dynamic-reports", "run-" + run.Id.ToString("N"));
            Require(File.ReadAllText(stem + ".html").Contains("road-01: closed through tick 5"));
            Require(File.ReadAllText(stem + "-vehicles.csv").Contains("road-99"));
        });
        Check("Dynamic waiting metrics distinguish outbound from returning vehicle-ticks", () =>
        {
            var run = RunHistory.New(scenario); for (int i = 0; i < 4; i++) run = Step(run);
            var before = RunMetrics.Calculate(run); run = Step(Close(run, "road-03", 2));
            var after = RunMetrics.Calculate(run);
            Require(after.ReturnTicks == before.ReturnTicks + 1 && after.WaitingTicks == before.WaitingTicks + 1 && after.Closures == 1);
        });
        Check("Zero-priority dynamic exports still retain road decision traces", () =>
        {
            var plain = scenario with { Shelters = scenario.Shelters.Select(s => s with { Priority = 0 }).ToArray() };
            var run = Step(Close(RunHistory.New(plain), "road-01", 5));
            string output = Path.Combine(directory, "unranked-roads"); Reports.Export(output, run);
            Require(File.ReadAllText(Path.Combine(output, "run-" + run.Id.ToString("N") + "-vehicles.csv")).Contains("Open road path"));
            Require(RunHistory.Append(run, run.Commands[0].Command).Commands.Length == run.Commands.Length);
        });
        Check("SQLite verifies dynamic closures selected paths and incident tampering", () =>
        {
            string path = Path.Combine(directory, "dynamic.db"); var repo = new RunRepository(path);
            var run = Step(Close(RunHistory.New(scenario), "road-01", 5)); repo.Save(run);
            Require(RunHistory.Hash(RunHistory.Replay(repo.Load(run.Id)).Session) == RunHistory.Hash(RunHistory.Replay(run).Session));
            using var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString()); db.Open();
            using var command = db.CreateCommand(); command.CommandText = "UPDATE Snapshots SET StateJson = REPLACE(StateJson, 'road-01', 'road-XX')"; command.ExecuteNonQuery();
            Reject(() => repo.Load(run.Id));
        });
        Check("Original engine 3.0 road binary fixture retains its hashes and SQL checkpoints", () =>
        {
            var run = JsonSerializer.Deserialize<RunDocument>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "legacy-v30-road-run.json")), ScenarioFiles.JsonOptions)!;
            Require(run.EngineVersion == "3.0.0" && RunHistory.Replay(run).Session.Tick == 14);
            var repo = new RunRepository(Path.Combine(directory, "legacy-road.db")); repo.Save(run);
            Require(RunHistory.Hash(RunHistory.Replay(repo.Load(run.Id)).Session) == run.Commands[^1].StateHash);
        });
        Check("Desktop road commands render alternate positions and disable writes in replay", () =>
        {
            var vm = new RunViewModel(new SimulationSession(scenario)); vm.SelectedRoadId = "road-01";
            Require(vm.CloseRoadCommand.CanExecute(null)); vm.CloseRoadCommand.Execute(null); vm.StepCommand.Execute(null); vm.StepCommand.Execute(null);
            Require(vm.ClosedRoadIds.Contains("road-01") && vm.ActiveRoadRoutes.Count == 2 && vm.MapVehicles[0].Position.Y != vm.DepotY);
            var run = vm.Document; vm.LoadReplay(run); Require(!vm.CloseRoadCommand.CanExecute(null));
            vm.NewScenario(ScenarioDefinition.FloodDemo()); Require(vm.RoadIds.Count == 0 && !vm.CloseRoadCommand.CanExecute(null));
        });
        return passed;
    }
}
