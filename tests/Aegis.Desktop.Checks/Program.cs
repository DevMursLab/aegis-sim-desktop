using Aegis.Simulation;
using Aegis.Presentation;

int passed = 0;
Check("Ready command availability", () =>
{
    var vm = Demo(); Require(vm.Status == "Ready" && vm.Tick == 0);
    Require(vm.StartCommand.CanExecute(null) && vm.StepCommand.CanExecute(null));
    Require(!vm.PauseCommand.CanExecute(null));
});
Check("Start does not advance; duplicate start is blocked", () =>
{
    var vm = Demo(); vm.StartCommand.Execute(null); vm.StartCommand.Execute(null);
    Require(vm.IsRunning && vm.Tick == 0 && !vm.StepCommand.CanExecute(null));
    vm.StepCommand.Execute(null); Require(vm.Tick == 0);
    vm.Advance(); Require(vm.Tick == 1 && vm.DeliveredTotal == 10);
});
Check("Pause blocks queued ticks; resume continues", () =>
{
    var vm = Demo(); vm.StartCommand.Execute(null); vm.Advance(); vm.PauseCommand.Execute(null);
    vm.Advance(); Require(vm.Tick == 1 && vm.Status == "Paused" && vm.StartLabel == "Resume");
    vm.StartCommand.Execute(null); vm.Advance(); Require(vm.Tick == 2 && vm.DeliveredTotal == 20);
});
Check("Manual step stays ready or paused", () =>
{
    var vm = Demo(); vm.StepCommand.Execute(null); Require(vm.Tick == 1 && vm.Status == "Ready");
    vm.StartCommand.Execute(null); vm.PauseCommand.Execute(null); vm.StepCommand.Execute(null);
    Require(vm.Tick == 2 && vm.Status == "Paused");
});
Check("Completion locks commands and ignores timer callbacks", () =>
{
    var vm = Demo(); vm.StartCommand.Execute(null);
    for (int i = 0; i < 20; i++) vm.Advance();
    Require(vm.Status == "Completed" && vm.Tick == 6 && vm.Remaining == 0 && vm.DeliveredTotal == 48);
    Require(!vm.StartCommand.CanExecute(null) && !vm.StepCommand.CanExecute(null) && !vm.PauseCommand.CanExecute(null));
    vm.StepCommand.Execute(null); Require(vm.Tick == 6 && vm.Events.Count == 6);
});
Check("Reset running, paused and completed sessions", () =>
{
    foreach (int mode in new[] { 0, 1, 2 })
    {
        var vm = Demo(); vm.StartCommand.Execute(null); vm.Advance();
        if (mode == 1) vm.PauseCommand.Execute(null);
        if (mode == 2) for (int i = 0; i < 6; i++) vm.Advance();
        vm.ResetCommand.Execute(null); vm.Advance();
        Require(vm.Status == "Ready" && vm.Tick == 0 && vm.Remaining == 48 && vm.Events.Count == 0);
        Require(vm.Shelters[0].Remaining == 25 && vm.Shelters[1].Remaining == 5);
    }
});
Check("Repeated runs preserve ordered delivery history", () =>
{
    var vm = Demo(); for (int i = 0; i < 6; i++) vm.StepCommand.Execute(null);
    var first = vm.Events.ToArray(); vm.ResetCommand.Execute(null);
    for (int i = 0; i < 6; i++) vm.StepCommand.Execute(null);
    Require(first.SequenceEqual(vm.Events));
    Require(first.Select(e => e.Amount).SequenceEqual(new[] { 10, 10, 5, 5, 10, 8 }));
});
Check("Each tick conserves supplies and obeys capacity", () =>
{
    foreach (int capacity in new[] { 1, 4, 10, 100 })
    {
        var session = new SimulationSession(new[] { ("A", 25), ("B", 0), ("C", 18) }, capacity);
        while (session.Status != RunStatus.Completed)
        {
            var delivery = session.Step(); Require(delivery.Amount > 0 && delivery.Amount <= capacity);
            Require(session.Shelters.All(s => s.Remaining >= 0));
            Require(session.DeliveredTotal + session.Shelters.Sum(s => s.Remaining) == session.InitialDemand);
        }
    }
});
Check("Reject invalid scenarios before running", () =>
{
    Expect<ArgumentOutOfRangeException>(() => new SimulationSession(new[] { ("A", 1) }, 0));
    Expect<ArgumentException>(() => new SimulationSession(new[] { ("A", -1) }, 10));
    Expect<ArgumentException>(() => new SimulationSession(new[] { ("A", 1), ("a", 1) }, 10));
    Expect<ArgumentException>(() => new SimulationSession(new[] { (" ", 1) }, 10));
    Expect<OverflowException>(() => new SimulationSession(new[] { ("A", int.MaxValue), ("B", 1) }, 10));
});
Check("Empty demand is completed and cannot step", () =>
{
    foreach (var definitions in new[] { Array.Empty<(string, int)>(), new[] { ("A", 0) } })
    {
        var session = new SimulationSession(definitions, 10);
        Require(session.Status == RunStatus.Completed); Expect<InvalidOperationException>(() => session.Step());
        session.Reset(); Require(session.Status == RunStatus.Completed && session.Tick == 0);
    }
});
Check("Domain rejects invalid lifecycle transitions", () =>
{
    var session = SimulationSession.CreateDemo(); Expect<InvalidOperationException>(() => session.Pause());
    session.Start(); Expect<InvalidOperationException>(() => session.Start());
    while (session.Status != RunStatus.Completed) session.Step();
    Expect<InvalidOperationException>(() => session.Start()); Expect<InvalidOperationException>(() => session.Step());
});
Check("External snapshots cannot mutate session", () =>
{
    var session = SimulationSession.CreateDemo(); var snapshot = session.Shelters;
    session.Step(); Require(snapshot[0].Remaining == 15);
    var edited = snapshot[0] with { Remaining = -100 };
    Require(session.Shelters[0].Remaining == 15 && edited.Remaining == -100);
    Expect<NotSupportedException>(() => ((IList<ShelterState>)snapshot)[0] = edited);
});
Check("Property and command notifications reach the screen", () =>
{
    var vm = Demo(); int propertyUpdates = 0, commandUpdates = 0;
    vm.PropertyChanged += (_, _) => propertyUpdates++;
    vm.StartCommand.CanExecuteChanged += (_, _) => commandUpdates++;
    vm.StartCommand.Execute(null); vm.Advance(); vm.PauseCommand.Execute(null); vm.ResetCommand.Execute(null);
    Require(propertyUpdates == 4 && commandUpdates == 4);
});
Check("Map coordinates round-trip across viewport sizes", () =>
{
    foreach (var size in new[] { (800.0, 500.0), (300.0, 900.0), (1200.0, 800.0) })
    {
        var map = new MapViewport(); map.Resize(size.Item1, size.Item2);
        foreach (var point in new[] { new MapPoint(0, 0), new MapPoint(480, 180), new MapPoint(1200, 800) })
        {
            var restored = map.ToWorld(map.ToScreen(point));
            Require(Math.Abs(restored.X - point.X) < 0.00001 && Math.Abs(restored.Y - point.Y) < 0.00001);
        }
    }
});
Check("Zoom keeps its anchor and obeys limits", () =>
{
    var map = new MapViewport(); map.Resize(800, 500);
    var anchor = new MapPoint(310, 190); var before = map.ToWorld(anchor);
    map.ZoomAt(2, anchor); var after = map.ToWorld(anchor);
    Require(Math.Abs(before.X - after.X) < 0.00001 && Math.Abs(before.Y - after.Y) < 0.00001);
    map.ZoomAt(1000, anchor); Require(map.Zoom == 5);
    map.ZoomAt(0.00001, anchor); Require(map.Zoom == 0.6);
});
Check("Pan and fit never mutate simulation state", () =>
{
    var vm = Demo(); var map = new MapViewport(); map.Resize(800, 500);
    var before = map.ToScreen(new(480, 180)); map.Pan(35, -20);
    var after = map.ToScreen(new(480, 180));
    Require(Math.Abs(after.X - before.X - 35) < 0.00001 && Math.Abs(after.Y - before.Y + 20) < 0.00001);
    map.ZoomAt(2, new(400, 250)); map.Reset();
    Require(map.Zoom == 1 && map.PanX == 0 && map.PanY == 0 && vm.Tick == 0 && vm.Remaining == 48);
});
Check("Hit-testing works after zoom and pan", () =>
{
    var vm = Demo(); var map = new MapViewport(); map.Resize(800, 500);
    map.ZoomAt(1.8, new(350, 220)); map.Pan(20, -10);
    var node = vm.MapNodes[1]; var screen = map.ToScreen(new(node.X, node.Y));
    Require(map.HitTest(vm.MapNodes, screen) == node.Name);
    Require(map.HitTest(vm.MapNodes, new(-10000, -10000)) == null);
});
Check("Invalid viewport inputs cannot create NaN", () =>
{
    var map = new MapViewport();
    Expect<ArgumentOutOfRangeException>(() => map.Resize(0, 500));
    Expect<ArgumentOutOfRangeException>(() => map.Resize(double.NaN, 500));
    Expect<ArgumentOutOfRangeException>(() => map.ZoomAt(double.NaN, new(0, 0)));
    Expect<ArgumentOutOfRangeException>(() => map.ZoomAt(0, new(0, 0)));
    Expect<ArgumentOutOfRangeException>(() => map.Pan(double.PositiveInfinity, 0));
});
Check("Map, selection and table agree after delivery and reset", () =>
{
    var vm = Demo(); vm.SelectedShelterName = "Shelter-A";
    vm.StepCommand.Execute(null);
    Require(vm.MapNodes[0].Remaining == 15 && vm.Shelters[0].Remaining == 15);
    Require(vm.SelectedShelterName == "Shelter-A" && vm.SelectedDetail.Contains("15 remaining"));
    Require(vm.LastDeliveryShelter == "Shelter-A" && vm.CompletionPercent > 20);
    vm.SelectedShelterName = "unknown"; Require(vm.SelectedShelterName == "Shelter-A");
    vm.ResetCommand.Execute(null);
    Require(vm.SelectedDetail.Contains("25 remaining") && vm.LastDeliveryShelter == null && vm.CompletionPercent == 0);
});
Check("Completion and zero-demand progress are bounded", () =>
{
    var vm = Demo(); for (int i = 0; i < 6; i++) vm.StepCommand.Execute(null);
    Require(vm.CompletionPercent == 100 && vm.MapNodes.All(n => n.Remaining == 0));
    var empty = new RunViewModel(new SimulationSession(Array.Empty<(string, int)>(), 10));
    Require(empty.CompletionPercent == 100 && empty.MapNodes.Count == 0 && empty.SelectedDetail.StartsWith("Choose"));
});
Console.WriteLine($"All {passed} desktop checks passed.");

static RunViewModel Demo() => new(SimulationSession.CreateDemo());
static void Require(bool result) { if (!result) throw new Exception("Check failed."); }
static void Expect<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}.");
}
void Check(string name, Action action) { action(); passed++; Console.WriteLine($"PASS: {name}"); }
