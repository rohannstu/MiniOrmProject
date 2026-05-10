using Npgsql;

namespace MiniOrm.Migrations.Commands;

/// <summary>
/// Handles all migration CLI commands:
///   add       — generates a new .sql migration file
///   apply     — runs pending migrations against the database
///   list      — shows applied and pending migrations
///   rollback  — reverts the last applied migration
/// </summary>
public class MigrationRunner
{
    private readonly string _connectionString;

    // Folder where .sql migration files are stored
    // Resolved relative to the project directory at runtime
    private readonly string _migrationsFolder;

    public MigrationRunner(string connectionString)
    {
        _connectionString = connectionString;

        // Walk up from the running executable to find the Migrations folder
        // The executable runs from bin/Debug/net8.0 — we need to go up to project root
        var baseDir = AppContext.BaseDirectory;
        _migrationsFolder = Path.Combine(baseDir, "..", "..", "..", "Migrations");
        _migrationsFolder = Path.GetFullPath(_migrationsFolder);

        // Create the Migrations folder if it doesn't exist yet
        Directory.CreateDirectory(_migrationsFolder);
    }

    // ADD — generate a new migration file

    /// <summary>
    /// Generates a new timestamped .sql migration file with -- up and -- down sections.
    ///
    /// Example:
    ///   dotnet run -- migrations add CreateProductsTable
    ///   → Migrations/20240510120000_CreateProductsTable.sql
    /// </summary>
    public void Add(string migrationName)
    {
        if (string.IsNullOrWhiteSpace(migrationName))
        {
            Console.WriteLine("ERROR: Migration name cannot be empty.");
            Console.WriteLine("Usage: dotnet run -- migrations add <Name>");
            return;
        }

        // Timestamp prefix ensures migrations are applied in creation order
        // Format: yyyyMMddHHmmss  e.g. 20240510120000
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var filename = $"{timestamp}_{migrationName}.sql";
        var filepath = Path.Combine(_migrationsFolder, filename);

        // Template with -- up and -- down sections
        // The developer fills in the actual SQL after generation
        var template = $"""
                        -- migration: {migrationName}
                        -- created:   {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC

                        -- up
                        -- Write your CREATE TABLE / ALTER TABLE / etc. here


                        -- down
                        -- Write the exact reverse of the above (DROP TABLE / DROP COLUMN / etc.)

                        """;

        File.WriteAllText(filepath, template);

        Console.WriteLine($"Migration created: {filename}");
        Console.WriteLine($"Location: {filepath}");
        Console.WriteLine("Edit the file and fill in the -- up and -- down sections.");
    }


    // APPLY — run all pending migrations

    /// <summary>
    /// Applies all pending migrations in filename order.
    /// Skips migrations already recorded in __migrations.
    /// </summary>
    public void Apply()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        // Ensure the tracking table exists
        EnsureMigrationsTable(connection);

        // Get list of already-applied migration filenames
        var applied = GetAppliedMigrations(connection);

        // Get all .sql files from the Migrations folder, sorted by filename
        // Sorting by filename sorts by timestamp prefix → chronological order
        var files = Directory.GetFiles(_migrationsFolder, "*.sql")
            .OrderBy(f => Path.GetFileName(f))
            .ToList();

        if (files.Count == 0)
        {
            Console.WriteLine("No migration files found.");
            return;
        }

        var pendingCount = 0;

        foreach (var file in files)
        {
            var filename = Path.GetFileName(file);

            // Skip already-applied migrations
            if (applied.Contains(filename))
            {
                Console.WriteLine($"[SKIP]    {filename} (already applied)");
                continue;
            }

            Console.WriteLine($"[APPLY]   {filename}");

            // Read the full file content
            var content = File.ReadAllText(file);

            // Extract only the -- up section
            var upSql = ExtractSection(content, "up");

            if (string.IsNullOrWhiteSpace(upSql))
            {
                Console.WriteLine($"          WARNING: No -- up section found. Skipping.");
                continue;
            }

            // Execute the up SQL against the database
            using var command = new NpgsqlCommand(upSql, connection);
            command.ExecuteNonQuery();

            // Record this migration as applied
            RecordMigration(connection, filename);
            pendingCount++;

            Console.WriteLine($"          Applied successfully.");
        }

