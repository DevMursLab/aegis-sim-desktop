namespace Aegis.Simulation;

public sealed record RoomSnapshot(long Revision, RunDocument Run, string? Error = null);
public sealed record HostCommandRequest(Guid RunId, int ExpectedVersion, ExerciseCommand Command);
