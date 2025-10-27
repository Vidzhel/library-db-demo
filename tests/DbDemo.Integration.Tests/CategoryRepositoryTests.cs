using DbDemo.ConsoleApp.Infrastructure.Repositories;
using DbDemo.ConsoleApp.Models;
using Xunit;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Integration tests for CategoryRepository
/// Tests actual database operations using the Docker SQL Server instance
/// Includes tests for hierarchical category structure
/// </summary>
[Collection("Database")]
public class CategoryRepositoryTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly CategoryRepository _repository;

    public CategoryRepositoryTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _repository = new CategoryRepository(_fixture.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        // Cleanup before each test (respect FK constraints)
        await _fixture.CleanupTableAsync("BookAuthors");
        await _fixture.CleanupTableAsync("Loans");
        await _fixture.CleanupTableAsync("Books");
        await _fixture.CleanupTableAsync("Categories");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateAsync_ValidCategory_ShouldInsertAndReturnCategoryWithId()
    {
        // Arrange
        var category = new Category("Fiction", "Fictional books");

        // Act
        var createdCategory = await _repository.CreateAsync(category);

        // Assert
        Assert.NotNull(createdCategory);
        Assert.True(createdCategory.Id > 0, "Created category should have an ID assigned");
        Assert.Equal("Fiction", createdCategory.Name);
        Assert.Equal("Fictional books", createdCategory.Description);
        Assert.Null(createdCategory.ParentCategoryId);
    }

    [Fact]
    public async Task CreateAsync_CategoryWithParent_ShouldCreateSuccessfully()
    {
        // Arrange
        var parent = new Category("Fiction");
        var createdParent = await _repository.CreateAsync(parent);

        var child = new Category("Science Fiction", "Sci-fi books", createdParent.Id);

        // Act
        var createdChild = await _repository.CreateAsync(child);

        // Assert
        Assert.NotNull(createdChild);
        Assert.True(createdChild.Id > 0);
        Assert.Equal("Science Fiction", createdChild.Name);
        Assert.Equal(createdParent.Id, createdChild.ParentCategoryId);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingCategory_ShouldReturnCategory()
    {
        // Arrange
        var category = new Category("Non-Fiction");
        var created = await _repository.CreateAsync(category);

        // Act
        var retrieved = await _repository.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("Non-Fiction", retrieved.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentCategory_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = 99999;

        // Act
        var retrieved = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCategories()
    {
        // Arrange
        await _repository.CreateAsync(new Category("Fiction"));
        await _repository.CreateAsync(new Category("Non-Fiction"));
        await _repository.CreateAsync(new Category("Science"));

        // Act
        var allCategories = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(allCategories);
        Assert.Equal(3, allCategories.Count);
        // Should be ordered by name
        Assert.Equal("Fiction", allCategories[0].Name);
        Assert.Equal("Non-Fiction", allCategories[1].Name);
        Assert.Equal("Science", allCategories[2].Name);
    }

    [Fact]
    public async Task GetTopLevelCategoriesAsync_ShouldReturnOnlyRootCategories()
    {
        // Arrange
        var fiction = await _repository.CreateAsync(new Category("Fiction"));
        var nonFiction = await _repository.CreateAsync(new Category("Non-Fiction"));
        var sciFi = await _repository.CreateAsync(new Category("Science Fiction", parentCategoryId: fiction.Id));
        var biography = await _repository.CreateAsync(new Category("Biography", parentCategoryId: nonFiction.Id));

        // Act
        var topLevel = await _repository.GetTopLevelCategoriesAsync();

        // Assert
        Assert.NotNull(topLevel);
        Assert.Equal(2, topLevel.Count);
        Assert.Contains(topLevel, c => c.Name == "Fiction");
        Assert.Contains(topLevel, c => c.Name == "Non-Fiction");
        Assert.DoesNotContain(topLevel, c => c.Name == "Science Fiction");
        Assert.DoesNotContain(topLevel, c => c.Name == "Biography");
    }

    [Fact]
    public async Task GetChildCategoriesAsync_ShouldReturnChildrenOfParent()
    {
        // Arrange
        var fiction = await _repository.CreateAsync(new Category("Fiction"));
        var sciFi = await _repository.CreateAsync(new Category("Science Fiction", parentCategoryId: fiction.Id));
        var fantasy = await _repository.CreateAsync(new Category("Fantasy", parentCategoryId: fiction.Id));
        var horror = await _repository.CreateAsync(new Category("Horror", parentCategoryId: fiction.Id));

        // Create a different parent with child
        var nonFiction = await _repository.CreateAsync(new Category("Non-Fiction"));
        var biography = await _repository.CreateAsync(new Category("Biography", parentCategoryId: nonFiction.Id));

        // Act
        var fictionChildren = await _repository.GetChildCategoriesAsync(fiction.Id);

        // Assert
        Assert.NotNull(fictionChildren);
        Assert.Equal(3, fictionChildren.Count);
        Assert.Contains(fictionChildren, c => c.Name == "Science Fiction");
        Assert.Contains(fictionChildren, c => c.Name == "Fantasy");
        Assert.Contains(fictionChildren, c => c.Name == "Horror");
        Assert.DoesNotContain(fictionChildren, c => c.Name == "Biography");
    }

    [Fact]
    public async Task GetChildCategoriesAsync_NoChildren_ShouldReturnEmptyList()
    {
        // Arrange
        var category = await _repository.CreateAsync(new Category("Childless Category"));

        // Act
        var children = await _repository.GetChildCategoriesAsync(category.Id);

        // Assert
        Assert.NotNull(children);
        Assert.Empty(children);
    }

    [Fact]
    public async Task GetCountAsync_ShouldReturnTotalCount()
    {
        // Arrange
        await _repository.CreateAsync(new Category("Category 1"));
        await _repository.CreateAsync(new Category("Category 2"));
        await _repository.CreateAsync(new Category("Category 3"));

        // Act
        var count = await _repository.GetCountAsync();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task UpdateAsync_ExistingCategory_ShouldUpdateSuccessfully()
    {
        // Arrange
        var category = new Category("Old Name", "Old description");
        var created = await _repository.CreateAsync(category);

        // Get the category to update it
        var toUpdate = await _repository.GetByIdAsync(created.Id);
        Assert.NotNull(toUpdate);

        toUpdate.UpdateDetails("New Name", "New description");

        // Act
        var updateResult = await _repository.UpdateAsync(toUpdate);

        // Assert
        Assert.True(updateResult);

        // Verify the update
        var updated = await _repository.GetByIdAsync(created.Id);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
        Assert.Equal("New description", updated.Description);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentCategory_ShouldReturnFalse()
    {
        // Arrange
        var category = new Category("Test");
        var created = await _repository.CreateAsync(category);

        // Manually set the ID to a non-existent value using reflection
        var idProperty = typeof(Category).GetProperty("Id",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idProperty?.SetValue(created, 99999);

        // Act
        var updateResult = await _repository.UpdateAsync(created);

        // Assert
        Assert.False(updateResult);
    }

    [Fact]
    public async Task DeleteAsync_CategoryWithoutDependencies_ShouldDeleteSuccessfully()
    {
        // Arrange
        var category = new Category("ToDelete");
        var created = await _repository.CreateAsync(category);

        // Act
        var deleteResult = await _repository.DeleteAsync(created.Id);

        // Assert
        Assert.True(deleteResult);

        // Verify deletion
        var deleted = await _repository.GetByIdAsync(created.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentCategory_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = 99999;

        // Act
        var deleteResult = await _repository.DeleteAsync(nonExistentId);

        // Assert
        Assert.False(deleteResult);
    }

    [Fact]
    public async Task DeleteAsync_CategoryWithChildren_ShouldReturnFalse()
    {
        // Arrange
        var parent = await _repository.CreateAsync(new Category("Parent"));
        var child = await _repository.CreateAsync(new Category("Child", parentCategoryId: parent.Id));

        // Act
        var deleteResult = await _repository.DeleteAsync(parent.Id);

        // Assert - Should fail due to FK constraint
        Assert.False(deleteResult);

        // Verify parent still exists
        var stillExists = await _repository.GetByIdAsync(parent.Id);
        Assert.NotNull(stillExists);
    }
}
