using Aegis.Simulation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Aegis.Infrastructure;

public sealed class StoredRun
{
    public Guid Id { get; set; }
    public string ScenarioName { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public string DocumentJson { get; set; } = "";
    public int Version { get; set; }
    public int Tick { get; set; }
    public string StateHash { get; set; } = "";
    public string Status { get; set; } = "";
    public override string ToString() => $"{ScenarioName} | tick {Tick} | {Status} | {CreatedAt[..16]} | {Id.ToString()[..8]}";
}
public sealed class StoredCommand
{
    public Guid RunId { get; set; }
    public int Sequence { get; set; }
    public Guid CommandId { get; set; }
    public string Json { get; set; } = "";
}
public sealed class StoredSnapshot
{
    public Guid RunId { get; set; }
    public int Sequence { get; set; }
    public int Tick { get; set; }
    public string StateJson { get; set; } = "";
    public string Hash { get; set; } = "";
}
public sealed class StoredEvent
{
    public Guid RunId { get; set; }
    public int Tick { get; set; }
    public string Json { get; set; } = "";
}
public sealed class RunDatabase(string path) : DbContext
{
    public DbSet<StoredRun> Runs => Set<StoredRun>();
    public DbSet<StoredCommand> Commands => Set<StoredCommand>();
    public DbSet<StoredSnapshot> Snapshots => Set<StoredSnapshot>();
    public DbSet<StoredEvent> Events => Set<StoredEvent>();
    protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite(new SqliteConnectionStringBuilder { DataSource = path, DefaultTimeout = 10 }.ToString());
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<StoredRun>().HasKey(r => r.Id);
        model.Entity<StoredCommand>().HasKey(c => new { c.RunId, c.Sequence });
        model.Entity<StoredCommand>().HasIndex(c => new { c.RunId, c.CommandId }).IsUnique();
        model.Entity<StoredSnapshot>().HasKey(s => new { s.RunId, s.Sequence });
        model.Entity<StoredSnapshot>().HasIndex(s => new { s.RunId, s.Tick });
        model.Entity<StoredEvent>().HasKey(e => new { e.RunId, e.Tick });
    }
}

