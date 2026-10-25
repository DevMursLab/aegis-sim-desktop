using Aegis.Presentation;
using Aegis.Simulation;

internal static class AsyncReplayChecks
{
    public static async Task<int> Run()
    {
        var source = RunHistory.New(ScenarioDefinition.FloodDemo());
        for (int i = 0; i < 6; i++) source = RunHistory.Append(source, new(Guid.NewGuid(), "Step"));
        var model = new RunViewModel(SimulationSession.CreateDemo());
        await model.LoadReplayAsync(source);
        Require(model.IsReplay && model.Tick == 6 && !model.IsReplayLoading);
        Pass("Background replay reconstructs the same verified endpoint");
        var first = model.SeekReplayAsync(1); var latest = model.SeekReplayAsync(5);
        await Task.WhenAll(first, latest);
        Require(model.Tick == 5 && model.ReplayTick == 5 && !model.IsReplayLoading);
        Pass("Rapid asynchronous seeks publish only the latest requested tick");
        var previous = model.Document;
        await model.LoadReplayAsync(source with { Commands = source.Commands.Select((r, i) => i == 0 ? r with { StateHash = "invalid" } : r).ToArray() });
        Require(model.Document == previous && model.Tick == 5 && model.Message.Contains("failed safely") && !model.IsReplayLoading);
        Pass("Corrupt background replay preserves the previously displayed run");
        var longRun = LongHistory();
        var pending = model.LoadReplayAsync(longRun);
        model.NewScenario(ScenarioDefinition.LogisticsDemo()); var fresh = model.Document.Id;
        await pending;
        Require(model.Document.Id == fresh && !model.IsReplay && !model.IsReplayLoading && model.Tick == 0);
        Pass("Reset or a new scenario prevents a pending replay from replacing it");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        try { RunHistory.Replay(longRun, cancellationToken: cancellation.Token); throw new Exception("Expected cancellation"); }
        catch (OperationCanceledException) { }
        Pass("Replay honors cancellation before reconstructing a large history");
        return 5;
    }
    public static RunDocument LongHistory()
    {
        var source = RunHistory.New(ScenarioDefinition.FloodDemo());
        var state = new SimulationSession(source.Scenario); state.CloseRoute("Shelter-A", 5);
        string hash = RunHistory.Hash(state);
        return source with { Commands = Enumerable.Range(1, 20000).Select(i => new RecordedCommand(i, new(Guid.NewGuid(), "CloseRoute", "Shelter-A", 5), hash)).ToArray() };
    }
    private static void Require(bool value) { if (!value) throw new Exception("Async replay check failed"); }
    private static void Pass(string message) => Console.WriteLine("PASS: " + message);
}
