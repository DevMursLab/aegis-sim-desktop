namespace Aegis.Simulation;

public sealed record RunMetrics(int Ticks, int Delivered, int Remaining, double CompletionPercent,
    double MeanServiceTick, double ActiveTickPercent, int WaitingTicks, int Closures,
    int? DepotStockRemaining = null, int InTransit = 0, int RestockedTotal = 0, int OutboundTicks = 0, int ReturnTicks = 0)
{
    public static RunMetrics Calculate(RunDocument document)
    {
        var replay = RunHistory.Replay(document);
        return Calculate(replay, document.Commands.Count(c => c.Command.Type is "CloseRoute" or "CloseRoad"));
    }

    // Callers that already verified a replay can reuse it instead of replaying
    // the complete command history again just to aggregate its events.
    public static RunMetrics Calculate(ReconstructedRun replay, int closures)
    {
        int delivered = replay.Session.DeliveredTotal;
        int ticks = replay.Session.Tick;
        return new(ticks, delivered, replay.Session.InitialDemand - delivered,
            replay.Session.InitialDemand == 0 ? 100 : 100.0 * delivered / replay.Session.InitialDemand,
            delivered == 0 ? 0 : replay.Events.Sum(e => (double)e.Tick * e.Amount) / delivered,
            ticks == 0 ? 0 : 100.0 * replay.Events.Count(e => e.Amount > 0) / ticks,
            replay.Events.Count(e => e.Kind is "Waiting" or "RouteWait" or "RoadWait" or "ReturnRoadWait" || e.Actions?.Any(a => a.Kind is "Waiting" or "RouteWait" or "RoadWait" or "ReturnRoadWait") == true), closures,
            replay.Session.DepotStockRemaining, replay.Session.InTransit, replay.Session.RestockedTotal,
            replay.Session.UsesLogistics ? replay.Events.Sum(e => e.Actions != null ? e.Actions.Count(a => a.Kind is "Travelling" or "Delivery" or "RouteWait" or "RoadWait") : e.Kind is "Travelling" or "Delivery" or "RouteWait" or "RoadWait" ? 1 : 0) : 0,
            replay.Events.Sum(e => e.Actions != null ? e.Actions.Count(a => a.Kind is "Returning" or "Returned" or "ReturnRoadWait") : e.Kind is "Returning" or "Returned" or "ReturnRoadWait" ? 1 : 0));
    }
}
