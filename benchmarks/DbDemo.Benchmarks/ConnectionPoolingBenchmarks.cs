using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DbDemo.Benchmarks;

/// <summary>
/// Benchmarks comparing connection pooling vs. non-pooled connections
/// Demonstrates the performance impact of connection pooling
///
/// Uses SimpleJob for faster execution (1 warm-up, 3 iterations)
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 1, iterationCount: 3)]
public class ConnectionPoolingBenchmarks
{
    private string _pooledConnectionString = null!;
    private string _nonPooledConnectionString = null!;

    [GlobalSetup]
    public void GlobalSetup()
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

        var baseConnectionString = configuration.GetConnectionString("LibraryDb")
            ?? throw new InvalidOperationException("Connection string not found");

        // Pooled connection (default behavior)
        _pooledConnectionString = baseConnectionString + "Pooling=True;Min Pool Size=5;Max Pool Size=100;";

        // Non-pooled connection
        _nonPooledConnectionString = baseConnectionString + "Pooling=False;";
    }

    // =====================================================================
    // Single Query Benchmarks
    // =====================================================================

    [Benchmark(Description = "Single query - Pooled")]
    [BenchmarkCategory("Single Query")]
    public async Task<int> SingleQuery_Pooled()
    {
        await using var connection = new SqlConnection(_pooledConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("SELECT COUNT(*) FROM Books WHERE IsDeleted = 0", connection);
        var result = await command.ExecuteScalarAsync();
        return result != null ? (int)result : 0;
    }

    [Benchmark(Description = "Single query - Non-pooled")]
    [BenchmarkCategory("Single Query")]
    public async Task<int> SingleQuery_NonPooled()
    {
        await using var connection = new SqlConnection(_nonPooledConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("SELECT COUNT(*) FROM Books WHERE IsDeleted = 0", connection);
        var result = await command.ExecuteScalarAsync();
        return result != null ? (int)result : 0;
    }

    // =====================================================================
    // Multiple Sequential Queries Benchmarks
    // =====================================================================

    [Benchmark(Description = "10 sequential queries - Pooled")]
    [BenchmarkCategory("Multiple Sequential")]
    public async Task<int> TenSequentialQueries_Pooled()
    {
        int totalCount = 0;

        for (int i = 0; i < 10; i++)
        {
            await using var connection = new SqlConnection(_pooledConnectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand("SELECT COUNT(*) FROM Books WHERE IsDeleted = 0", connection);
            var result = await command.ExecuteScalarAsync();
            totalCount += result != null ? (int)result : 0;
        }

        return totalCount;
    }

    [Benchmark(Description = "10 sequential queries - Non-pooled")]
    [BenchmarkCategory("Multiple Sequential")]
    public async Task<int> TenSequentialQueries_NonPooled()
    {
        int totalCount = 0;

        for (int i = 0; i < 10; i++)
        {
            await using var connection = new SqlConnection(_nonPooledConnectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand("SELECT COUNT(*) FROM Books WHERE IsDeleted = 0", connection);
            var result = await command.ExecuteScalarAsync();
            totalCount += result != null ? (int)result : 0;
        }

        return totalCount;
    }

    // =====================================================================
    // Connection Open/Close Benchmarks
    // =====================================================================

    [Benchmark(Description = "Open/close connection 50 times - Pooled")]
    [BenchmarkCategory("Connection Open/Close")]
    public async Task OpenClose50Times_Pooled()
    {
        for (int i = 0; i < 50; i++)
        {
            await using var connection = new SqlConnection(_pooledConnectionString);
            await connection.OpenAsync();
            // Connection automatically closed when disposed
        }
    }

    [Benchmark(Description = "Open/close connection 50 times - Non-pooled")]
    [BenchmarkCategory("Connection Open/Close")]
    public async Task OpenClose50Times_NonPooled()
    {
        for (int i = 0; i < 50; i++)
        {
            await using var connection = new SqlConnection(_nonPooledConnectionString);
            await connection.OpenAsync();
            // Connection automatically closed when disposed
        }
    }

    private static string FindProjectRoot(string currentDirectory)
    {
        var directory = new DirectoryInfo(currentDirectory);
        while (directory != null)
        {
            // Prioritize finding .sln file (actual repository root)
            // Don't stop at migrations directory in bin folder
            if (directory.GetFiles("*.sln").Any())
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
