using DbDemo.ConsoleApp.Infrastructure.BulkOperations;
using Microsoft.Data.SqlClient;

namespace DbDemo.ConsoleApp.Demos;

/// <summary>
/// Demonstrates high-performance bulk insert operations
/// Compares SqlBulkCopy, batched inserts, and individual inserts
/// </summary>
public class BulkOperationsDemo
{
    private readonly string _connectionString;
    private readonly BulkBookImporter _importer;

    public BulkOperationsDemo(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _importer = new BulkBookImporter(connectionString);
    }

    /// <summary>
    /// Runs the complete bulk operations demonstration
    /// </summary>
    public async Task RunDemonstrationAsync()
    {
        PrintHeader("BULK OPERATIONS DEMONSTRATION");

        Console.WriteLine("This demo compares different approaches to inserting large volumes of data:");
        Console.WriteLine("1. Individual INSERT statements (baseline - SLOW)");
        Console.WriteLine("2. Batched INSERT statements with transactions (better)");
        Console.WriteLine("3. SqlBulkCopy (FASTEST - recommended for bulk imports)");
        Console.WriteLine();
        Console.WriteLine("We'll insert 10,000 books and measure the performance of each approach.");
        Console.WriteLine();

        await Task.Delay(2000);

        // Ensure we have a category to use
        var categoryId = await EnsureCategoryExistsAsync();

        // Part 1: Small dataset comparison (100 records)
        await RunSmallDatasetComparisonAsync(categoryId);

        await Task.Delay(1500);

        // Part 2: Large dataset comparison (10,000 records)
        await RunLargeDatasetComparisonAsync(categoryId);

        await Task.Delay(1500);

        // Part 3: Batch size comparison for SqlBulkCopy
        await RunBatchSizeComparisonAsync(categoryId);

        PrintSuccess("\n=== BULK OPERATIONS DEMONSTRATION COMPLETED ===\n");
    }

    /// <summary>
    /// Compares methods with a small dataset (100 records)
    /// </summary>
    private async Task RunSmallDatasetComparisonAsync(int categoryId)
    {
        PrintHeader("TEST 1: Small Dataset Comparison (100 records)");

        const int recordCount = 100;
        var books = BulkBookImporter.GenerateSampleBooks(recordCount, categoryId);

        PrintInfo($"Generated {recordCount} sample books for testing");
        Console.WriteLine();

        // Test 1: Individual Inserts
        await CleanupBooksAsync();
        PrintStep("Method 1: Individual INSERT statements...");
        var (count1, time1) = await _importer.BulkInsertWithIndividualInsertsAsync(books);
        PrintSuccess($"Inserted {count1} books in {time1:N0} ms ({(time1 / (double)count1):F2} ms per book)");
        Console.WriteLine();

        // Test 2: Batched Inserts
        await CleanupBooksAsync();
        PrintStep("Method 2: Batched INSERT statements (batch size: 20)...");
        var (count2, time2) = await _importer.BulkInsertWithBatchedInsertsAsync(books, batchSize: 20);
        PrintSuccess($"Inserted {count2} books in {time2:N0} ms ({(time2 / (double)count2):F2} ms per book)");
        Console.WriteLine();

        // Test 3: SqlBulkCopy
        await CleanupBooksAsync();
        PrintStep("Method 3: SqlBulkCopy...");
        var (count3, time3) = await _importer.BulkInsertWithSqlBulkCopyAsync(books, batchSize: 100);
        PrintSuccess($"Inserted {count3} books in {time3:N0} ms ({(time3 / (double)count3):F2} ms per book)");
        Console.WriteLine();

        // Summary
        PrintSuccess("📊 PERFORMANCE COMPARISON (100 records):");
        PrintInfo($"  Individual INSERTs:  {time1,6:N0} ms (baseline)");
        PrintInfo($"  Batched INSERTs:     {time2,6:N0} ms ({(time1 / (double)time2):F1}x faster)");
        PrintInfo($"  SqlBulkCopy:         {time3,6:N0} ms ({(time1 / (double)time3):F1}x faster)");
        Console.WriteLine();
    }

