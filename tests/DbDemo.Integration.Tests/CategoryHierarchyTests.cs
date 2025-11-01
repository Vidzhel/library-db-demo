using DbDemo.ConsoleApp.Infrastructure.Repositories;
using DbDemo.ConsoleApp.Models;
using Xunit;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Integration tests for fn_GetCategoryHierarchy recursive CTE function
/// Tests hierarchical traversal, level calculation, and path building
/// </summary>
[Collection("Database")]
public class CategoryHierarchyTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly CategoryRepository _categoryRepository;

    public CategoryHierarchyTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _categoryRepository = new CategoryRepository();
    }

    public async Task InitializeAsync()
    {
        // Cleanup before each test
        await _fixture.CleanupTableAsync("Books");
        await _fixture.CleanupTableAsync("Categories");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetHierarchyAsync_EntireTree_ShouldReturnAllCategories()
    {
        // Arrange
        await CreateTestHierarchy();

        // Act - Get entire tree (rootCategoryId = null)
        var hierarchy = await _fixture.WithTransactionAsync(tx =>
            _categoryRepository.GetHierarchyAsync(null, tx));

        // Assert
        Assert.Equal(5, hierarchy.Count); // Root, Child1, Child2, Grandchild1, Grandchild2
    }

    [Fact]
    public async Task GetHierarchyAsync_RootCategory_ShouldHaveLevelZero()
    {
        // Arrange
        await CreateTestHierarchy();

        // Act
        var hierarchy = await _fixture.WithTransactionAsync(tx =>
            _categoryRepository.GetHierarchyAsync(null, tx));

        // Assert
        var rootCategory = hierarchy.Single(c => c.Name == "Root");
        Assert.Equal(0, rootCategory.Level);
        Assert.Null(rootCategory.ParentCategoryId);
        Assert.True(rootCategory.IsRoot);
    }

    [Fact]
    public async Task GetHierarchyAsync_ChildCategories_ShouldHaveCorrectLevels()
    {
        // Arrange
        await CreateTestHierarchy();

        // Act
        var hierarchy = await _fixture.WithTransactionAsync(tx =>
            _categoryRepository.GetHierarchyAsync(null, tx));

        // Assert
        var child1 = hierarchy.Single(c => c.Name == "Child1");
        var child2 = hierarchy.Single(c => c.Name == "Child2");

        Assert.Equal(1, child1.Level);
        Assert.Equal(1, child2.Level);
        Assert.NotNull(child1.ParentCategoryId);
        Assert.NotNull(child2.ParentCategoryId);
    }

    [Fact]
    public async Task GetHierarchyAsync_GrandchildCategories_ShouldHaveCorrectLevels()
    {
        // Arrange
        await CreateTestHierarchy();

        // Act
        var hierarchy = await _fixture.WithTransactionAsync(tx =>
            _categoryRepository.GetHierarchyAsync(null, tx));

        // Assert
        var grandchild1 = hierarchy.Single(c => c.Name == "Grandchild1");
        var grandchild2 = hierarchy.Single(c => c.Name == "Grandchild2");

        Assert.Equal(2, grandchild1.Level);
        Assert.Equal(2, grandchild2.Level);
    }

    [Fact]
    public async Task GetHierarchyAsync_ShouldBuildCorrectHierarchyPaths()
    {
        // Arrange
        await CreateTestHierarchy();

        // Act
        var hierarchy = await _fixture.WithTransactionAsync(tx =>
            _categoryRepository.GetHierarchyAsync(null, tx));

        // Assert
        var root = hierarchy.Single(c => c.Name == "Root");
        var child1 = hierarchy.Single(c => c.Name == "Child1");
        var grandchild1 = hierarchy.Single(c => c.Name == "Grandchild1");

        Assert.Equal("Root", root.HierarchyPath);
        Assert.Equal("Root > Child1", child1.HierarchyPath);
        Assert.Equal("Root > Child1 > Grandchild1", grandchild1.HierarchyPath);
    }

    [Fact]
    public async Task GetHierarchyAsync_ShouldBuildCorrectFullPaths()
    {
        // Arrange
        await CreateTestHierarchy();

        // Act
        var hierarchy = await _fixture.WithTransactionAsync(tx =>
            _categoryRepository.GetHierarchyAsync(null, tx));

        // Assert
        var root = hierarchy.Single(c => c.Name == "Root");
        var child2 = hierarchy.Single(c => c.Name == "Child2");
        var grandchild2 = hierarchy.Single(c => c.Name == "Grandchild2");

        Assert.Equal("/Root", root.FullPath);
        Assert.Equal("/Root/Child2", child2.FullPath);
        Assert.Equal("/Root/Child2/Grandchild2", grandchild2.FullPath);
    }

    [Fact]
    public async Task GetHierarchyAsync_WithRootId_ShouldReturnSubtree()
    {
        // Arrange
        var child1Id = await CreateTestHierarchy();

        // Act - Get subtree starting from Child1
        var hierarchy = await _fixture.WithTransactionAsync(tx =>
            _categoryRepository.GetHierarchyAsync(child1Id, tx));

        // Assert
        Assert.Equal(2, hierarchy.Count); // Child1 and Grandchild1 only
        Assert.Contains(hierarchy, c => c.Name == "Child1");
        Assert.Contains(hierarchy, c => c.Name == "Grandchild1");
        Assert.DoesNotContain(hierarchy, c => c.Name == "Root");
        Assert.DoesNotContain(hierarchy, c => c.Name == "Child2");
    }

    [Fact]
    public async Task GetHierarchyAsync_SubtreeRoot_ShouldHaveLevelZero()
    {
        // Arrange
        var child1Id = await CreateTestHierarchy();

        // Act - Get subtree from Child1
        var hierarchy = await _fixture.WithTransactionAsync(tx =>
            _categoryRepository.GetHierarchyAsync(child1Id, tx));

        // Assert
        var subtreeRoot = hierarchy.Single(c => c.Name == "Child1");
        Assert.Equal(0, subtreeRoot.Level); // Child1 is now level 0 in this subtree

        var grandchild = hierarchy.Single(c => c.Name == "Grandchild1");
        Assert.Equal(1, grandchild.Level); // Grandchild1 is level 1 in subtree
    }

    [Fact]
    public async Task GetHierarchyAsync_LeafCategory_ShouldReturnOnlyItself()
    {
        // Arrange
        var grandchildId = await CreateGrandchildCategory();

        // Act - Get "subtree" of leaf (no children)
        var hierarchy = await _fixture.WithTransactionAsync(tx =>
            _categoryRepository.GetHierarchyAsync(grandchildId, tx));

        // Assert
        Assert.Single(hierarchy);
        Assert.Equal("Grandchild1", hierarchy[0].Name);
        Assert.Equal(0, hierarchy[0].Level);
    }

    [Fact]
    public async Task GetHierarchyAsync_EmptyDatabase_ShouldReturnEmpty()
    {
        // No categories created

        // Act
        var hierarchy = await _fixture.WithTransactionAsync(tx =>
            _categoryRepository.GetHierarchyAsync(null, tx));

        // Assert
        Assert.Empty(hierarchy);
    }

    // Helper methods
    private async Task<int> CreateTestHierarchy()
    {
        return await _fixture.WithTransactionAsync(async tx =>
        {
            // Create root
            var root = new Category("Root", "Root category");
            var createdRoot = await _categoryRepository.CreateAsync(root, tx);

            // Create children
            var child1 = new Category("Child1", "First child", createdRoot.Id);
            var createdChild1 = await _categoryRepository.CreateAsync(child1, tx);

            var child2 = new Category("Child2", "Second child", createdRoot.Id);
            var createdChild2 = await _categoryRepository.CreateAsync(child2, tx);

            // Create grandchildren - one under each child
            var grandchild1 = new Category("Grandchild1", "First grandchild", createdChild1.Id);
            await _categoryRepository.CreateAsync(grandchild1, tx);

            var grandchild2 = new Category("Grandchild2", "Second grandchild", createdChild2.Id);
            await _categoryRepository.CreateAsync(grandchild2, tx);

            return createdChild1.Id;
        });
    }

    private async Task<int> CreateGrandchildCategory()
    {
        return await _fixture.WithTransactionAsync(async tx =>
        {
            var root = new Category("Root");
            var createdRoot = await _categoryRepository.CreateAsync(root, tx);

            var child = new Category("Child1", null, createdRoot.Id);
            var createdChild = await _categoryRepository.CreateAsync(child, tx);

            var grandchild = new Category("Grandchild1", null, createdChild.Id);
            var createdGrandchild = await _categoryRepository.CreateAsync(grandchild, tx);

            return createdGrandchild.Id;
        });
    }
}
