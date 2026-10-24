namespace Aegis.Simulation;

public sealed record RunMetrics(int Ticks, int Delivered, int Remaining, double CompletionPercent,
    double MeanServiceTick, double ActiveTickPercent, int WaitingTicks, int Closures,
    int? DepotStockRemaining = null, int InTransit = 0, int RestockedTotal = 0, int OutboundTicks = 0, int ReturnTicks = 0)
{
    public static RunMetrics Calculate(RunDocument document)
    {
        var replay = RunHistory.Replay(document);
        int delivered = replay.Session.DeliveredTotal;
        int ticks = replay.Session.Tick;
        return new(ticks, delivered, replay.Session.InitialDemand - delivered,
            replay.Session.InitialDemand == 0 ? 100 : 100.0 * delivered / replay.Session.InitialDemand,
            delivered == 0 ? 0 : replay.Events.Sum(e => (double)e.Tick * e.Amount) / delivered,
            ticks == 0 ? 0 : 100.0 * replay.Events.Count(e => e.Amount > 0) / ticks,
            replay.Events.Count(e => e.Kind is "Waiting" or "RouteWait"), document.Commands.Count(c => c.Command.Type == "CloseRoute"),
            replay.Session.DepotStockRemaining, replay.Session.Vehicle.Load, replay.Session.RestockedTotal,
            replay.Session.UsesLogistics ? replay.Events.Count(e => e.Kind is "Travelling" or "Delivery" or "RouteWait") : 0,
            replay.Events.Count(e => e.Kind is "Returning" or "Returned"));
    }
}
