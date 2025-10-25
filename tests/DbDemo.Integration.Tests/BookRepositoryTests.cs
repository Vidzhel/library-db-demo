using DbDemo.ConsoleApp.Infrastructure.Repositories;
using DbDemo.ConsoleApp.Models;
using Xunit;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Integration tests for BookRepository
/// Tests actual database operations using the Docker SQL Server instance
/// </summary>
[Collection("Database")]
public class BookRepositoryTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly BookRepository _repository;

    public BookRepositoryTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _repository = new BookRepository(_fixture.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        // Cleanup before each test
        await _fixture.CleanupTableAsync("BookAuthors");
        await _fixture.CleanupTableAsync("Loans");
        await _fixture.CleanupTableAsync("Books");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateAsync_ValidBook_ShouldInsertAndReturnBookWithId()
    {
        // Arrange
        var book = new Book(
            isbn: "978-0132350884",
            title: "Clean Code",
            categoryId: 1,  // Assuming category with Id=1 exists from seed data
            totalCopies: 3
        );

        // Act
        var createdBook = await _repository.CreateAsync(book);

        // Assert
        Assert.NotNull(createdBook);
        Assert.True(createdBook.Id > 0, "Created book should have an ID assigned");
        Assert.Equal("978-0132350884", createdBook.ISBN);
        Assert.Equal("Clean Code", createdBook.Title);
        Assert.Equal(3, createdBook.TotalCopies);
        Assert.Equal(3, createdBook.AvailableCopies);
        Assert.False(createdBook.IsDeleted);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingBook_ShouldReturnBook()
    {
        // Arrange
        var book = new Book("978-0201633610", "Design Patterns", 1, 2);
        var created = await _repository.CreateAsync(book);

        // Act
        var retrieved = await _repository.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("978-0201633610", retrieved.ISBN);
        Assert.Equal("Design Patterns", retrieved.Title);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentBook_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = 99999;

        // Act
        var retrieved = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetByIsbnAsync_ExistingBook_ShouldReturnBook()
    {
        // Arrange
        var book = new Book("978-0136291558", "Object-Oriented Software Engineering", 1, 1);
        await _repository.CreateAsync(book);

        // Act
        var retrieved = await _repository.GetByIsbnAsync("978-0136291558");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("978-0136291558", retrieved.ISBN);
        Assert.Equal("Object-Oriented Software Engineering", retrieved.Title);
    }

    [Fact]
    public async Task GetByIsbnAsync_NonExistentIsbn_ShouldReturnNull()
    {
        // Act
        var retrieved = await _repository.GetByIsbnAsync("978-9999999999");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetPagedAsync_WithData_ShouldReturnCorrectPage()
    {
        // Arrange - Create 5 books
        for (int i = 1; i <= 5; i++)
        {
            var book = new Book($"978-{i:D10}", $"Book {i}", 1, 1);
            await _repository.CreateAsync(book);
        }

        // Act - Get first page (2 items per page)
        var page1 = await _repository.GetPagedAsync(pageNumber: 1, pageSize: 2);
        var page2 = await _repository.GetPagedAsync(pageNumber: 2, pageSize: 2);
        var page3 = await _repository.GetPagedAsync(pageNumber: 3, pageSize: 2);

        // Assert
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.Single(page3);  // Only 1 item on page 3
    }

    [Fact]
    public async Task GetPagedAsync_ExcludesDeletedBooks_ByDefault()
    {
        // Arrange
        var book1 = new Book("978-1111111111", "Active Book", 1, 1);
        var book2 = new Book("978-2222222222", "Deleted Book", 1, 1);

        var created1 = await _repository.CreateAsync(book1);
        var created2 = await _repository.CreateAsync(book2);

        // Delete book2
        await _repository.DeleteAsync(created2.Id);

        // Act
        var books = await _repository.GetPagedAsync(pageNumber: 1, pageSize: 10, includeDeleted: false);

        // Assert
        Assert.Single(books);
        Assert.Equal(created1.Id, books[0].Id);
    }

    [Fact]
    public async Task SearchByTitleAsync_PartialMatch_ShouldReturnMatchingBooks()
    {
        // Arrange
        await _repository.CreateAsync(new Book("978-1111111111", "Clean Code", 1, 1));
        await _repository.CreateAsync(new Book("978-2222222222", "Clean Architecture", 1, 1));
        await _repository.CreateAsync(new Book("978-3333333333", "Dirty Code", 1, 1));

        // Act
        var results = await _repository.SearchByTitleAsync("Clean");

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, book => Assert.Contains("Clean", book.Title));
    }

    [Fact]
    public async Task GetByCategoryAsync_ShouldReturnBooksInCategory()
    {
        // Arrange
        var cat1Book1 = new Book("978-1111111111", "Category 1 Book A", 1, 1);
        var cat1Book2 = new Book("978-2222222222", "Category 1 Book B", 1, 1);
        var cat2Book = new Book("978-3333333333", "Category 2 Book", 2, 1);  // Different category

        await _repository.CreateAsync(cat1Book1);
        await _repository.CreateAsync(cat1Book2);
        await _repository.CreateAsync(cat2Book);

        // Act
        var category1Books = await _repository.GetByCategoryAsync(1);
        var category2Books = await _repository.GetByCategoryAsync(2);

        // Assert
        Assert.Equal(2, category1Books.Count);
        Assert.Single(category2Books);
        Assert.All(category1Books, book => Assert.Equal(1, book.CategoryId));
    }

    [Fact]
    public async Task GetCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        await _repository.CreateAsync(new Book("978-1111111111", "Book 1", 1, 1));
        await _repository.CreateAsync(new Book("978-2222222222", "Book 2", 1, 1));
        var deletedBook = await _repository.CreateAsync(new Book("978-3333333333", "Book 3", 1, 1));
        await _repository.DeleteAsync(deletedBook.Id);

        // Act
        var totalCount = await _repository.GetCountAsync(includeDeleted: true);
        var activeCount = await _repository.GetCountAsync(includeDeleted: false);

        // Assert
        Assert.Equal(3, totalCount);
        Assert.Equal(2, activeCount);
    }

    [Fact]
    public async Task UpdateAsync_ValidBook_ShouldUpdateSuccessfully()
    {
        // Arrange
        var book = new Book("978-0132350884", "Original Title", 1, 3);
        var created = await _repository.CreateAsync(book);

        // Modify the book
        created.UpdateDetails("Updated Title", "New Subtitle", "New Description", "New Publisher");

        // Act
        var updateResult = await _repository.UpdateAsync(created);
        var updated = await _repository.GetByIdAsync(created.Id);

        // Assert
        Assert.True(updateResult);
        Assert.NotNull(updated);
        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal("New Subtitle", updated.Subtitle);
        Assert.Equal("New Description", updated.Description);
        Assert.Equal("New Publisher", updated.Publisher);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentBook_ShouldReturnFalse()
    {
        // Arrange
        var book = new Book("978-9999999999", "Non-existent", 1, 1);
        // Use reflection to set a non-existent ID
        var idProperty = typeof(Book).GetProperty("Id");
        idProperty?.SetValue(book, 99999);

        // Act
        var result = await _repository.UpdateAsync(book);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingBook_ShouldMarkAsDeleted()
    {
        // Arrange
        var book = new Book("978-0132350884", "To Be Deleted", 1, 1);
        var created = await _repository.CreateAsync(book);

        // Act
        var deleteResult = await _repository.DeleteAsync(created.Id);
        var retrieved = await _repository.GetByIdAsync(created.Id);

        // Assert
        Assert.True(deleteResult);
        Assert.NotNull(retrieved);
        Assert.True(retrieved.IsDeleted, "Book should be marked as deleted");
    }

    [Fact]
    public async Task DeleteAsync_NonExistentBook_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.DeleteAsync(99999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CreateAsync_WithOptionalFields_ShouldStoreAllData()
    {
        // Arrange
        var book = new Book("978-0132350884", "Book with Details", 1, 5);
        book.UpdateDetails("Book with Details", "A Subtitle", "A Description", "Tech Publisher");
        book.UpdatePublishingInfo(new DateTime(2008, 8, 1), 464, "English");
        book.UpdateShelfLocation("A-15");

        // Act
        var created = await _repository.CreateAsync(book);
        var retrieved = await _repository.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("A Subtitle", retrieved.Subtitle);
        Assert.Equal("A Description", retrieved.Description);
        Assert.Equal("Tech Publisher", retrieved.Publisher);
        Assert.Equal(new DateTime(2008, 8, 1), retrieved.PublishedDate);
        Assert.Equal(464, retrieved.PageCount);
        Assert.Equal("English", retrieved.Language);
        Assert.Equal("A-15", retrieved.ShelfLocation);
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 3)]
    [InlineData(3, 10)]
    public async Task GetPagedAsync_DifferentPageSizes_ShouldReturnCorrectResults(int pageNumber, int pageSize)
    {
        // Arrange - Create 15 books
        for (int i = 1; i <= 15; i++)
        {
            await _repository.CreateAsync(new Book($"978-{i:D10}", $"Book {i}", 1, 1));
        }

        // Act
        var results = await _repository.GetPagedAsync(pageNumber, pageSize);

        // Assert
        var expectedCount = Math.Min(pageSize, Math.Max(0, 15 - (pageNumber - 1) * pageSize));
        Assert.Equal(expectedCount, results.Count);
    }
}
