namespace Aegis.Simulation;

public sealed record CompletionProjection(string Outcome, int StartTick, int EndTick, int Delivered,
    int Remaining, int? Stock, int Cargo, int AdditionalSupplyRequired, string FinalHash)
{
    public int AdditionalTicks => EndTick - StartTick;
}

// A deterministic continuation of a verified copy, not a trained prediction model.
public static class CompletionPreview
{
    public static CompletionProjection Calculate(RunDocument document, int? replayTick = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = RunHistory.Replay(document, replayTick).Session;
        int start = session.Tick;
        while (session.Status is not (RunStatus.Completed or RunStatus.SupplyExhausted) && session.Tick < 20000)
        {
            cancellationToken.ThrowIfCancellationRequested();
            session.Step();
        }
        int remaining = session.InitialDemand - session.DeliveredTotal;
        int shortage = session.UsesLogistics ? Math.Max(0, remaining - session.DepotStockRemaining!.Value - session.Vehicle.Load) : 0;
        return new(session.Tick == 20000 && session.Status is not (RunStatus.Completed or RunStatus.SupplyExhausted) ? "TickLimit" : session.Status.ToString(),
            start, session.Tick, session.DeliveredTotal, remaining, session.DepotStockRemaining, session.Vehicle.Load, shortage, RunHistory.Hash(session));
    }
}
