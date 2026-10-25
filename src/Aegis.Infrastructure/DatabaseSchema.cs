using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Infrastructure;

public static class DatabaseSchema
{
    public const int Current = 1;
    public static string? Prepare(string path)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, DefaultTimeout = 10 }.ToString()); connection.Open();
        using var command = connection.CreateCommand(); command.CommandText = "PRAGMA user_version"; int version = Convert.ToInt32(command.ExecuteScalar());
        if (version > Current) throw new InvalidDataException("Database was created by a newer application; use that version or a separate data directory.");
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
        var tables = new List<string>(); using (var reader = command.ExecuteReader()) while (reader.Read()) tables.Add(reader.GetString(0));
        if (tables.Count > 0 && !new[] { "Runs", "Commands", "Snapshots" }.All(tables.Contains)) throw new InvalidDataException("Database has an unsupported core schema.");
        if (version == Current && !tables.Contains("SchemaMigrations")) throw new InvalidDataException("Database schema tracking is missing.");
        if (version != 0 || tables.Count == 0) return null;
        string backup = path + ".backup-v0-" + Guid.NewGuid().ToString("N") + ".sqlite";
        using var destination = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = backup }.ToString()); destination.Open();
        connection.BackupDatabase(destination);
        return backup;
    }

    public static void Complete(RunDatabase database)
    {
        database.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS SchemaMigrations (Version INTEGER PRIMARY KEY, Name TEXT NOT NULL, AppliedAt TEXT NOT NULL)");
        string applied = DateTimeOffset.UtcNow.ToString("O");
        database.Database.ExecuteSqlInterpolated($"INSERT OR IGNORE INTO SchemaMigrations (Version,Name,AppliedAt) VALUES (1, 'Tracked bootstrap with verified event history', {applied})");
        database.Database.ExecuteSqlRaw("PRAGMA user_version=1");
    }
}
