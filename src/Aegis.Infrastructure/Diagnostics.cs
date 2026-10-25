using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Aegis.Simulation;

namespace Aegis.Infrastructure;

public static class Diagnostics
{
    // Intentionally excludes file paths, credentials, command payloads and host tokens.
    public static string Create(RunDocument document)
    {
        var watch = Stopwatch.StartNew();
        var replay = RunHistory.Replay(document);
        watch.Stop();
        using var process = Process.GetCurrentProcess();
        return JsonSerializer.Serialize(new
        {
            Format = 1, Engine = RunHistory.EngineVersion, ArchiveEngine = document.EngineVersion,
            Runtime = RuntimeInformation.FrameworkDescription, OS = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(), document.Id,
            ScenarioSchema = document.Scenario.SchemaVersion, Commands = document.Commands.Length,
            replay.Session.Tick, Status = replay.Session.Status.ToString(), replay.Session.DeliveredTotal,
            replay.Session.DepotStockRemaining, VehiclePhase = replay.Session.Vehicle.Phase.ToString(),
            Cargo = replay.Session.InTransit, TravelTicksRemaining = replay.Session.Vehicle.TicksRemaining,
            VehicleCount = replay.Session.Vehicles.Count,
            Vehicles = replay.Session.Vehicles.Select(v => new { v.Id, Phase = v.State.Phase.ToString(), Cargo = v.State.Load, TravelTicksRemaining = v.State.TicksRemaining }).ToArray(),
            StateHash = RunHistory.Hash(replay.Session), ReplayMilliseconds = watch.Elapsed.TotalMilliseconds,
            WorkingSetBytes = process.WorkingSet64,
            Note = "One observed process sample; replay timing is not a benchmark. No credentials or file paths included."
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}
