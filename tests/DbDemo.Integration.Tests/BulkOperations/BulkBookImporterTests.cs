using DbDemo.ConsoleApp.Infrastructure.BulkOperations;
using DbDemo.ConsoleApp.Models;
using Microsoft.Data.SqlClient;
using Xunit;

namespace DbDemo.Integration.Tests.BulkOperations;

[Collection("Database")]
public class BulkBookImporterTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly BulkBookImporter _importer;

    public BulkBookImporterTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _importer = new BulkBookImporter(_fixture.ConnectionString);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Cleanup test data after each test
        await CleanupBooksAsync();
    }

    [Fact]
    public async Task BulkInsertWithSqlBulkCopy_ShouldInsertAllRecords()
    {
        // Arrange
        var categoryId = await EnsureCategoryExistsAsync();
        var books = BulkBookImporter.GenerateSampleBooks(100, categoryId);

        // Act
        var (insertedCount, elapsedMs) = await _importer.BulkInsertWithSqlBulkCopyAsync(books);

        // Assert
        Assert.Equal(100, insertedCount);
        Assert.True(elapsedMs < 5000, $"Bulk insert took too long: {elapsedMs}ms");

        // Verify count in database
        var countInDb = await GetBookCountAsync();
        Assert.True(countInDb >= 100, $"Expected at least 100 books in database, found {countInDb}");
    }

    [Fact]
    public async Task BulkInsertWithSqlBulkCopy_LargeDataset_ShouldBeVeryFast()
    {
        // Arrange
        var categoryId = await EnsureCategoryExistsAsync();
        var books = BulkBookImporter.GenerateSampleBooks(1000, categoryId);

        // Act
        var (insertedCount, elapsedMs) = await _importer.BulkInsertWithSqlBulkCopyAsync(books, batchSize: 500);

        // Assert
        Assert.Equal(1000, insertedCount);
        Assert.True(elapsedMs < 10000, $"Bulk insert of 1000 records took too long: {elapsedMs}ms (expected < 10s)");

        // Performance assertion: Should be much faster than 1ms per record
        var msPerRecord = elapsedMs / (double)insertedCount;
        Assert.True(msPerRecord < 1.0, $"Performance too slow: {msPerRecord:F3}ms per record (expected < 1ms)");
    }

    [Fact]
    public async Task BulkInsertWithIndividualInserts_SmallDataset_ShouldWork()
    {
        // Arrange
        var categoryId = await EnsureCategoryExistsAsync();
        var books = BulkBookImporter.GenerateSampleBooks(10, categoryId); // Small dataset only

        // Act
        var (insertedCount, elapsedMs) = await _importer.BulkInsertWithIndividualInsertsAsync(books);

        // Assert
        Assert.Equal(10, insertedCount);

        // Verify all books were inserted
        var countInDb = await GetBookCountAsync();
        Assert.True(countInDb >= 10);
    }

    [Fact]
    public async Task BulkInsertWithBatchedInserts_ShouldInsertAllRecords()
    {
        // Arrange
        var categoryId = await EnsureCategoryExistsAsync();
        var books = BulkBookImporter.GenerateSampleBooks(50, categoryId);

        // Act
        var (insertedCount, elapsedMs) = await _importer.BulkInsertWithBatchedInsertsAsync(books, batchSize: 10);

        // Assert
        Assert.Equal(50, insertedCount);

        // Verify all books were inserted
        var countInDb = await GetBookCountAsync();
        Assert.True(countInDb >= 50);
    }

    [Fact]
    public async Task BulkInsertWithSqlBulkCopy_WithNullableFields_ShouldHandleNulls()
    {
        // Arrange
        var categoryId = await EnsureCategoryExistsAsync();
        var books = new List<Book>
        {
            new Book("978-1-234-56789-0", "Book With Nulls", categoryId, 1),
            new Book("978-1-234-56789-1", "Book With Data", categoryId, 2)
        };

        // Add optional data to second book only
        books[1].UpdateDetails("Book With Data", "Subtitle", "Description", "Publisher");
        books[1].UpdatePublishingInfo(new DateTime(2024, 1, 1), 300, "English");

        // Act
        var (insertedCount, _) = await _importer.BulkInsertWithSqlBulkCopyAsync(books);

        // Assert
        Assert.Equal(2, insertedCount);

        // Verify nullable fields are handled correctly
        var retrievedBooks = await GetBooksWithIsbnPrefixAsync("978-1-234-56789");
        Assert.Equal(2, retrievedBooks.Count);

        var bookWithNulls = retrievedBooks.First(b => b.ISBN == "978-1-234-56789-0");
        Assert.Null(bookWithNulls.Subtitle);
        Assert.Null(bookWithNulls.Description);
        Assert.Null(bookWithNulls.Publisher);

        var bookWithData = retrievedBooks.First(b => b.ISBN == "978-1-234-56789-1");
        Assert.Equal("Subtitle", bookWithData.Subtitle);
        Assert.Equal("Description", bookWithData.Description);
        Assert.Equal("Publisher", bookWithData.Publisher);
    }

    [Fact]
    public async Task BulkInsertWithSqlBulkCopy_DifferentBatchSizes_ShouldAllWork()
    {
        // Arrange
        var categoryId = await EnsureCategoryExistsAsync();
        var books = BulkBookImporter.GenerateSampleBooks(250, categoryId);

        var batchSizes = new[] { 10, 50, 100, 250 };
        var results = new List<(int batchSize, long timeMs)>();

        // Act
        foreach (var batchSize in batchSizes)
        {
            await CleanupBooksAsync();

            var (insertedCount, elapsedMs) = await _importer.BulkInsertWithSqlBulkCopyAsync(books, batchSize);

            results.Add((batchSize, elapsedMs));

            Assert.Equal(250, insertedCount);
        }

        // Assert - All batch sizes should complete successfully
        Assert.All(results, r => Assert.True(r.timeMs < 10000, $"Batch size {r.batchSize} took {r.timeMs}ms"));
    }

    [Fact]
    public async Task GenerateSampleBooks_ShouldCreateValidBooks()
    {
        // Arrange & Act
        var books = BulkBookImporter.GenerateSampleBooks(100, categoryId: 1);

        // Assert
        Assert.Equal(100, books.Count);

        // All books should have valid ISBNs
        Assert.All(books, book => Assert.False(string.IsNullOrWhiteSpace(book.ISBN)));

        // All books should have valid titles
        Assert.All(books, book => Assert.False(string.IsNullOrWhiteSpace(book.Title)));

        // All books should have the specified category
        Assert.All(books, book => Assert.Equal(1, book.CategoryId));

        // ISBNs should be unique
        var uniqueIsbns = books.Select(b => b.ISBN).Distinct().Count();
        Assert.Equal(100, uniqueIsbns);
    }

    [Fact]
    public async Task BulkInsertWithSqlBulkCopy_PerformanceComparison_SqlBulkCopyShouldBeMuchFaster()
    {
        // Arrange
        var categoryId = await EnsureCategoryExistsAsync();
        var books = BulkBookImporter.GenerateSampleBooks(100, categoryId);

        // Act - Individual inserts
        await CleanupBooksAsync();
        var (_, individualTime) = await _importer.BulkInsertWithIndividualInsertsAsync(books.Take(100).ToList());

        // Act - Batched inserts
        await CleanupBooksAsync();
        var (_, batchedTime) = await _importer.BulkInsertWithBatchedInsertsAsync(books, batchSize: 20);

        // Act - SqlBulkCopy
        await CleanupBooksAsync();
        var (_, bulkCopyTime) = await _importer.BulkInsertWithSqlBulkCopyAsync(books);

        // Assert - SqlBulkCopy should be significantly faster
        Assert.True(bulkCopyTime < batchedTime,
            $"SqlBulkCopy ({bulkCopyTime}ms) should be faster than batched ({batchedTime}ms)");

        Assert.True(bulkCopyTime < individualTime,
            $"SqlBulkCopy ({bulkCopyTime}ms) should be faster than individual ({individualTime}ms)");

        // SqlBulkCopy should be at least 2x faster than batched (usually 5-10x)
        Assert.True(bulkCopyTime * 2 < batchedTime,
            $"SqlBulkCopy should be at least 2x faster. Bulk: {bulkCopyTime}ms, Batched: {batchedTime}ms");
    }

    #region Helper Methods

    private async Task<int> EnsureCategoryExistsAsync()
    {
        const string checkSql = "SELECT TOP 1 Id FROM Categories ORDER BY Id";
        const string insertSql = @"
            INSERT INTO Categories (Name, Description, IsDeleted, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES ('Bulk Test Category', 'Category for bulk operation tests', 0, GETUTCDATE(), GETUTCDATE())";

        await using var connection = new SqlConnection(_fixture.ConnectionString);
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

    private async Task<int> GetBookCountAsync()
    {
        const string sql = "SELECT COUNT(*) FROM Books WHERE IsDeleted = 0";

        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        return (int)await command.ExecuteScalarAsync()!;
    }

    private async Task<List<Book>> GetBooksWithIsbnPrefixAsync(string isbnPrefix)
    {
        const string sql = @"
            SELECT Id, ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                   PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
                   ShelfLocation, IsDeleted, CreatedAt, UpdatedAt
            FROM Books
            WHERE ISBN LIKE @IsbnPrefix + '%'
            ORDER BY ISBN";

        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IsbnPrefix", isbnPrefix);

        var books = new List<Book>();

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var isbn = reader.GetString(reader.GetOrdinal("ISBN"));
            var title = reader.GetString(reader.GetOrdinal("Title"));
            var categoryId = reader.GetInt32(reader.GetOrdinal("CategoryId"));
            var totalCopies = reader.GetInt32(reader.GetOrdinal("TotalCopies"));

            var book = new Book(isbn, title, categoryId, totalCopies);

            // Set optional fields
            var subtitleOrdinal = reader.GetOrdinal("Subtitle");
            if (!reader.IsDBNull(subtitleOrdinal))
            {
                var subtitle = reader.GetString(subtitleOrdinal);
                var description = reader.IsDBNull(reader.GetOrdinal("Description"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Description"));
                var publisher = reader.IsDBNull(reader.GetOrdinal("Publisher"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Publisher"));

                book.UpdateDetails(title, subtitle, description, publisher);
            }

            books.Add(book);
        }

        return books;
    }

    private async Task CleanupBooksAsync()
    {
        const string sql = "DELETE FROM Books WHERE Id > 0";

        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    #endregion
}