public sealed class RunRepository
{
    public string DatabasePath { get; }
    public RunRepository(string path)
    {
        DatabasePath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        using var db = new RunDatabase(DatabasePath);
        db.Database.EnsureCreated();
        db.Database.OpenConnection();
        using var check = db.Database.GetDbConnection().CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Events'";
        bool hasEventTable = Convert.ToInt32(check.ExecuteScalar()) > 0;
        // Explicit additive upgrade for development databases predating the typed event table.
        db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Events (RunId TEXT NOT NULL, Tick INTEGER NOT NULL, Json TEXT NOT NULL, PRIMARY KEY (RunId, Tick))");
        if (!hasEventTable)
        {
            foreach (var saved in db.Runs.AsNoTracking().ToArray())
            {
                var document = JsonSerializer.Deserialize<RunDocument>(saved.DocumentJson, ScenarioFiles.JsonOptions)!;
                foreach (var e in RunHistory.Replay(document).Events)
                    db.Events.Add(new StoredEvent { RunId = saved.Id, Tick = e.Tick, Json = JsonSerializer.Serialize(e, ScenarioFiles.JsonOptions) });
            }
            db.SaveChanges();
        }
    }
    public StoredRun[] List()
    {
        using var db = new RunDatabase(DatabasePath);
        return db.Runs.AsNoTracking().OrderByDescending(r => r.CreatedAt).Take(200).ToArray();
    }
    public RunDocument Load(Guid id)
    {
        using var db = new RunDatabase(DatabasePath);
        var row = db.Runs.AsNoTracking().Single(r => r.Id == id);
        var document = JsonSerializer.Deserialize<RunDocument>(row.DocumentJson, ScenarioFiles.JsonOptions) ?? throw new InvalidDataException("Invalid saved run.");
        var replay = RunHistory.Replay(document);
        if (RunHistory.Hash(replay.Session) != row.StateHash) throw new InvalidDataException("Saved state checksum mismatch.");
        var commands = db.Commands.AsNoTracking().Where(c => c.RunId == id).OrderBy(c => c.Sequence).ToArray();
        if (commands.Length != document.Commands.Length || commands.Where((c, i) => c.Json != JsonSerializer.Serialize(document.Commands[i], ScenarioFiles.JsonOptions)).Any())
            throw new InvalidDataException("SQL command history does not match the saved run.");
        var savedEvents = db.Events.AsNoTracking().Where(e => e.RunId == id).OrderBy(e => e.Tick).ToArray();
        if (savedEvents.Length != replay.Events.Length || savedEvents.Where((e, i) => e.Json != JsonSerializer.Serialize(replay.Events[i], ScenarioFiles.JsonOptions)).Any())
            throw new InvalidDataException("SQL event history does not match replay.");
        var checkpoint = db.Snapshots.AsNoTracking().Single(s => s.RunId == id && s.Sequence == document.Commands.Length);
        if (checkpoint.Hash != row.StateHash || checkpoint.StateJson != CheckpointJson(replay.Session))
            throw new InvalidDataException("SQL checkpoint does not match replay.");
        return document;
    }
    public void Save(RunDocument document)
    {
        var replay = RunHistory.Replay(document);
        using var db = new RunDatabase(DatabasePath);
        using var transaction = db.Database.BeginTransaction();
        var row = db.Runs.SingleOrDefault(r => r.Id == document.Id);
        int previousVersion = 0;
        if (row != null)
        {
            var old = JsonSerializer.Deserialize<RunDocument>(row.DocumentJson, ScenarioFiles.JsonOptions)!;
            previousVersion = old.Commands.Length;
            if (JsonSerializer.Serialize(old.Scenario, ScenarioFiles.JsonOptions) != JsonSerializer.Serialize(document.Scenario, ScenarioFiles.JsonOptions) ||
                previousVersion > document.Commands.Length || !old.Commands.SequenceEqual(document.Commands.Take(previousVersion)))
                throw new InvalidOperationException("Saved history is immutable; create a new run instead.");
            if (previousVersion == document.Commands.Length) return;
        }
        else { row = new StoredRun { Id = document.Id }; db.Runs.Add(row); }
        row.ScenarioName = document.Scenario.Name;
        row.CreatedAt = document.CreatedAt.ToString("O");
        row.DocumentJson = JsonSerializer.Serialize(document, ScenarioFiles.JsonOptions);
        row.Version = document.Commands.Length;
        row.Tick = replay.Session.Tick;
        row.Status = replay.Session.Status.ToString();
        row.StateHash = RunHistory.Hash(replay.Session);
        foreach (var command in document.Commands.Skip(previousVersion)) db.Commands.Add(new StoredCommand { RunId = document.Id, Sequence = command.Sequence, CommandId = command.Command.Id, Json = JsonSerializer.Serialize(command, ScenarioFiles.JsonOptions) });
        int previousTick = db.Events.Count(e => e.RunId == document.Id);
        foreach (var e in replay.Events.Skip(previousTick)) db.Events.Add(new StoredEvent { RunId = document.Id, Tick = e.Tick, Json = JsonSerializer.Serialize(e, ScenarioFiles.JsonOptions) });
        db.Snapshots.Add(new StoredSnapshot
        {
            RunId = document.Id,
            Sequence = document.Commands.Length,
            Tick = row.Tick,
            Hash = row.StateHash,
            StateJson = CheckpointJson(replay.Session)
        });
        db.SaveChanges();
        transaction.Commit();
    }
    private static string CheckpointJson(SimulationSession session) => session.UsesLogistics ?
        JsonSerializer.Serialize(new { session.Tick, session.DeliveredTotal, session.Shelters, session.DepotStockRemaining, session.RestockedTotal, session.Vehicle }, ScenarioFiles.JsonOptions) :
        JsonSerializer.Serialize(new { session.Tick, session.DeliveredTotal, session.Shelters }, ScenarioFiles.JsonOptions);
}
