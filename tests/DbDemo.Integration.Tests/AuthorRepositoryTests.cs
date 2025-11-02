using DbDemo.Application.Repositories;
using DbDemo.Infrastructure.Repositories;
using DbDemo.Domain.Entities;
using DbDemo.Application.DTOs;
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
        _repository = new AuthorRepository();
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
        var createdAuthor = await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(author, tx));

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
        var created = await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(author, tx));

        // Act
        var retrieved = await _fixture.WithTransactionAsync(tx => _repository.GetByIdAsync(created.Id, tx));

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
        var retrieved = await _fixture.WithTransactionAsync(tx => _repository.GetByIdAsync(nonExistentId, tx));

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetByEmailAsync_ExistingAuthor_ShouldReturnAuthor()
    {
        // Arrange
        var author = new Author("Martin", "Fowler", "martin@refactoring.com");
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(author, tx));

        // Act
        var retrieved = await _fixture.WithTransactionAsync(tx => _repository.GetByEmailAsync("martin@refactoring.com", tx));

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
        var retrieved = await _fixture.WithTransactionAsync(tx => _repository.GetByEmailAsync(nonExistentEmail, tx));

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResults()
    {
        // Arrange - Create 5 authors
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Alice", "Anderson"), tx));
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Bob", "Brown"), tx));
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Charlie", "Clark"), tx));
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("David", "Davis"), tx));
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Eve", "Evans"), tx));

        // Act - Get page 1 with 3 items
        var page1 = await _fixture.WithTransactionAsync(tx => _repository.GetPagedAsync(pageNumber: 1, pageSize: 3, tx));

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
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Alice", "Anderson"), tx));
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Bob", "Brown"), tx));
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Charlie", "Clark"), tx));
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("David", "Davis"), tx));
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Eve", "Evans"), tx));

        // Act - Get page 2 with 3 items
        var page2 = await _fixture.WithTransactionAsync(tx => _repository.GetPagedAsync(pageNumber: 2, pageSize: 3, tx));

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
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Robert", "Martin"), tx));
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Martin", "Fowler"), tx));
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Eric", "Evans"), tx));

        // Act
        var results = await _fixture.WithTransactionAsync(tx => _repository.SearchByNameAsync("Martin", tx));

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
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Robert", "Martin"), tx));
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Martin", "Fowler"), tx));
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Eric", "Evans"), tx));

        // Act
        var results = await _fixture.WithTransactionAsync(tx => _repository.SearchByNameAsync("Fowler", tx));

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
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Robert", "Martin"), tx));
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Martin", "Fowler"), tx));

        // Act
        var results = await _fixture.WithTransactionAsync(tx => _repository.SearchByNameAsync("Mar", tx));

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetCountAsync_ShouldReturnTotalCount()
    {
        // Arrange
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Author", "One"), tx));
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Author", "Two"), tx));
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(new Author("Author", "Three"), tx));

        // Act
        var count = await _fixture.WithTransactionAsync(tx => _repository.GetCountAsync(tx));

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task UpdateAsync_ExistingAuthor_ShouldUpdateSuccessfully()
    {
        // Arrange
        var author = new Author("Robert", "Martin", "old@email.com");
        var created = await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(author, tx));

        // Get the author to update it
        var toUpdate = await _fixture.WithTransactionAsync(tx => _repository.GetByIdAsync(created.Id, tx));
        Assert.NotNull(toUpdate);

        toUpdate.UpdateDetails("Robert", "C. Martin", "new@email.com");

        // Act
        var updateResult = await _fixture.WithTransactionAsync(tx => _repository.UpdateAsync(toUpdate, tx));

        // Assert
        Assert.True(updateResult);

        // Verify the update
        var updated = await _fixture.WithTransactionAsync(tx => _repository.GetByIdAsync(created.Id, tx));
        Assert.NotNull(updated);
        Assert.Equal("C. Martin", updated.LastName);
        Assert.Equal("new@email.com", updated.Email);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentAuthor_ShouldReturnFalse()
    {
        // Arrange
        var author = new Author("Test", "Author");
        var created = await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(author, tx));

        // Manually set the ID to a non-existent value using reflection
        var idProperty = typeof(Author).GetProperty("Id",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idProperty?.SetValue(created, 99999);

        // Act
        var updateResult = await _fixture.WithTransactionAsync(tx => _repository.UpdateAsync(created, tx));

        // Assert
        Assert.False(updateResult);
    }

    [Fact]
    public async Task DeleteAsync_ExistingAuthor_ShouldDeleteSuccessfully()
    {
        // Arrange
        var author = new Author("ToDelete", "Author");
        var created = await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(author, tx));

        // Act
        var deleteResult = await _fixture.WithTransactionAsync(tx => _repository.DeleteAsync(created.Id, tx));

        // Assert
        Assert.True(deleteResult);

        // Verify deletion
        var deleted = await _fixture.WithTransactionAsync(tx => _repository.GetByIdAsync(created.Id, tx));
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentAuthor_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = 99999;

        // Act
        var deleteResult = await _fixture.WithTransactionAsync(tx => _repository.DeleteAsync(nonExistentId, tx));

        // Assert
        Assert.False(deleteResult);
    }
}
