using System.Text.Json;
using Aegis.Infrastructure;
using Aegis.Presentation;
using Aegis.Simulation;
using Microsoft.Data.Sqlite;

internal static class JunctionRoutingChecks
{
    public static int Run(ScenarioDefinition scenario, string directory)
    {
        int passed = 0;
        void Require(bool value) { if (!value) throw new Exception("Junction routing check failed"); }
        void Check(string name, Action action) { action(); passed++; Console.WriteLine("PASS: " + name); }
        void Reject(Action action)
        {
            try { action(); } catch (Exception e) when (e is ArgumentException or InvalidDataException or InvalidOperationException) { return; }
            throw new Exception("Expected junction rejection");
        }
        SimulationSession Session() { var s = new SimulationSession(scenario); s.EnableJunctionRouting(); return s; }
        void Step(SimulationSession s, int count) { for (int i = 0; i < count; i++) s.Step(); }
        RunDocument Command(RunDocument run, string type, string? target = null, int duration = 0) => RunHistory.Append(run, new(Guid.NewGuid(), type, target, duration));
        void Conserved(SimulationSession s) => Require(s.DeliveredTotal + s.InTransit + s.DepotStockRemaining == s.Scenario.DepotStock + s.RestockedTotal && s.Shelters.All(x => x.Remaining >= 0));
        void Finish(SimulationSession s)
        {
            while (s.Status is not (RunStatus.Completed or RunStatus.SupplyExhausted)) { Require(s.Tick < 150); s.Step(); Conserved(s); }
        }
        Check("Junction travel advances within one road and reaches its exact endpoint", () =>
        {
            var s = Session(); s.Step(); Require(s.Vehicle.Journey!.Node == "depot" && s.InTransit == 20);
            s.Step(); var moving = s.Vehicle.Journey!;
            Require(moving.RoadId == "road-01" && moving.MetersRemaining == 145 && moving.Position != scenario.Geography!.Depot);
            s.Step(); var node = s.Vehicle.Journey!;
            Require(node.Node == "junction" && node.RoadId == null && node.Position == RoadRouting.NodePoint(scenario.Geography!, "junction") && s.DeliveredTotal == 0);
            Conserved(s);
        });
        Check("Closure ahead reroutes at the next junction without teleporting or redirecting cargo", () =>
        {
            var s = Session(); Step(s, 2); s.CloseRoad("road-03", 10); s.Step(); var point = s.Vehicle.Journey!.Position;
            var e = s.Step(); var vehicle = s.Vehicle;
            Require(vehicle.Journey!.RoadId == "road-02" && vehicle.Journey.Points[0] == point);
            Require(!vehicle.Route!.RoadIds.Contains("road-03") && vehicle.Destination == "Road Shelter-B" && vehicle.Load == 10);
            Require(e.Actions![0].Reason!.Contains("entering road-02 from junction")); Conserved(s);
        });
        Check("An occupied road finishes after closure instead of freezing or jumping", () =>
        {
            var s = Session(); Step(s, 2); s.CloseRoad("road-01", 5); var e = s.Step();
            Require(e.Actions![0].Kind == "Travelling" && s.Vehicle.Journey!.Node == "junction" && s.Vehicle.Journey.RoadId == null);
            s.Step(); Require(s.Vehicle.Journey!.RoadId == "road-03"); Conserved(s);
        });
        Check("Closing an already traversed road does not halt downstream travel", () =>
        {
            var s = Session(); Step(s, 3); s.CloseRoad("road-01", 5); s.Step();
            Require(s.Vehicle.Journey!.RoadId == "road-03" && s.Vehicle.Journey.MetersRemaining == 145);
        });
        Check("No open junction path retains cargo and position until exact closure expiry", () =>
        {
            var s = Session(); Step(s, 3); var point = s.Vehicle.Journey!.Position;
            foreach (string id in new[] { "road-01", "road-02", "road-03" }) s.CloseRoad(id, 2);
            for (int i = 0; i < 2; i++) { var e = s.Step(); Require(e.Actions!.All(a => a.Kind == "RoadWait")); Require(s.Vehicle.Journey!.Position == point && s.InTransit == 20); }
            s.Step(); Require(s.Vehicle.Journey!.RoadId == "road-03" && s.Tick == 6); Conserved(s);
        });
        Check("Return journeys choose a different open path without duplicate delivery", () =>
        {
            var s = Session(); Step(s, 5); Require(s.Vehicle.Phase == VehiclePhase.Returning && s.Vehicle.Journey!.Node == "shelter-b");
            var point = s.Vehicle.Journey!.Position; s.CloseRoad("road-03", 10); s.Step();
            Require(s.Vehicle.Journey!.RoadId == "road-04" && s.Vehicle.Journey.Points[0] == point && s.Vehicle.Load == 0 && s.DeliveredTotal == 10);
            Finish(s); Require(s.Status == RunStatus.Completed && s.DeliveredTotal == 30);
        });
        Check("Shelter closures still pause only the assigned outbound service", () =>
        {
            var s = Session(); Step(s, 2); var point = s.Vehicle.Journey!.Position; s.CloseRoute("Road Shelter-B", 2);
            for (int i = 0; i < 2; i++) { var e = s.Step(); Require(e.Actions![0].Kind == "RouteWait" && s.Vehicle.Journey!.Position == point); }
            s.Step(); Require(s.Vehicle.Journey!.Node == "junction" && s.Vehicle.Journey.RoadId == null); Conserved(s);
        });
        Check("Unused movement budget never crosses a second road in the same tick", () =>
        {
            var fast = scenario with { Geography = scenario.Geography! with { MetersPerTick = 100000 }, Shelters = scenario.Shelters.Select(x => x with { TravelTicks = 1 }).ToArray() };
            var s = new SimulationSession(fast); s.EnableJunctionRouting(); Step(s, 2);
            Require(s.Vehicle.Journey!.Node == "junction" && s.DeliveredTotal == 0);
            s.Step(); Require(s.Vehicle.Phase == VehiclePhase.Returning && s.DeliveredTotal == 10); Conserved(s);
        });
        Check("Junction fleet matrix conserves stock and completes all returns after incidents", () =>
        {
            foreach (int count in new[] { 1, 2, 8 })
            {
                var s = new SimulationSession(scenario with { FleetSize = count }); s.EnableJunctionRouting(); Step(s, 2);
                s.CloseRoad("road-01", 3); s.CloseRoad("road-03", 6); Finish(s);
                Require(s.Status == RunStatus.Completed && s.DeliveredTotal == 30 && s.Vehicles.All(v => v.State.Phase == VehiclePhase.AtDepot));
            }
        });
        Check("Junction mode enables only at the depot and remains idempotent until reset", () =>
        {
            var s = new SimulationSession(scenario); s.Step(); string hash = RunHistory.Hash(s); Reject(s.EnableJunctionRouting);
            Require(!s.JunctionRoutingEnabled && hash == RunHistory.Hash(s)); s.Reset(); s.EnableJunctionRouting(); s.Step();
            hash = RunHistory.Hash(s); s.EnableJunctionRouting(); Require(hash == RunHistory.Hash(s)); s.Reset(); Require(!s.JunctionRoutingEnabled && s.Vehicle.Journey == null);
            Reject(() => new SimulationSession(ScenarioDefinition.FloodDemo()).EnableJunctionRouting());
            var done = Session(); Finish(done); Reject(done.EnableJunctionRouting);
        });
        Check("Older engine documents and malformed junction command fields are refused", () =>
        {
            var run = RunHistory.New(scenario);
            foreach (string version in new[] { "3.0.0", "3.1.0" })
            {
                Reject(() => Command(run with { EngineVersion = version }, "EnableJunctionRouting"));
                Reject(() => RunHistory.Replay(Command(run, "EnableJunctionRouting") with { EngineVersion = version }));
            }
            Reject(() => Command(run, "EnableJunctionRouting", "road-01"));
            Reject(() => RunHistory.Append(run, new(Guid.NewGuid(), "EnableJunctionRouting", Amount: 1)));
        });
        Check("Returned junction road geometry cannot mutate hashes or positions", () =>
        {
            var s = Session(); Step(s, 2); string hash = RunHistory.Hash(s); var point = s.Vehicle.Journey!.Position;
            s.Vehicles[0].State.Journey!.Points[0] = new(0, 0); s.Vehicle.Journey!.Points[1] = new(0, 0);
            Require(hash == RunHistory.Hash(s) && s.Vehicle.Journey!.Position == point);
        });
        Check("Junction preview replay SQL and checkpoint tamper detection agree", () =>
        {
            var run = Command(RunHistory.New(scenario), "EnableJunctionRouting");
            for (int i = 0; i < 3; i++) run = Command(run, "Step");
            run = Command(Command(run, "CloseRoad", "road-03", 10), "Step");
            var projected = CompletionPreview.Calculate(run);
            var repo = new RunRepository(Path.Combine(directory, "junction.db")); repo.Save(run);
            var loaded = repo.Load(run.Id); Require(RunHistory.Hash(RunHistory.Replay(loaded).Session) == run.Commands[^1].StateHash);
            var prefix = RunHistory.Replay(loaded, 3).Session; Require(prefix.JunctionRoutingEnabled && prefix.Vehicle.Journey!.Node == "junction" && prefix.Vehicle.Journey.RoadId == null);
            while (RunHistory.Replay(run).Session.Status != RunStatus.Completed) run = Command(run, "Step");
            Require(projected.FinalHash == RunHistory.Hash(RunHistory.Replay(run).Session));
            using var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = repo.DatabasePath }.ToString()); db.Open();
            using var command = db.CreateCommand(); command.CommandText = "UPDATE Snapshots SET StateJson = REPLACE(StateJson, 'junction', 'forged')"; command.ExecuteNonQuery();
            Reject(() => repo.Load(loaded.Id));
        });
        Check("Unranked junction reports include mode timing policy and route decision traces", () =>
        {
            var plain = scenario with { Shelters = scenario.Shelters.Select(x => x with { Priority = 0 }).ToArray() };
            var run = Command(RunHistory.New(plain), "EnableJunctionRouting");
            for (int i = 0; i < 4; i++) run = Command(run, "Step");
            string output = Path.Combine(directory, "junction-reports"); Reports.Export(output, run);
            string stem = Path.Combine(output, "run-" + run.Id.ToString("N"));
            Require(File.ReadAllText(stem + ".html").Contains("Junction routing enabled") && File.ReadAllText(stem + "-vehicles.csv").Contains("Junction path:"));
            Require(File.Exists(stem + ".pdf"));
        });
        Check("Original engine 3.1 incident fixture retains whole-trip waiting and hashes", () =>
        {
            var run = JsonSerializer.Deserialize<RunDocument>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "legacy-v31-road-incidents.json")), ScenarioFiles.JsonOptions)!;
            var replay = RunHistory.Replay(run); Require(run.EngineVersion == "3.1.0" && replay.Session.Tick == 26 && !replay.Session.JunctionRoutingEnabled);
            Require(replay.Events.Any(e => e.Actions!.Any(a => a.Kind == "RoadWait")));
            var repo = new RunRepository(Path.Combine(directory, "legacy-v31.db")); repo.Save(run); Require(RunHistory.Hash(RunHistory.Replay(repo.Load(run.Id)).Session) == run.Commands[^1].StateHash);
            Reject(() => Command(run, "EnableJunctionRouting"));
        });
        Check("Desktop junction commands preserve mode in replay and reset into fresh setup", () =>
        {
            var vm = new RunViewModel(new SimulationSession(scenario)); Require(vm.EnableJunctionRoutingCommand.CanExecute(null));
            vm.EnableJunctionRoutingCommand.Execute(null); vm.StepCommand.Execute(null); vm.StepCommand.Execute(null);
            Require(vm.JunctionRoutingEnabled && !vm.EnableJunctionRoutingCommand.CanExecute(null) && vm.MapVehicles[0].Position.X > vm.DepotX);
            var run = vm.Document; vm.LoadReplay(run); Require(vm.JunctionRoutingEnabled && !vm.EnableJunctionRoutingCommand.CanExecute(null));
            vm.NewScenario(scenario); Require(!vm.JunctionRoutingEnabled && vm.EnableJunctionRoutingCommand.CanExecute(null));
            vm.NewScenario(ScenarioDefinition.FloodDemo()); Require(!vm.EnableJunctionRoutingCommand.CanExecute(null));
        });
        Check("Junction choices and positions are stable when source road order reverses", () =>
        {
            var a = Session(); var b = new SimulationSession(scenario with { Geography = scenario.Geography! with { Roads = scenario.Geography.Roads.Reverse().ToArray() } }); b.EnableJunctionRouting();
            while (a.Status != RunStatus.Completed)
            {
                Require(a.Tick < 150);
                if (a.Tick == 2) { a.CloseRoad("road-03", 10); b.CloseRoad("road-03", 10); }
                Require(JsonSerializer.Serialize(a.Step()) == JsonSerializer.Serialize(b.Step()));
                Require(a.Vehicles.Select(v => v.State.Journey?.Position).SequenceEqual(b.Vehicles.Select(v => v.State.Journey?.Position)));
            }
            Require(b.Status == RunStatus.Completed);
        });
        Check("Junction supply exhaustion waits for return and restock retains the mode", () =>
        {
            var s = new SimulationSession(scenario with { DepotStock = 10 }); s.EnableJunctionRouting(); Finish(s);
            Require(s.Status == RunStatus.SupplyExhausted && s.DeliveredTotal == 10 && s.InTransit == 0);
            s.Restock(20); Require(s.JunctionRoutingEnabled); Finish(s); Require(s.Status == RunStatus.Completed && s.DeliveredTotal == 30); Conserved(s);
        });
        return passed;
    }
}
