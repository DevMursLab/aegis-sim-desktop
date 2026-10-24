using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Aegis.Simulation;

public sealed record ExerciseCommand(Guid Id, string Type, string? Target = null, int Duration = 0);
public sealed record RecordedCommand(int Sequence, ExerciseCommand Command, string StateHash);
public sealed record RunDocument(int SchemaVersion, string EngineVersion, Guid Id, DateTimeOffset CreatedAt,
    ScenarioDefinition Scenario, RecordedCommand[] Commands, Guid CreationCommandId = default);
public sealed record ReconstructedRun(SimulationSession Session, DeliveryEvent[] Events);

public static class RunHistory
{
    public const string EngineVersion = "2.0.0";
    public static RunDocument New(ScenarioDefinition scenario)
    {
        scenario.Validate();
        return new(1, EngineVersion, Guid.NewGuid(), DateTimeOffset.UtcNow,
            scenario with { Shelters = scenario.Shelters.ToArray() }, Array.Empty<RecordedCommand>());
    }

    public static string Hash(SimulationSession session) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(new { session.Tick, session.Status, session.DeliveredTotal, session.Shelters }))));

    public static ReconstructedRun Replay(RunDocument document, int? targetTick = null)
    {
        if (document == null || document.SchemaVersion != 1 || document.EngineVersion != EngineVersion || document.Id == Guid.Empty ||
            document.Commands == null || document.Commands.Length > 25000)
            throw new InvalidDataException("Unsupported or invalid run document.");
        var session = new SimulationSession(document.Scenario);
        var events = new List<DeliveryEvent>();
        var ids = new HashSet<Guid>();
        foreach (var record in document.Commands)
        {
            if (record == null || record.Sequence != ids.Count + 1 || record.Command == null || record.Command.Id == Guid.Empty || !ids.Add(record.Command.Id))
                throw new InvalidDataException("Command sequence is invalid or duplicated.");
            if (targetTick.HasValue && record.Command.Type == "Step" && session.Tick >= targetTick.Value) break;
            Apply(session, record.Command, events);
            if (Hash(session) != record.StateHash) throw new InvalidDataException($"Replay checksum mismatch at sequence {record.Sequence}.");
        }
        return new(session, events.ToArray());
    }

    public static RunDocument Append(RunDocument document, ExerciseCommand command)
    {
        var duplicate = document.Commands.FirstOrDefault(c => c.Command.Id == command.Id);
        if (duplicate != null)
        {
            if (duplicate.Command != command) throw new ArgumentException("A command ID cannot be reused for a different action.");
            return document;
        }
        if (document.Commands.Length >= 25000 || command.Id == Guid.Empty) throw new InvalidOperationException("Command limit reached or invalid ID.");
        var replay = Replay(document);
        Apply(replay.Session, command, new List<DeliveryEvent>());
        return document with { Commands = document.Commands.Append(new RecordedCommand(document.Commands.Length + 1, command, Hash(replay.Session))).ToArray() };
    }

    private static void Apply(SimulationSession session, ExerciseCommand command, List<DeliveryEvent> events)
    {
        if (command.Type != "CloseRoute" && (command.Target != null || command.Duration != 0)) throw new ArgumentException("Unexpected command fields.");
        switch (command.Type)
        {
            case "Start": session.Start(); break;
            case "Pause": session.Pause(); break;
            case "Step": events.Add(session.Step()); break;
            case "CloseRoute": session.CloseRoute(command.Target ?? "", command.Duration); break;
            default: throw new ArgumentException("Unknown command type.");
        }
    }
}