    /// <summary>
    /// Compares methods with a large dataset (10,000 records)
    /// </summary>
    private async Task RunLargeDatasetComparisonAsync(int categoryId)
    {
        PrintHeader("TEST 2: Large Dataset Comparison (10,000 records)");

        const int recordCount = 10000;
        PrintInfo($"Generating {recordCount:N0} sample books...");
        var books = BulkBookImporter.GenerateSampleBooks(recordCount, categoryId);
        PrintSuccess($"Generated {recordCount:N0} books");
        Console.WriteLine();

        long time1 = 0, time2 = 0, time3 = 0;

        // For large datasets, individual inserts would take too long - we'll skip it
        PrintWarning("⏭  Skipping individual INSERTs (would take 30-60 seconds)");
        PrintWarning("   For demonstration, we'll estimate: ~40,000 ms");
        time1 = 40000; // Estimated
        Console.WriteLine();

        // Test 2: Batched Inserts
        await CleanupBooksAsync();
        PrintStep("Method 1: Batched INSERT statements (batch size: 100)...");
        var (count2, time2Actual) = await _importer.BulkInsertWithBatchedInsertsAsync(books, batchSize: 100);
        time2 = time2Actual;
        PrintSuccess($"Inserted {count2:N0} books in {time2:N0} ms ({(time2 / (double)count2):F2} ms per book)");
        Console.WriteLine();

        // Test 3: SqlBulkCopy
        await CleanupBooksAsync();
        PrintStep("Method 2: SqlBulkCopy...");
        var (count3, time3Actual) = await _importer.BulkInsertWithSqlBulkCopyAsync(books, batchSize: 1000);
        time3 = time3Actual;
        PrintSuccess($"Inserted {count3:N0} books in {time3:N0} ms ({(time3 / (double)count3):F2} ms per book)");
        Console.WriteLine();

        // Summary
        PrintSuccess("📊 PERFORMANCE COMPARISON (10,000 records):");
        PrintInfo($"  Individual INSERTs:  {time1,6:N0} ms (estimated baseline)");
        PrintInfo($"  Batched INSERTs:     {time2,6:N0} ms ({(time1 / (double)time2):F1}x faster)");
        PrintInfo($"  SqlBulkCopy:         {time3,6:N0} ms ({(time1 / (double)time3):F1}x faster)");
        Console.WriteLine();

        PrintSuccess("💡 KEY INSIGHTS:");
        PrintInfo($"  • SqlBulkCopy is {(time2 / (double)time3):F1}x faster than batched inserts");
        PrintInfo($"  • SqlBulkCopy handles {(recordCount / (time3 / 1000.0)):N0} records per second");
        PrintInfo($"  • For datasets >1000 records, SqlBulkCopy is the clear winner");
        Console.WriteLine();
    }

    /// <summary>
    /// Tests different batch sizes for SqlBulkCopy
    /// </summary>
    private async Task RunBatchSizeComparisonAsync(int categoryId)
    {
        PrintHeader("TEST 3: SqlBulkCopy Batch Size Comparison");

        const int recordCount = 5000;
        PrintInfo($"Testing SqlBulkCopy with {recordCount:N0} records and different batch sizes");
        Console.WriteLine();

        var books = BulkBookImporter.GenerateSampleBooks(recordCount, categoryId);

        var batchSizes = new[] { 100, 500, 1000, 2500, 5000 };
        var results = new List<(int batchSize, long timeMs)>();

        foreach (var batchSize in batchSizes)
        {
            await CleanupBooksAsync();
            PrintStep($"Testing batch size: {batchSize:N0}...");

            var (count, time) = await _importer.BulkInsertWithSqlBulkCopyAsync(books, batchSize);

            results.Add((batchSize, time));
            PrintSuccess($"  Inserted {count:N0} records in {time:N0} ms ({(time / (double)count):F2} ms per record)");
        }

        Console.WriteLine();
        PrintSuccess("📊 BATCH SIZE COMPARISON:");
        PrintInfo("  Batch Size │ Time (ms) │ Records/sec");
        PrintInfo("  ───────────┼───────────┼─────────────");

        foreach (var (batchSize, time) in results)
        {
            var recordsPerSec = recordCount / (time / 1000.0);
            PrintInfo($"  {batchSize,10:N0} │ {time,9:N0} │ {recordsPerSec,11:N0}");
        }

        Console.WriteLine();

        var optimal = results.OrderBy(r => r.timeMs).First();
        PrintSuccess($"💡 Optimal batch size for this test: {optimal.batchSize:N0} ({optimal.timeMs:N0} ms)");
        PrintInfo("   Note: Optimal batch size depends on:");
        PrintInfo("   • Network latency");
        PrintInfo("   • Record size");
        PrintInfo("   • Server resources");
        PrintInfo("   • Typical range: 1000-5000 records per batch");
        Console.WriteLine();
    }

    #region Helper Methods

    /// <summary>
    /// Ensures a category exists for testing (creates one if needed)
    /// </summary>
    private async Task<int> EnsureCategoryExistsAsync()
    {
        const string checkSql = "SELECT TOP 1 Id FROM Categories ORDER BY Id";
        const string insertSql = "INSERT INTO Categories (Name, Description, IsDeleted, CreatedAt, UpdatedAt) " +
                                "OUTPUT INSERTED.Id " +
                                "VALUES ('Bulk Import Test', 'Category for testing bulk operations', 0, GETUTCDATE(), GETUTCDATE())";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Try to get existing category
        await using var checkCommand = new SqlCommand(checkSql, connection);
        var existingId = await checkCommand.ExecuteScalarAsync();

        if (existingId != null)
        {
            return (int)existingId;
        }

        // Create new category
        await using var insertCommand = new SqlCommand(insertSql, connection);
        var newId = await insertCommand.ExecuteScalarAsync();
        return (int)newId!;
    }

    /// <summary>
    /// Cleans up books table for testing
    /// </summary>
    private async Task CleanupBooksAsync()
    {
        const string sql = "DELETE FROM Books WHERE Id > 0"; // Keep structure, remove all data

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    #endregion

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

    #endregion
}
