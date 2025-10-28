using DbDemo.ConsoleApp.Infrastructure.Repositories;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace DbDemo.ConsoleApp.Demos;

/// <summary>
/// Demonstrates the performance impact of connection pooling in ADO.NET
/// </summary>
public class ConnectionPoolingDemo
{
    private readonly string _connectionString;
    private readonly IBookRepository _bookRepository;

    public ConnectionPoolingDemo(string connectionString, IBookRepository bookRepository)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _bookRepository = bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));
    }

    /// <summary>
    /// Runs the complete connection pooling demonstration
    /// </summary>
    public async Task RunDemonstrationAsync()
    {
        PrintHeader("CONNECTION POOLING DEMONSTRATION");

        Console.WriteLine("This demo compares the performance of database operations with and without connection pooling.");
        Console.WriteLine();
        Console.WriteLine("Connection pooling reuses existing physical connections instead of creating new ones");
        Console.WriteLine("for each database operation, which significantly improves performance.");
        Console.WriteLine();

        await Task.Delay(2000);

        // Part 1: Sequential queries without pooling
        await RunSequentialQueriesTestAsync(poolingEnabled: false);

        await Task.Delay(1000);

        // Part 2: Sequential queries with pooling
        await RunSequentialQueriesTestAsync(poolingEnabled: true);

        await Task.Delay(1000);

        // Part 3: Concurrent queries comparison
        await RunConcurrentQueriesTestAsync();

        await Task.Delay(1000);

        // Part 4: Connection statistics
        await DisplayConnectionStatisticsAsync();

        PrintSuccess("\n=== CONNECTION POOLING DEMONSTRATION COMPLETED ===\n");
    }

    /// <summary>
    /// Tests sequential database queries with or without connection pooling
    /// </summary>
    private async Task RunSequentialQueriesTestAsync(bool poolingEnabled)
    {
        var poolingStatus = poolingEnabled ? "ENABLED" : "DISABLED";
        PrintHeader($"TEST 1: Sequential Queries - Pooling {poolingStatus}");

        // Modify connection string to enable/disable pooling
        var connectionString = ModifyConnectionString(_connectionString, poolingEnabled);

        PrintInfo($"Connection String Settings:");
        PrintInfo($"  Pooling: {poolingEnabled}");
        PrintInfo($"  Operations to perform: 50 sequential queries");
        Console.WriteLine();

        await Task.Delay(500);

        PrintStep("Starting performance test...");

        var stopwatch = Stopwatch.StartNew();
        var operationCount = 50;

        try
        {
            for (int i = 0; i < operationCount; i++)
            {
                // Perform a simple query - getting book count
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                await using var command = new SqlCommand("SELECT COUNT(*) FROM Books", connection);
                var count = await command.ExecuteScalarAsync();

                // Show progress every 10 operations
                if ((i + 1) % 10 == 0)
                {
                    Console.Write(".");
                }
            }

            stopwatch.Stop();
            Console.WriteLine(); // New line after dots

            var averageMs = stopwatch.ElapsedMilliseconds / (double)operationCount;

            PrintSuccess($"\nCompleted {operationCount} operations in {stopwatch.ElapsedMilliseconds:N0} ms");
            PrintInfo($"  Total Time: {stopwatch.ElapsedMilliseconds:N0} ms");
            PrintInfo($"  Average per operation: {averageMs:F2} ms");
            PrintInfo($"  Operations per second: {(operationCount / stopwatch.Elapsed.TotalSeconds):F2}");

            if (!poolingEnabled)
            {
                PrintWarning("\n⚠ Without pooling: Each operation creates a new physical connection!");
                PrintWarning("  This is slow because establishing a connection involves:");
                PrintWarning("  - TCP/IP handshake");
                PrintWarning("  - Authentication");
                PrintWarning("  - Session initialization");
            }
            else
            {
                PrintSuccess("\n✓ With pooling: Connections are reused from the pool!");
                PrintSuccess("  Much faster because existing connections are reused.");
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            PrintError($"Error during test: {ex.Message}");
            throw;
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Tests concurrent database queries to show pooling benefits under load
    /// </summary>
    private async Task RunConcurrentQueriesTestAsync()
    {
        PrintHeader("TEST 2: Concurrent Queries Comparison");

        PrintInfo("Simulating 20 concurrent requests (like a web application under load)");
        Console.WriteLine();

        await Task.Delay(500);

        // Test WITHOUT pooling
        PrintStep("Phase 1: WITHOUT connection pooling...");
        var connectionStringNoPool = ModifyConnectionString(_connectionString, poolingEnabled: false);
        var stopwatchNoPool = Stopwatch.StartNew();

        var tasksNoPool = new List<Task>();
        for (int i = 0; i < 20; i++)
        {
            tasksNoPool.Add(ExecuteSimpleQueryAsync(connectionStringNoPool, i));
        }

        await Task.WhenAll(tasksNoPool);
        stopwatchNoPool.Stop();

        PrintSuccess($"Completed in {stopwatchNoPool.ElapsedMilliseconds:N0} ms (without pooling)");
        Console.WriteLine();

        await Task.Delay(500);

        // Test WITH pooling
        PrintStep("Phase 2: WITH connection pooling...");
        var connectionStringWithPool = ModifyConnectionString(_connectionString, poolingEnabled: true);
        var stopwatchWithPool = Stopwatch.StartNew();

        var tasksWithPool = new List<Task>();
        for (int i = 0; i < 20; i++)
        {
            tasksWithPool.Add(ExecuteSimpleQueryAsync(connectionStringWithPool, i));
        }

        await Task.WhenAll(tasksWithPool);
        stopwatchWithPool.Stop();

        PrintSuccess($"Completed in {stopwatchWithPool.ElapsedMilliseconds:N0} ms (with pooling)");
        Console.WriteLine();

        // Calculate improvement
        var improvementPercent = ((stopwatchNoPool.ElapsedMilliseconds - stopwatchWithPool.ElapsedMilliseconds) /
                                  (double)stopwatchNoPool.ElapsedMilliseconds) * 100;

        PrintSuccess("📊 PERFORMANCE COMPARISON:");
        PrintInfo($"  Without Pooling: {stopwatchNoPool.ElapsedMilliseconds:N0} ms");
        PrintInfo($"  With Pooling:    {stopwatchWithPool.ElapsedMilliseconds:N0} ms");
        PrintSuccess($"  Improvement:     {improvementPercent:F1}% faster with pooling");
        Console.WriteLine();
    }

    /// <summary>
    /// Displays connection pool statistics and configuration
    /// </summary>
    private async Task DisplayConnectionStatisticsAsync()
    {
        PrintHeader("CONNECTION POOL STATISTICS");

        PrintInfo("Connection Pool Configuration Options:");
        PrintInfo("  Min Pool Size:     Minimum connections kept in pool (default: 0)");
        PrintInfo("  Max Pool Size:     Maximum connections in pool (default: 100)");
        PrintInfo("  Connection Timeout: Time to wait for connection (default: 15s)");
        PrintInfo("  Connection Lifetime: Max lifetime of pooled connection (default: 0 = unlimited)");
        Console.WriteLine();

        PrintStep("Example connection string configurations:");
        Console.WriteLine();

        var examples = new[]
        {
            new { Name = "Default (Recommended)", ConnStr = "Pooling=true;Min Pool Size=0;Max Pool Size=100" },
            new { Name = "High Concurrency App", ConnStr = "Pooling=true;Min Pool Size=10;Max Pool Size=200" },
            new { Name = "Low Traffic App", ConnStr = "Pooling=true;Min Pool Size=1;Max Pool Size=50" },
            new { Name = "No Pooling (Not Recommended)", ConnStr = "Pooling=false" }
        };

        foreach (var example in examples)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  {example.Name}:");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"    {example.ConnStr}");
            Console.ResetColor();
            Console.WriteLine();
        }

        PrintInfo("💡 Best Practices:");
        PrintInfo("  ✓ Always keep pooling enabled (default)");
        PrintInfo("  ✓ Use 'await using' for proper connection disposal");
        PrintInfo("  ✓ Keep connections open for shortest time possible");
        PrintInfo("  ✓ Let the pool manage connection lifecycle");
        PrintInfo("  ✗ Don't manually cache connections - use the pool!");
        Console.WriteLine();

        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes a simple query (helper method for concurrent tests)
    /// </summary>
    private async Task ExecuteSimpleQueryAsync(string connectionString, int queryNumber)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("SELECT COUNT(*) FROM Books WHERE Id > @Id", connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = queryNumber;

        var result = await command.ExecuteScalarAsync();
    }

    /// <summary>
    /// Modifies a connection string to enable or disable connection pooling
    /// </summary>
    private string ModifyConnectionString(string originalConnectionString, bool poolingEnabled)
    {
        var builder = new SqlConnectionStringBuilder(originalConnectionString)
        {
            Pooling = poolingEnabled
        };

        // Also set some explicit pool settings for demonstration
        if (poolingEnabled)
        {
            builder.MinPoolSize = 0;
            builder.MaxPoolSize = 100;
            builder.ConnectTimeout = 15;
        }

        return builder.ConnectionString;
    }

    #region Console Output Helpers

    private void PrintHeader(string title)
    {
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  {title}");
        Console.ResetColor();
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    private void PrintStep(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"▶ {message}");
        Console.ResetColor();
    }

    private void PrintSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private void PrintInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private void PrintWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ {message}");
        Console.ResetColor();
    }

    #endregion
}
