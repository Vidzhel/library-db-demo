using DbDemo.Infrastructure.BulkOperations;
using DbDemo.Domain.Entities;
using Microsoft.Data.SqlClient;
using DbDemo.Application.DTOs;
using Xunit;

namespace DbDemo.Integration.Tests.BulkOperations;

[Collection("Database")]
public class TvpBookImporterTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly TvpBookImporter _importer;

    public TvpBookImporterTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _importer = new TvpBookImporter(_fixture.ConnectionString);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await CleanupBooksAsync();
    }

    [Fact]
    public async Task IsTvpInfrastructureAvailable_ShouldReturnTrue_AfterMigration()
    {
        // Act
        var isAvailable = await _importer.IsTvpInfrastructureAvailableAsync();

        // Assert
        Assert.True(isAvailable, "TVP infrastructure should be available after migration V004");
    }

    [Fact]
    public async Task BulkInsertWithTvp_ShouldInsertAllRecords()
    {
        // Arrange
        var categoryId = await EnsureCategoryExistsAsync();
        var books = BulkBookImporter.GenerateSampleBooks(100, categoryId);

        // Act
        var (insertedCount, elapsedMs) = await _importer.BulkInsertWithTvpAsync(books);

        // Assert
        Assert.Equal(100, insertedCount);
        Assert.True(elapsedMs < 10000, $"TVP insert took too long: {elapsedMs}ms");

        // Verify count in database
        var countInDb = await GetBookCountAsync();
        Assert.True(countInDb >= 100, $"Expected at least 100 books, found {countInDb}");
    }

    [Fact]
    public async Task BulkInsertWithTvp_WithDuplicateISBN_ShouldThrowException()
    {
        // Arrange
        var categoryId = await EnsureCategoryExistsAsync();
        var books = new List<Book>
        {
            new Book("978-3-16-148410-0", "Book 1", categoryId, 1),
            new Book("978-3-16-148410-0", "Book 2", categoryId, 1) // Duplicate ISBN
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SqlException>(
            async () => await _importer.BulkInsertWithTvpAsync(books)
        );

        Assert.Contains("Duplicate", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BulkInsertWithTvp_WithExistingISBN_ShouldThrowException()
    {
        // Arrange
        var categoryId = await EnsureCategoryExistsAsync();

        // Insert first book
        var existingBook = new Book("978-0-306-40615-7", "Existing Book", categoryId, 1);
        await _importer.BulkInsertWithTvpAsync(new[] { existingBook });

        // Try to insert another book with same ISBN
        var duplicateBook = new Book("978-0-306-40615-7", "Another Book", categoryId, 1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SqlException>(
            async () => await _importer.BulkInsertWithTvpAsync(new[] { duplicateBook })
        );

        Assert.Contains("already exist", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BulkInsertWithTvp_WithInvalidCategoryId_ShouldThrowException()
    {
        // Arrange
        var invalidCategoryId = 99999; // Non-existent category
        var books = new List<Book>
        {
            new Book("978-1-234-56789-7", "Book 1", invalidCategoryId, 1)
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SqlException>(
            async () => await _importer.BulkInsertWithTvpAsync(books)
        );

        Assert.Contains("CategoryId", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BulkInsertWithTvp_WithNullableFields_ShouldHandleCorrectly()
    {
        // Arrange
        var categoryId = await EnsureCategoryExistsAsync();
        var books = new List<Book>
        {
            new Book("978-0-123-45678-9", "Book With Nulls", categoryId, 1),
            new Book("978-0-123-45679-6", "Book With Data", categoryId, 2)
        };

        // Add details to second book only
        books[1].UpdateDetails("Book With Data", "Subtitle", "Description", "Publisher");
        books[1].UpdatePublishingInfo(new DateTime(2024, 1, 1), 300, "English");
        books[1].UpdateShelfLocation("A-1-1");

        // Act
        var (insertedCount, _) = await _importer.BulkInsertWithTvpAsync(books);

        // Assert
        Assert.Equal(2, insertedCount);

        // Retrieve and verify
        var retrievedBooks = await _importer.GetBooksByISBNsAsync(new[] { "978-0-123-45678-9", "978-0-123-45679-6" });
        Assert.Equal(2, retrievedBooks.Count);

        var bookWithNulls = retrievedBooks.First(b => b.ISBN == "978-0-123-45678-9");
        Assert.Null(bookWithNulls.Subtitle);
        Assert.Null(bookWithNulls.Publisher);

        var bookWithData = retrievedBooks.First(b => b.ISBN == "978-0-123-45679-6");
        Assert.Equal("Subtitle", bookWithData.Subtitle);
        Assert.Equal("Publisher", bookWithData.Publisher);
    }

    [Fact]
    public async Task BulkInsertWithTvpAndTransaction_ShouldCommitSuccessfully()
    {
        // Arrange
        var categoryId = await EnsureCategoryExistsAsync();
        var books = BulkBookImporter.GenerateSampleBooks(50, categoryId);

        // Act
        var (insertedCount, elapsedMs) = await _importer.BulkInsertWithTvpAndTransactionAsync(books);

        // Assert
        Assert.Equal(50, insertedCount);

        var countInDb = await GetBookCountAsync();
        Assert.True(countInDb >= 50);
    }

    [Fact]
    public async Task BulkInsertWithTvpAndTransaction_OnError_ShouldRollback()
    {
        // Arrange
        var categoryId = await EnsureCategoryExistsAsync();
        var booksWithDuplicate = new List<Book>
        {
            new Book("978-1-56619-909-4", "Book 1", categoryId, 1),
            new Book("978-1-56619-909-4", "Book 2", categoryId, 1) // Duplicate - should cause rollback
        };

        var initialCount = await GetBookCountAsync();

        // Act & Assert
        await Assert.ThrowsAsync<SqlException>(
            async () => await _importer.BulkInsertWithTvpAndTransactionAsync(booksWithDuplicate)
        );

        // Verify rollback - count should be unchanged
        var finalCount = await GetBookCountAsync();
        Assert.Equal(initialCount, finalCount);
    }

    [Fact]
    public async Task GetBooksByISBNs_ShouldRetrieveCorrectBooks()
    {
        // Arrange
        var categoryId = await EnsureCategoryExistsAsync();
        var books = new List<Book>
        {
            new Book("978-0-201-61622-4", "Book 1", categoryId, 1),
            new Book("978-0-201-61623-1", "Book 2", categoryId, 2),
            new Book("978-0-201-61624-8", "Book 3", categoryId, 3)
        };

        await _importer.BulkInsertWithTvpAsync(books);

        // Act
        var retrievedBooks = await _importer.GetBooksByISBNsAsync(new[] { "978-0-201-61622-4", "978-0-201-61624-8" });

        // Assert
        Assert.Equal(2, retrievedBooks.Count);
        Assert.Contains(retrievedBooks, b => b.ISBN == "978-0-201-61622-4");
        Assert.Contains(retrievedBooks, b => b.ISBN == "978-0-201-61624-8");
        Assert.DoesNotContain(retrievedBooks, b => b.ISBN == "978-0-201-61623-1");
    }

    [Fact]
    public async Task BulkInsertWithTvp_PerformanceTest_ShouldBeReasonablyFast()
    {
        // Arrange
        var categoryId = await EnsureCategoryExistsAsync();
        var books = BulkBookImporter.GenerateSampleBooks(500, categoryId);

        // Act
        var (insertedCount, elapsedMs) = await _importer.BulkInsertWithTvpAsync(books);

        // Assert
        Assert.Equal(500, insertedCount);

        // TVP should be faster than 2ms per record on average
        var msPerRecord = elapsedMs / (double)insertedCount;
        Assert.True(msPerRecord < 2.0,
            $"TVP performance too slow: {msPerRecord:F3}ms per record (expected < 2ms)");
    }

    [Fact]
    public async Task BulkInsertWithTvp_EmptyList_ShouldThrowError()
    {
        // Arrange
        var emptyBooks = new List<Book>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SqlException>(
            async () => await _importer.BulkInsertWithTvpAsync(emptyBooks)
        );

        Assert.Contains("No books", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    #region Helper Methods

    private async Task<int> EnsureCategoryExistsAsync()
    {
        const string checkSql = "SELECT TOP 1 Id FROM Categories ORDER BY Id";
        const string insertSql = @"
            INSERT INTO Categories (Name, Description, IsDeleted, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES ('TVP Test Category', 'Category for TVP tests', 0, GETUTCDATE(), GETUTCDATE())";

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

    private async Task CleanupBooksAsync()
    {
        // Delete in correct order to respect FK constraints
        const string deleteLoans = "DELETE FROM Loans WHERE BookId > 0";
        const string deleteBooks = "DELETE FROM Books WHERE Id > 0";

        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        // Delete Loans first (child records)
        await using var loansCommand = new SqlCommand(deleteLoans, connection);
        await loansCommand.ExecuteNonQueryAsync();

        // Then delete Books (parent records)
        await using var booksCommand = new SqlCommand(deleteBooks, connection);
        await booksCommand.ExecuteNonQueryAsync();
    }

    #endregion
}
