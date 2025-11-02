using DbDemo.Application.Repositories;
using DbDemo.Infrastructure.Repositories;
using DbDemo.Domain.Entities;
using DbDemo.Application.DTOs;
using Xunit;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Integration tests for SQL Server spatial data (GEOGRAPHY type).
/// Tests location storage, distance calculations, and proximity queries.
/// </summary>
[Collection("Database")]
public class SpatialDataTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly LibraryBranchRepository _repository;

    public SpatialDataTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _repository = new LibraryBranchRepository();
    }

    public async Task InitializeAsync()
    {
        // Cleanup before each test
        await _fixture.CleanupTableAsync("LibraryBranches");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateBranch_WithLocation_ShouldStoreCoordinates()
    {
        // Arrange
        var branch = new LibraryBranch("Vienna Central", "Urban-Loritz-Platz 2a", "Vienna", "1070");
        branch.SetLocation(48.2082, 16.3738);

        // Act
        var created = await _fixture.WithTransactionAsync(tx =>
            _repository.CreateAsync(branch, tx));

        // Assert
        Assert.True(created.Id > 0);
        Assert.Equal(48.2082, created.Latitude);
        Assert.Equal(16.3738, created.Longitude);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnBranchWithLocation()
    {
        // Arrange
        var branch = new LibraryBranch("Test Branch", "Test Address", "Vienna");
        branch.SetLocation(48.2082, 16.3738);

        var created = await _fixture.WithTransactionAsync(tx =>
            _repository.CreateAsync(branch, tx));

        // Act
        var retrieved = await _fixture.WithTransactionAsync(tx =>
            _repository.GetByIdAsync(created.Id, tx));

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(48.2082, retrieved.Latitude);
        Assert.Equal(16.3738, retrieved.Longitude);
    }

    [Fact]
    public async Task FindWithinDistanceAsync_ShouldReturnNearbyBranches()
    {
        // Arrange - Create branches in Vienna
        await CreateTestBranch("Vienna Central", 48.2082, 16.3738);
        await CreateTestBranch("Vienna University", 48.2108, 16.3608);
        await CreateTestBranch("Graz Library", 47.0707, 15.4395); // ~150km away

        // Act - Find branches within 50km of Vienna center
        var nearby = await _fixture.WithTransactionAsync(tx =>
            _repository.FindWithinDistanceAsync(48.2082, 16.3738, 50, tx));

        // Assert - Should find 2 Vienna branches, not Graz
        Assert.Equal(2, nearby.Count);
        Assert.All(nearby, result => Assert.True(result.DistanceKm <= 50));
    }

    [Fact]
    public async Task FindNearestAsync_ShouldReturnClosestBranches()
    {
        // Arrange
        await CreateTestBranch("Branch A", 48.2082, 16.3738); // Vienna center
        await CreateTestBranch("Branch B", 48.2108, 16.3608); // ~1.5km away
        await CreateTestBranch("Branch C", 48.2319, 16.4440); // ~8km away
        await CreateTestBranch("Branch D", 47.0707, 15.4395); // ~150km away

        // Act - Find 2 nearest branches to Vienna center
        var nearest = await _fixture.WithTransactionAsync(tx =>
            _repository.FindNearestAsync(48.2082, 16.3738, 2, tx));

        // Assert
        Assert.Equal(2, nearest.Count);
        Assert.True(nearest[0].DistanceKm < nearest[1].DistanceKm); // Ordered by distance
        Assert.True(nearest[1].DistanceKm < 10); // Both should be close
    }

    [Fact]
    public async Task FindWithinDistanceAsync_WithLargeRadius_ShouldReturnAllBranches()
    {
        // Arrange
        await CreateTestBranch("Vienna", 48.2082, 16.3738);
        await CreateTestBranch("Graz", 47.0707, 15.4395);
        await CreateTestBranch("Linz", 48.3066, 14.2855);

        // Act - Find branches within 500km
        var results = await _fixture.WithTransactionAsync(tx =>
            _repository.FindWithinDistanceAsync(48.2082, 16.3738, 500, tx));

        // Assert - Should find all 3 branches
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllBranches()
    {
        // Arrange
        await CreateTestBranch("Branch 1", 48.2082, 16.3738);
        await CreateTestBranch("Branch 2", 48.2108, 16.3608);
        await CreateTestBranch("Branch 3", 47.0707, 15.4395);

        // Act
        var all = await _fixture.WithTransactionAsync(tx =>
            _repository.GetAllAsync(tx));

        // Assert
        Assert.Equal(3, all.Count);
        Assert.All(all, branch =>
        {
            Assert.NotNull(branch.Latitude);
            Assert.NotNull(branch.Longitude);
        });
    }

    [Fact]
    public async Task DistanceCalculation_ShouldBeAccurate()
    {
        // Arrange - Vienna to Graz is approximately 150km
        await CreateTestBranch("Vienna", 48.2082, 16.3738);
        await CreateTestBranch("Graz", 47.0707, 15.4395);

        // Act
        var results = await _fixture.WithTransactionAsync(tx =>
            _repository.FindNearestAsync(48.2082, 16.3738, 10, tx));

        var grazDistance = results.First(r => r.Branch.BranchName == "Graz").DistanceKm;

        // Assert - Distance should be approximately 150km (±10km tolerance)
        Assert.InRange(grazDistance, 140, 160);
    }

    // Helper method
    private async Task<LibraryBranch> CreateTestBranch(string name, double latitude, double longitude)
    {
        var branch = new LibraryBranch(name, "Test Address", "Test City");
        branch.SetLocation(latitude, longitude);

        return await _fixture.WithTransactionAsync(tx =>
            _repository.CreateAsync(branch, tx));
    }
}
