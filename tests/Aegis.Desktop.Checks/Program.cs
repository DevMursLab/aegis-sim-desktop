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
Console.WriteLine($"All {passed} desktop checks passed.");

static RunViewModel Demo() => new(SimulationSession.CreateDemo());
static void Require(bool result) { if (!result) throw new Exception("Check failed."); }
static void Expect<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}.");
}
void Check(string name, Action action) { action(); passed++; Console.WriteLine($"PASS: {name}"); }
