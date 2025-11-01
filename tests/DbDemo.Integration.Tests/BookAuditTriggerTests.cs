using DbDemo.ConsoleApp.Infrastructure.Repositories;
using DbDemo.ConsoleApp.Models;
using Xunit;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Integration tests for Book Audit Trigger (TR_Books_Audit)
/// Tests that database trigger creates audit records for INSERT, UPDATE, DELETE operations
/// </summary>
[Collection("Database")]
public class BookAuditTriggerTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly BookRepository _bookRepository;
    private readonly BookAuditRepository _auditRepository;
    private readonly CategoryRepository _categoryRepository;
    private int _testCategoryId;

    public BookAuditTriggerTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _bookRepository = new BookRepository();
        _auditRepository = new BookAuditRepository();
        _categoryRepository = new CategoryRepository();
    }

    public async Task InitializeAsync()
    {
        // Cleanup before each test (in correct order)
        // Note: Deleting Books triggers DELETE audit records, so BooksAudit must be cleaned AFTER Books
        await _fixture.CleanupTableAsync("Loans");
        await _fixture.CleanupTableAsync("BookAuthors");
        await _fixture.CleanupTableAsync("Books");         // This creates DELETE audit records
        await _fixture.CleanupTableAsync("BooksAudit");   // Clean up audit records created above
        await _fixture.CleanupTableAsync("Categories");

        // Create a test category for all book tests
        var testCategory = await _fixture.WithTransactionAsync(tx =>
            _categoryRepository.CreateAsync(new Category("Test Category"), tx));
        _testCategoryId = testCategory.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task InsertBook_ShouldCreateAuditRecordWithInsertAction()
    {
        // Arrange
        var book = new Book("978-0-135-95705-9", "Test Book", _testCategoryId, 5);

        // Act
        var createdBook = await _fixture.WithTransactionAsync(async tx =>
        {
            var result = await _bookRepository.CreateAsync(book, tx);
            return result;
        });

        // Assert - Verify audit record was created
        var auditHistory = await _fixture.WithTransactionAsync(tx =>
            _auditRepository.GetAuditHistoryAsync(createdBook.Id, tx));

        Assert.NotNull(auditHistory);
        Assert.Single(auditHistory);

        var auditRecord = auditHistory[0];
        Assert.Equal("INSERT", auditRecord.Action);
        Assert.Equal(createdBook.Id, auditRecord.BookId);
        Assert.Equal("978-0-135-95705-9", auditRecord.NewISBN);
        Assert.Equal("Test Book", auditRecord.NewTitle);
        Assert.Equal(5, auditRecord.NewAvailableCopies);
        Assert.Equal(5, auditRecord.NewTotalCopies);
        Assert.Null(auditRecord.OldISBN);
        Assert.Null(auditRecord.OldTitle);
        Assert.Null(auditRecord.OldAvailableCopies);
    }

    [Fact]
    public async Task UpdateBook_ShouldCreateAuditRecordWithUpdateAction()
    {
        // Arrange
        var book = new Book("978-0-201-63361-0", "Original Title", _testCategoryId, 3);
        var createdBook = await _fixture.WithTransactionAsync(tx =>
            _bookRepository.CreateAsync(book, tx));

        // Act - Update the book
        await _fixture.WithTransactionAsync(async tx =>
        {
            createdBook.UpdateDetails(
                title: createdBook.Title,
                subtitle: "Updated Subtitle",
                description: "Updated description",
                publisher: "Test Publisher"
            );
            createdBook.UpdatePublishingInfo(
                publishedDate: new DateTime(2024, 1, 1),
                pageCount: 200,
                language: "English"
            );
            createdBook.AddCopies(2); // Increase total copies from 3 to 5
            await _bookRepository.UpdateAsync(createdBook, tx);
        });

        // Assert - Verify audit records (INSERT + UPDATE)
        var auditHistory = await _fixture.WithTransactionAsync(tx =>
            _auditRepository.GetAuditHistoryAsync(createdBook.Id, tx));

        Assert.NotNull(auditHistory);
        Assert.Equal(2, auditHistory.Count);

        // Most recent should be UPDATE (ordered by ChangedAt DESC)
        var updateRecord = auditHistory[0];
        Assert.Equal("UPDATE", updateRecord.Action);
        Assert.Equal(3, updateRecord.OldTotalCopies);
        Assert.Equal(5, updateRecord.NewTotalCopies);

        // Oldest should be INSERT
        var insertRecord = auditHistory[1];
        Assert.Equal("INSERT", insertRecord.Action);
    }

    [Fact]
    public async Task BorrowAndReturnBook_ShouldCreateMultipleUpdateAuditRecords()
    {
        // Arrange
        var book = new Book("978-0-13-468599-1", "Borrow Test Book", _testCategoryId, 5);
        var createdBook = await _fixture.WithTransactionAsync(tx =>
            _bookRepository.CreateAsync(book, tx));

        // Act - Borrow a copy
        await _fixture.WithTransactionAsync(async tx =>
        {
            createdBook.BorrowCopy();
            await _bookRepository.UpdateAsync(createdBook, tx);
        });

        // Act - Return the copy
        await _fixture.WithTransactionAsync(async tx =>
        {
            createdBook.ReturnCopy();
            await _bookRepository.UpdateAsync(createdBook, tx);
        });

        // Assert - Verify audit records (INSERT + 2 UPDATEs)
        var auditHistory = await _fixture.WithTransactionAsync(tx =>
            _auditRepository.GetAuditHistoryAsync(createdBook.Id, tx));

        Assert.NotNull(auditHistory);
        Assert.Equal(3, auditHistory.Count);

        // Most recent: Return (5 available copies)
        var returnRecord = auditHistory[0];
        Assert.Equal("UPDATE", returnRecord.Action);
        Assert.Equal(4, returnRecord.OldAvailableCopies);
        Assert.Equal(5, returnRecord.NewAvailableCopies);

        // Middle: Borrow (4 available copies)
        var borrowRecord = auditHistory[1];
        Assert.Equal("UPDATE", borrowRecord.Action);
        Assert.Equal(5, borrowRecord.OldAvailableCopies);
        Assert.Equal(4, borrowRecord.NewAvailableCopies);

        // Oldest: INSERT
        var insertRecord = auditHistory[2];
        Assert.Equal("INSERT", insertRecord.Action);
    }

    [Fact]
    public async Task DeleteBook_ShouldCreateUpdateAuditRecord()
    {
        // Arrange
        var book = new Book("978-0-321-12742-6", "Delete Test Book", _testCategoryId, 2);
        var createdBook = await _fixture.WithTransactionAsync(tx =>
            _bookRepository.CreateAsync(book, tx));

        // Act - Soft delete the book
        await _fixture.WithTransactionAsync(tx =>
            _bookRepository.DeleteAsync(createdBook.Id, tx));

        // Assert - Verify audit records (INSERT + UPDATE for soft delete)
        var auditHistory = await _fixture.WithTransactionAsync(tx =>
            _auditRepository.GetAuditHistoryAsync(createdBook.Id, tx));

        Assert.NotNull(auditHistory);
        Assert.Equal(2, auditHistory.Count); // INSERT + UPDATE (soft delete)

        // Most recent should be UPDATE (soft delete sets IsDeleted = true)
        var deleteRecord = auditHistory[0];
        Assert.Equal("UPDATE", deleteRecord.Action);

        // Oldest should be INSERT
        var insertRecord = auditHistory[1];
        Assert.Equal("INSERT", insertRecord.Action);
    }

    [Fact]
    public async Task GetAllAuditRecordsAsync_ShouldReturnFilteredByAction()
    {
        // Arrange - Create and update multiple books
        var book1 = new Book("978-1-234-56789-0", "Book One", _testCategoryId, 3);
        var book2 = new Book("978-1-234-56789-1", "Book Two", _testCategoryId, 2);

        var created1 = await _fixture.WithTransactionAsync(tx =>
            _bookRepository.CreateAsync(book1, tx));
        var created2 = await _fixture.WithTransactionAsync(tx =>
            _bookRepository.CreateAsync(book2, tx));

        // Update book1
        await _fixture.WithTransactionAsync(async tx =>
        {
            created1.AddCopies(1);
            await _bookRepository.UpdateAsync(created1, tx);
        });

        // Act - Get all INSERT audit records
        var insertRecords = await _fixture.WithTransactionAsync(tx =>
            _auditRepository.GetAllAuditRecordsAsync("INSERT", limit: 100, transaction: tx));

        // Assert
        Assert.NotNull(insertRecords);
        Assert.True(insertRecords.Count >= 2, "Should have at least 2 INSERT records");
        Assert.All(insertRecords, record => Assert.Equal("INSERT", record.Action));
    }

    [Fact]
    public async Task GetAllAuditRecordsAsync_WithLimit_ShouldRespectLimit()
    {
        // Arrange - Create several books
        for (int i = 0; i < 5; i++)
        {
            var book = new Book($"978-1-234-{i:D5}-0", $"Book {i}", _testCategoryId, 1);
            await _fixture.WithTransactionAsync(tx => _bookRepository.CreateAsync(book, tx));
        }

        // Act - Get only 3 records
        var auditRecords = await _fixture.WithTransactionAsync(tx =>
            _auditRepository.GetAllAuditRecordsAsync(action: null, limit: 3, transaction: tx));

        // Assert
        Assert.NotNull(auditRecords);
        Assert.Equal(3, auditRecords.Count);
    }

    [Fact]
    public async Task AuditRecord_ShouldIncludeChangedByInformation()
    {
        // Arrange
        var book = new Book("978-0-596-00977-0", "Metadata Test Book", _testCategoryId, 1);

        // Act
        var createdBook = await _fixture.WithTransactionAsync(tx =>
            _bookRepository.CreateAsync(book, tx));

        // Assert
        var auditHistory = await _fixture.WithTransactionAsync(tx =>
            _auditRepository.GetAuditHistoryAsync(createdBook.Id, tx));

        Assert.Single(auditHistory);
        var auditRecord = auditHistory[0];

        // Verify metadata fields
        Assert.NotEqual(default(DateTime), auditRecord.ChangedAt);
        Assert.NotNull(auditRecord.ChangedBy);
        Assert.NotEmpty(auditRecord.ChangedBy);
    }

    [Fact]
    public async Task MultipleUpdates_ShouldMaintainAuditTrailOrder()
    {
        // Arrange
        var book = new Book("978-0-07-106681-6", "Multi-Update Test", _testCategoryId, 10);
        var createdBook = await _fixture.WithTransactionAsync(tx =>
            _bookRepository.CreateAsync(book, tx));

        // Act - Perform 3 updates
        await _fixture.WithTransactionAsync(async tx =>
        {
            createdBook.BorrowCopy(); // 9 available
            await _bookRepository.UpdateAsync(createdBook, tx);
        });

        await _fixture.WithTransactionAsync(async tx =>
        {
            createdBook.BorrowCopy(); // 8 available
            await _bookRepository.UpdateAsync(createdBook, tx);
        });

        await _fixture.WithTransactionAsync(async tx =>
        {
            createdBook.ReturnCopy(); // 9 available
            await _bookRepository.UpdateAsync(createdBook, tx);
        });

        // Assert
        var auditHistory = await _fixture.WithTransactionAsync(tx =>
            _auditRepository.GetAuditHistoryAsync(createdBook.Id, tx));

        Assert.Equal(4, auditHistory.Count); // 1 INSERT + 3 UPDATEs

        // Verify order (most recent first)
        Assert.Equal("UPDATE", auditHistory[0].Action);
        Assert.Equal(8, auditHistory[0].OldAvailableCopies); // Return: 8 → 9
        Assert.Equal(9, auditHistory[0].NewAvailableCopies);

        Assert.Equal("UPDATE", auditHistory[1].Action);
        Assert.Equal(9, auditHistory[1].OldAvailableCopies); // Borrow: 9 → 8
        Assert.Equal(8, auditHistory[1].NewAvailableCopies);

        Assert.Equal("UPDATE", auditHistory[2].Action);
        Assert.Equal(10, auditHistory[2].OldAvailableCopies); // Borrow: 10 → 9
        Assert.Equal(9, auditHistory[2].NewAvailableCopies);

        Assert.Equal("INSERT", auditHistory[3].Action);
    }
}
