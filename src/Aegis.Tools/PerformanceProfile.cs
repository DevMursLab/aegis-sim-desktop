using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Aegis.Simulation;

internal static class PerformanceProfile
{
    public static void Write(string path)
    {
        var scenario = new ScenarioDefinition(2, "Synthetic profiling / 1000 ticks", 1,
            new[] { new ShelterDefinition("A", 1000, 400, 400) }, 1000, 1, 0);
        var document = RunHistory.New(scenario);
        var session = new SimulationSession(scenario);
        var commands = new List<RecordedCommand>();
        for (int i = 0; i < 1000; i++)
        {
            session.Step();
            commands.Add(new(i + 1, new(Guid.NewGuid(), "Step"), RunHistory.Hash(session)));
        }
        document = document with { Commands = commands.ToArray() };
        string expected = RunHistory.Hash(session);
        void Engine()
        {
            var run = new SimulationSession(scenario);
            for (int i = 0; i < 1000; i++) run.Step();
            if (RunHistory.Hash(run) != expected) throw new InvalidOperationException("Profile state mismatch.");
        }
        void Replay()
        {
            if (RunHistory.Hash(RunHistory.Replay(document).Session) != expected) throw new InvalidOperationException("Profile replay mismatch.");
        }
        for (int i = 0; i < 3; i++) { Engine(); Replay(); }
        object Measure(Action action)
        {
            var samples = new List<double>();
            for (int i = 0; i < 15; i++) { var watch = Stopwatch.StartNew(); action(); watch.Stop(); samples.Add(watch.Elapsed.TotalMilliseconds); }
            samples.Sort(); return new { Samples = 15, TicksPerSample = 1000, MedianMilliseconds = samples[7], P95Milliseconds = samples[14] };
        }
        var engine = Measure(Engine); var replay = Measure(Replay);
        var result = new
        {
            Format = 1, Engine = RunHistory.EngineVersion, Runtime = RuntimeInformation.FrameworkDescription,
            OS = RuntimeInformation.OSDescription, Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            LogicalProcessors = Environment.ProcessorCount,
            Processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "not available",
            EngineOnly = engine, VerifiedReplay = replay, ExpectedHash = expected,
            Note = "Three warmups and fifteen serial samples in this process. Includes final hash checks. No UI, SQLite, network or contention measurement. Observed profile, not a production throughput claim."
        };
        string absolute = Path.GetFullPath(path); Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        File.WriteAllText(absolute, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("Measured profile saved: " + absolute);
    }
}