        if (pendingCount == 0)
            Console.WriteLine("No pending migrations. Database is up to date.");
        else
            Console.WriteLine($"\n{pendingCount} migration(s) applied.");
    }

    // LIST — show applied and pending migrations

    public void List()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        EnsureMigrationsTable(connection);

        var applied = GetAppliedMigrations(connection);

        var files = Directory.GetFiles(_migrationsFolder, "*.sql")
            .OrderBy(f => Path.GetFileName(f))
            .ToList();

        if (files.Count == 0)
        {
            Console.WriteLine("No migration files found.");
            return;
        }

        Console.WriteLine($"{"Filename",-50} {"Status",-10}");
        Console.WriteLine(new string('─', 62));

        foreach (var file in files)
        {
            var filename = Path.GetFileName(file);
            var status = applied.Contains(filename) ? "applied" : "pending";
            Console.WriteLine($"{filename,-50} {status,-10}");
        }

        Console.WriteLine();
        Console.WriteLine($"Applied: {applied.Count}  |  " +
                          $"Pending: {files.Count - applied.Count}");
    }


    // ROLLBACK

    /// <summary>
    /// Finds the most recently applied migration, runs its -- down section,
    /// and removes it from __migrations.
    /// </summary>
    public void Rollback()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        EnsureMigrationsTable(connection);

        // Get the last applied migration (ordered by applied_at descending)
        var lastMigration = GetLastAppliedMigration(connection);

        if (lastMigration == null)
        {
            Console.WriteLine("No applied migrations to rollback.");
            return;
        }

        Console.WriteLine($"[ROLLBACK] {lastMigration}");

        var filepath = Path.Combine(_migrationsFolder, lastMigration);

        if (!File.Exists(filepath))
        {
            Console.WriteLine($"ERROR: Migration file not found: {filepath}");
            Console.WriteLine("Cannot rollback — file is missing from Migrations folder.");
            return;
        }

        var content = File.ReadAllText(filepath);
        var downSql = ExtractSection(content, "down");

        if (string.IsNullOrWhiteSpace(downSql))
        {
            Console.WriteLine("ERROR: No -- down section found in migration file.");
            return;
        }

        // Execute the down SQL
        using var command = new NpgsqlCommand(downSql, connection);
        command.ExecuteNonQuery();

        // Remove from tracking table
        RemoveMigrationRecord(connection, lastMigration);

        Console.WriteLine($"           Rolled back successfully.");
    }


    // PRIVATE HELPERS


    /// <summary>
    /// Creates the __migrations tracking table if it doesn't exist.
    /// Safe to call on every run — IF NOT EXISTS means it's idempotent.
    /// </summary>
    private void EnsureMigrationsTable(NpgsqlConnection connection)
    {
        var sql = """
                  CREATE TABLE IF NOT EXISTS __migrations (
                      id          SERIAL PRIMARY KEY,
                      filename    TEXT NOT NULL UNIQUE,
                      applied_at  TIMESTAMP NOT NULL DEFAULT NOW()
                  )
                  """;

        using var command = new NpgsqlCommand(sql, connection);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Returns a HashSet of all filenames already recorded in __migrations.
    /// HashSet gives O(1) lookup when checking if a file is already applied.
    /// </summary>
    private HashSet<string> GetAppliedMigrations(NpgsqlConnection connection)
    {
        var applied = new HashSet<string>();
        var sql = "SELECT filename FROM __migrations ORDER BY applied_at";

        using var command = new NpgsqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
            applied.Add(reader.GetString(0));

        return applied;
    }

    /// <summary>
    /// Returns the filename of the most recently applied migration.
    /// Returns null if no migrations have been applied.
    /// </summary>
    private string? GetLastAppliedMigration(NpgsqlConnection connection)
    {
        var sql = "SELECT filename FROM __migrations ORDER BY applied_at DESC LIMIT 1";

        using var command = new NpgsqlCommand(sql, connection);
        var result = command.ExecuteScalar();

        return result == null || result == DBNull.Value
            ? null
            : (string)result;
    }

    /// <summary>
    /// Inserts a filename into __migrations to mark it as applied.
    /// </summary>
    private void RecordMigration(NpgsqlConnection connection, string filename)
    {
        var sql = "INSERT INTO __migrations (filename) VALUES (@filename)";

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@filename", filename);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Removes a filename from __migrations after a successful rollback.
    /// </summary>
    private void RemoveMigrationRecord(NpgsqlConnection connection, string filename)
    {
        var sql = "DELETE FROM __migrations WHERE filename = @filename";

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@filename", filename);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Extracts the SQL between a section marker and the next section marker (or end of file).
    ///
    /// For section "up":
    ///   Finds "-- up" line, collects everything after it until "-- down" or EOF.
    ///
    /// For section "down":
    ///   Finds "-- down" line, collects everything after it until EOF.
    /// </summary>
    private string ExtractSection(string content, string section)
    {
        var lines = content.Split('\n');
        var result = new List<string>();
        var inside = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Detect section start marker: "-- up" or "-- down"
            if (trimmed == $"-- {section}")
            {
                inside = true;
                continue;
            }

            // Detect start of a DIFFERENT section — stop collecting
            if (inside && trimmed.StartsWith("-- ") &&
                (trimmed == "-- up" || trimmed == "-- down"))
            {
                break;
            }

            if (inside)
                result.Add(line);
        }

        return string.Join('\n', result).Trim();
    }
}