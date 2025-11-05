using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using DbDemo.Domain.Entities;
using DbDemo.Infrastructure.BulkOperations;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DbDemo.Benchmarks;

/// <summary>
/// Benchmarks comparing different bulk insert approaches:
/// - Batched INSERT statements
/// - Table-Valued Parameters (TVP)
/// - SqlBulkCopy
///
/// Uses SimpleJob for faster execution (3 warm-up, 5 iterations)
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 1, iterationCount: 3)]
public class BulkInsertBenchmarks
{
    private string _connectionString = null!;
    private BulkBookImporter _bulkImporter = null!;
    private TvpBookImporter _tvpImporter = null!;
    private int _categoryId;

    private List<Book> _books50 = null!;
    private List<Book> _books100 = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Find and load .env file from repository root
        var currentDirectory = Directory.GetCurrentDirectory();
        var repoRoot = FindProjectRoot(currentDirectory);
        var envFile = Path.Combine(repoRoot, ".env");
        if (File.Exists(envFile))
        {
            DotNetEnv.Env.Load(envFile);
        }

        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(currentDirectory)
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        // Expand environment variables in connection strings
        ExpandConnectionStrings(configuration);

        _connectionString = configuration.GetConnectionString("LibraryDb")
            ?? throw new InvalidOperationException("Connection string not found");

        _bulkImporter = new BulkBookImporter(_connectionString);
        _tvpImporter = new TvpBookImporter(_connectionString);

        // Ensure category exists
        _categoryId = await EnsureCategoryExistsAsync();

        // Pre-generate test data
        _books50 = BulkBookImporter.GenerateSampleBooks(50, _categoryId);
        _books100 = BulkBookImporter.GenerateSampleBooks(100, _categoryId);

        // Initial cleanup
        CleanupBooks();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Clean up books before each iteration (synchronous)
        CleanupBooks();
    }

    // =====================================================================
    // 50 Records Benchmarks
    // =====================================================================

    [Benchmark(Description = "Batched INSERTs - 50 records")]
    [BenchmarkCategory("50 Records")]
    public async Task<int> BatchedInserts_50Records()
    {
        var (count, _) = await _bulkImporter.BulkInsertWithBatchedInsertsAsync(_books50, batchSize: 20);
        return count;
    }

    [Benchmark(Description = "TVP - 50 records")]
    [BenchmarkCategory("50 Records")]
    public async Task<int> TvpInsert_50Records()
    {
        var (count, _) = await _tvpImporter.BulkInsertWithTvpAsync(_books50);
        return count;
    }

    [Benchmark(Description = "SqlBulkCopy - 50 records", Baseline = true)]
    [BenchmarkCategory("50 Records")]
    public async Task<int> SqlBulkCopy_50Records()
    {
        var (count, _) = await _bulkImporter.BulkInsertWithSqlBulkCopyAsync(_books50, batchSize: 50);
        return count;
    }

    // =====================================================================
    // 100 Records Benchmarks
    // =====================================================================

    [Benchmark(Description = "Batched INSERTs - 100 records")]
    [BenchmarkCategory("100 Records")]
    public async Task<int> BatchedInserts_100Records()
    {
        var (count, _) = await _bulkImporter.BulkInsertWithBatchedInsertsAsync(_books100, batchSize: 20);
        return count;
    }

    [Benchmark(Description = "TVP - 100 records")]
    [BenchmarkCategory("100 Records")]
    public async Task<int> TvpInsert_100Records()
    {
        var (count, _) = await _tvpImporter.BulkInsertWithTvpAsync(_books100);
        return count;
    }

    [Benchmark(Description = "SqlBulkCopy - 100 records")]
    [BenchmarkCategory("100 Records")]
    public async Task<int> SqlBulkCopy_100Records()
    {
        var (count, _) = await _bulkImporter.BulkInsertWithSqlBulkCopyAsync(_books100, batchSize: 100);
        return count;
    }

    // =====================================================================
    // Helper Methods
    // =====================================================================

    private async Task<int> EnsureCategoryExistsAsync()
    {
        const string checkSql = "SELECT TOP 1 Id FROM Categories ORDER BY Id";
        const string insertSql = @"
            INSERT INTO Categories (Name, Description, IsDeleted, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES ('Benchmark Category', 'Category for benchmark tests', 0, GETUTCDATE(), GETUTCDATE())";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var checkCommand = new SqlCommand(checkSql, connection);
        var existingId = await checkCommand.ExecuteScalarAsync();

        if (existingId != null)
        {
            return (int)existingId;
        }

        await using var insertCommand = new SqlCommand(insertSql, connection);
        var newId = await insertCommand.ExecuteScalarAsync();
        return (int)newId!;
    }

    private void CleanupBooks()
    {
        // Synchronous cleanup to avoid async in IterationSetup
        const string sql = "DELETE FROM Books WHERE Id > 0";

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using var command = new SqlCommand(sql, connection);
        command.ExecuteNonQuery();
    }

    private static string FindProjectRoot(string currentDirectory)
    {
        var directory = new DirectoryInfo(currentDirectory);
        while (directory != null)
        {
            if (directory.GetFiles("*.sln").Any() ||
                directory.GetDirectories("migrations").Any())
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find project root (no .sln file found)");
    }

    private static void ExpandConnectionStrings(IConfiguration configuration)
    {
        var connectionStrings = configuration.GetSection("ConnectionStrings");
        foreach (var conn in connectionStrings.GetChildren())
        {
            var value = conn.Value;
            if (string.IsNullOrEmpty(value)) continue;

            // Replace ${VAR} with environment variable values
            var expanded = System.Text.RegularExpressions.Regex.Replace(
                value,
                @"\$\{([^}]+)\}",
                match =>
                {
                    var varName = match.Groups[1].Value;
                    return Environment.GetEnvironmentVariable(varName) ?? match.Value;
                });

            // Update the configuration value
            if (expanded != value)
            {
                configuration[conn.Path] = expanded;
            }
        }
    }
}
