using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace DbDemo.Infrastructure.Migrations;

/// <summary>
/// Automated migration runner with checksum validation and transaction support.
///
/// Philosophy:
/// - Forward-only migrations (never modify applied migrations)
/// - Idempotent (safe to re-run)
/// - Checksum validation prevents tampering
/// - Transaction per migration (atomic)
/// </summary>
public class MigrationRunner
{
    private readonly string _connectionString;
    private readonly string _migrationsPath;

    public MigrationRunner(string connectionString, string migrationsPath)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _migrationsPath = migrationsPath ?? throw new ArgumentNullException(nameof(migrationsPath));
    }

    /// <summary>
    /// Runs all pending migrations in order
    /// </summary>
    /// <returns>Number of migrations executed</returns>
    public async Task<int> RunMigrationsAsync()
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("🚀 Migration Runner");
        Console.WriteLine("========================================");
        Console.WriteLine();

        try
        {
            // 1. Scan migration files from disk
            var allMigrations = ScanMigrationFiles();
            Console.WriteLine($"📂 Found {allMigrations.Count} migration files");

            if (allMigrations.Count == 0)
            {
                Console.WriteLine("⚠️  No migration files found!");
                return 0;
            }

            // 2. Get applied migrations from database
            var appliedMigrations = await GetAppliedMigrationsAsync();
            Console.WriteLine($"✅ Already applied: {appliedMigrations.Count} migrations");

            // 3. Mark which migrations are already applied
            foreach (var migration in allMigrations)
            {
                if (appliedMigrations.TryGetValue(migration.Version, out var applied))
                {
                    migration.IsApplied = true;
                    migration.AppliedAt = applied.AppliedAt;
                    migration.ExecutionTimeMs = applied.ExecutionTimeMs;
                    migration.DatabaseChecksum = applied.Checksum;
                }
            }

            // 4. Validate checksums (detect tampering)
            ValidateChecksums(allMigrations);

            // 5. Get pending migrations
            var pendingMigrations = allMigrations.Where(m => !m.IsApplied).ToList();

            if (pendingMigrations.Count == 0)
            {
                Console.WriteLine("✅ Database is up to date - no pending migrations");
                Console.WriteLine();
                return 0;
            }

            Console.WriteLine($"🆕 Pending migrations: {pendingMigrations.Count}");
            Console.WriteLine();

            // 6. Execute each pending migration
            var executedCount = 0;
            var totalStopwatch = Stopwatch.StartNew();

            foreach (var migration in pendingMigrations)
            {
                await ExecuteMigrationAsync(migration);
                executedCount++;
            }

            totalStopwatch.Stop();

            // 7. Summary
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine($"✅ Migration Complete!");
            Console.WriteLine("========================================");
            Console.WriteLine($"Executed: {executedCount} migrations");
            Console.WriteLine($"Total time: {totalStopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine("========================================");
            Console.WriteLine();

            return executedCount;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("❌ Migration Failed!");
            Console.WriteLine("========================================");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("========================================");
            Console.WriteLine();
            throw;
        }
    }

    /// <summary>
    /// Scans the migrations directory for V*.sql files
    /// </summary>
    private List<MigrationRecord> ScanMigrationFiles()
    {
        var migrationsDir = Path.GetFullPath(_migrationsPath);

        if (!Directory.Exists(migrationsDir))
        {
            throw new DirectoryNotFoundException($"Migrations directory not found: {migrationsDir}");
        }

        var files = Directory.GetFiles(migrationsDir, "V*.sql", SearchOption.TopDirectoryOnly);
        var migrations = new List<MigrationRecord>();

        foreach (var filePath in files)
        {
            var fileName = Path.GetFileName(filePath);

            // Skip V000 - it's the bootstrap script, not a versioned migration
            if (fileName.StartsWith("V000", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Extract version from filename (e.g., V001__initial_schema.sql → "001")
            var match = Regex.Match(fileName, @"^V(\d{3})__", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                Console.WriteLine($"⚠️  Skipping invalid migration filename: {fileName}");
                continue;
            }

            var version = match.Groups[1].Value;
            var sqlContent = File.ReadAllText(filePath);
            var checksum = CalculateSHA256(sqlContent);

            migrations.Add(new MigrationRecord
            {
                Version = version,
                FileName = fileName,
                FilePath = filePath,
                SqlContent = sqlContent,
                Checksum = checksum
            });
        }

        // Sort by version number
        return migrations.OrderBy(m => m.Version).ToList();
    }

    /// <summary>
    /// Gets all applied migrations from the database
    /// </summary>
    private async Task<Dictionary<string, (string Checksum, DateTime AppliedAt, int ExecutionTimeMs)>> GetAppliedMigrationsAsync()
    {
        var applied = new Dictionary<string, (string, DateTime, int)>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Check if migrations history table exists
        var tableExists = false;
        await using (var checkCmd = new SqlCommand(
            "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__MigrationsHistory'",
            connection))
        {
            var result = await checkCmd.ExecuteScalarAsync();
            tableExists = result != null;
        }

        if (!tableExists)
        {
            // Table doesn't exist yet - no migrations applied
            return applied;
        }

        // Get all applied migrations
        await using var cmd = new SqlCommand(
            "SELECT MigrationVersion, Checksum, AppliedAt, ExecutionTimeMs FROM __MigrationsHistory ORDER BY MigrationVersion",
            connection);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var version = reader.GetString(0);
            var checksum = reader.GetString(1);
            var appliedAt = reader.GetDateTime(2);
            var executionTime = reader.GetInt32(3);

            applied[version] = (checksum, appliedAt, executionTime);
        }

        return applied;
    }

    /// <summary>
    /// Validates that checksums match between files and database (detects tampering)
    /// </summary>
    private void ValidateChecksums(List<MigrationRecord> migrations)
    {
        var tamperedMigrations = migrations
            .Where(m => m.IsApplied && !m.ChecksumMatches)
            .ToList();

        if (tamperedMigrations.Any())
        {
            Console.WriteLine();
            Console.WriteLine("❌ CHECKSUM MISMATCH DETECTED!");
            Console.WriteLine("The following migrations have been modified after being applied:");
            Console.WriteLine();

            foreach (var migration in tamperedMigrations)
            {
                Console.WriteLine($"  ❌ {migration.FileName}");
                Console.WriteLine($"     Expected: {migration.DatabaseChecksum}");
                Console.WriteLine($"     Actual:   {migration.Checksum}");
            }

            Console.WriteLine();
            Console.WriteLine("⚠️  IMPORTANT: Never modify an applied migration!");
            Console.WriteLine("   Instead, create a new migration to make changes.");
            Console.WriteLine();

            throw new InvalidOperationException(
                "Migration integrity check failed. One or more applied migrations have been modified. " +
                "See output above for details.");
        }
    }

    /// <summary>
    /// Executes a single migration within a transaction
    /// </summary>
    private async Task ExecuteMigrationAsync(MigrationRecord migration)
    {
        Console.WriteLine($"⏳ Executing: {migration.FileName}...");

        var stopwatch = Stopwatch.StartNew();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            // Split SQL into batches by GO statements
            var batches = SplitSqlBatches(migration.SqlContent);

            foreach (var batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch))
                    continue;

                await using var cmd = new SqlCommand(batch, connection, transaction);
                cmd.CommandTimeout = 300; // 5 minutes timeout
                await cmd.ExecuteNonQueryAsync();
            }

            // Record migration in history
            await using var historyCmd = new SqlCommand(
                @"INSERT INTO __MigrationsHistory (MigrationVersion, FileName, Checksum, AppliedAt, ExecutionTimeMs)
                  VALUES (@Version, @FileName, @Checksum, @AppliedAt, @ExecutionTimeMs)",
                connection, transaction);

            historyCmd.Parameters.AddWithValue("@Version", migration.Version);
            historyCmd.Parameters.AddWithValue("@FileName", migration.FileName);
            historyCmd.Parameters.AddWithValue("@Checksum", migration.Checksum);
            historyCmd.Parameters.AddWithValue("@AppliedAt", DateTime.UtcNow);
            historyCmd.Parameters.AddWithValue("@ExecutionTimeMs", stopwatch.ElapsedMilliseconds);

            await historyCmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            stopwatch.Stop();
            Console.WriteLine($"   ✅ Success ({stopwatch.ElapsedMilliseconds}ms)");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            stopwatch.Stop();

            Console.WriteLine($"   ❌ Failed ({stopwatch.ElapsedMilliseconds}ms)");
            Console.WriteLine($"   Error: {ex.Message}");
            throw new InvalidOperationException($"Migration {migration.FileName} failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Splits SQL script into batches separated by GO statements
    /// </summary>
    private List<string> SplitSqlBatches(string sql)
    {
        // Split by GO statements (case-insensitive, must be on its own line)
        var batches = Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        return batches.Where(b => !string.IsNullOrWhiteSpace(b)).ToList();
    }

    /// <summary>
    /// Calculates SHA256 checksum of a string
    /// </summary>
    private string CalculateSHA256(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
