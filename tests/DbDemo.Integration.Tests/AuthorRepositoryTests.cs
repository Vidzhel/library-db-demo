using DbDemo.ConsoleApp.Infrastructure.Repositories;
using DbDemo.ConsoleApp.Models;
using Xunit;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Integration tests for AuthorRepository
/// Tests actual database operations using the Docker SQL Server instance
/// </summary>
[Collection("Database")]
public class AuthorRepositoryTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly AuthorRepository _repository;

    public AuthorRepositoryTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _repository = new AuthorRepository(_fixture.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        // Cleanup before each test (respect FK constraints)
        await _fixture.CleanupTableAsync("BookAuthors");
        await _fixture.CleanupTableAsync("Authors");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateAsync_ValidAuthor_ShouldInsertAndReturnAuthorWithId()
    {
        // Arrange
        var author = new Author("Robert", "Martin", "bob@cleancode.com");

        // Act
        var createdAuthor = await _repository.CreateAsync(author);

        // Assert
        Assert.NotNull(createdAuthor);
        Assert.True(createdAuthor.Id > 0, "Created author should have an ID assigned");
        Assert.Equal("Robert", createdAuthor.FirstName);
        Assert.Equal("Martin", createdAuthor.LastName);
        Assert.Equal("bob@cleancode.com", createdAuthor.Email);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingAuthor_ShouldReturnAuthor()
    {
        // Arrange
        var author = new Author("Erich", "Gamma");
        var created = await _repository.CreateAsync(author);

        // Act
        var retrieved = await _repository.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("Erich", retrieved.FirstName);
        Assert.Equal("Gamma", retrieved.LastName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentAuthor_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = 99999;

        // Act
        var retrieved = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetByEmailAsync_ExistingAuthor_ShouldReturnAuthor()
    {
        // Arrange
        var author = new Author("Martin", "Fowler", "martin@refactoring.com");
        await _repository.CreateAsync(author);

        // Act
        var retrieved = await _repository.GetByEmailAsync("martin@refactoring.com");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Martin", retrieved.FirstName);
        Assert.Equal("Fowler", retrieved.LastName);
        Assert.Equal("martin@refactoring.com", retrieved.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_NonExistentEmail_ShouldReturnNull()
    {
        // Arrange
        var nonExistentEmail = "nonexistent@test.com";

        // Act
        var retrieved = await _repository.GetByEmailAsync(nonExistentEmail);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResults()
    {
        // Arrange - Create 5 authors
        await _repository.CreateAsync(new Author("Alice", "Anderson"));
        await _repository.CreateAsync(new Author("Bob", "Brown"));
        await _repository.CreateAsync(new Author("Charlie", "Clark"));
        await _repository.CreateAsync(new Author("David", "Davis"));
        await _repository.CreateAsync(new Author("Eve", "Evans"));

        // Act - Get page 1 with 3 items
        var page1 = await _repository.GetPagedAsync(pageNumber: 1, pageSize: 3);

        // Assert
        Assert.NotNull(page1);
        Assert.Equal(3, page1.Count);
        // Should be ordered by LastName, FirstName
        Assert.Equal("Anderson", page1[0].LastName);
        Assert.Equal("Brown", page1[1].LastName);
        Assert.Equal("Clark", page1[2].LastName);
    }

    [Fact]
    public async Task GetPagedAsync_SecondPage_ShouldReturnCorrectResults()
    {
        // Arrange
        await _repository.CreateAsync(new Author("Alice", "Anderson"));
        await _repository.CreateAsync(new Author("Bob", "Brown"));
        await _repository.CreateAsync(new Author("Charlie", "Clark"));
        await _repository.CreateAsync(new Author("David", "Davis"));
        await _repository.CreateAsync(new Author("Eve", "Evans"));

        // Act - Get page 2 with 3 items
        var page2 = await _repository.GetPagedAsync(pageNumber: 2, pageSize: 3);

        // Assert
        Assert.NotNull(page2);
        Assert.Equal(2, page2.Count);
        Assert.Equal("Davis", page2[0].LastName);
        Assert.Equal("Evans", page2[1].LastName);
    }

    [Fact]
    public async Task SearchByNameAsync_ByFirstName_ShouldReturnMatchingAuthors()
    {
        // Arrange
        await _repository.CreateAsync(new Author("Robert", "Martin"));
        await _repository.CreateAsync(new Author("Martin", "Fowler"));
        await _repository.CreateAsync(new Author("Eric", "Evans"));

        // Act
        var results = await _repository.SearchByNameAsync("Martin");

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.Contains(results, a => a.FirstName == "Martin");
        Assert.Contains(results, a => a.LastName == "Martin");
    }

    [Fact]
    public async Task SearchByNameAsync_ByLastName_ShouldReturnMatchingAuthors()
    {
        // Arrange
        await _repository.CreateAsync(new Author("Robert", "Martin"));
        await _repository.CreateAsync(new Author("Martin", "Fowler"));
        await _repository.CreateAsync(new Author("Eric", "Evans"));

        // Act
        var results = await _repository.SearchByNameAsync("Fowler");

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal("Martin", results[0].FirstName);
        Assert.Equal("Fowler", results[0].LastName);
    }

    [Fact]
    public async Task SearchByNameAsync_PartialMatch_ShouldReturnMatchingAuthors()
    {
        // Arrange
        await _repository.CreateAsync(new Author("Robert", "Martin"));
        await _repository.CreateAsync(new Author("Martin", "Fowler"));

        // Act
        var results = await _repository.SearchByNameAsync("Mar");

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetCountAsync_ShouldReturnTotalCount()
    {
        // Arrange
        await _repository.CreateAsync(new Author("Author", "One"));
        await _repository.CreateAsync(new Author("Author", "Two"));
        await _repository.CreateAsync(new Author("Author", "Three"));

        // Act
        var count = await _repository.GetCountAsync();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task UpdateAsync_ExistingAuthor_ShouldUpdateSuccessfully()
    {
        // Arrange
        var author = new Author("Robert", "Martin", "old@email.com");
        var created = await _repository.CreateAsync(author);

        // Get the author to update it
        var toUpdate = await _repository.GetByIdAsync(created.Id);
        Assert.NotNull(toUpdate);

        toUpdate.UpdateDetails("Robert", "C. Martin", "new@email.com");

        // Act
        var updateResult = await _repository.UpdateAsync(toUpdate);

        // Assert
        Assert.True(updateResult);

        // Verify the update
        var updated = await _repository.GetByIdAsync(created.Id);
        Assert.NotNull(updated);
        Assert.Equal("C. Martin", updated.LastName);
        Assert.Equal("new@email.com", updated.Email);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentAuthor_ShouldReturnFalse()
    {
        // Arrange
        var author = new Author("Test", "Author");
        var created = await _repository.CreateAsync(author);

        // Manually set the ID to a non-existent value using reflection
        var idProperty = typeof(Author).GetProperty("Id",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idProperty?.SetValue(created, 99999);

        // Act
        var updateResult = await _repository.UpdateAsync(created);

        // Assert
        Assert.False(updateResult);
    }

    [Fact]
    public async Task DeleteAsync_ExistingAuthor_ShouldDeleteSuccessfully()
    {
        // Arrange
        var author = new Author("ToDelete", "Author");
        var created = await _repository.CreateAsync(author);

        // Act
        var deleteResult = await _repository.DeleteAsync(created.Id);

        // Assert
        Assert.True(deleteResult);

        // Verify deletion
        var deleted = await _repository.GetByIdAsync(created.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentAuthor_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = 99999;

        // Act
        var deleteResult = await _repository.DeleteAsync(nonExistentId);

        // Assert
        Assert.False(deleteResult);
    }
}
