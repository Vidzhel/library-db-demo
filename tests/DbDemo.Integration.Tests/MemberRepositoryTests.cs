using DbDemo.ConsoleApp.Infrastructure.Repositories;
using DbDemo.ConsoleApp.Models;
using Xunit;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Integration tests for MemberRepository
/// Tests actual database operations using the Docker SQL Server instance
/// </summary>
[Collection("Database")]
public class MemberRepositoryTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly MemberRepository _repository;

    public MemberRepositoryTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _repository = new MemberRepository(_fixture.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        // Cleanup before each test (respect FK constraints)
        await _fixture.CleanupTableAsync("Loans");
        await _fixture.CleanupTableAsync("Members");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateAsync_ValidMember_ShouldInsertAndReturnMemberWithId()
    {
        // Arrange
        var member = new Member(
            membershipNumber: "MEM001",
            firstName: "John",
            lastName: "Doe",
            email: "john.doe@example.com",
            dateOfBirth: new DateTime(1990, 5, 15)
        );

        // Act
        var createdMember = await _repository.CreateAsync(member);

        // Assert
        Assert.NotNull(createdMember);
        Assert.True(createdMember.Id > 0, "Created member should have an ID assigned");
        Assert.Equal("MEM001", createdMember.MembershipNumber);
        Assert.Equal("John", createdMember.FirstName);
        Assert.Equal("Doe", createdMember.LastName);
        Assert.Equal("john.doe@example.com", createdMember.Email);
        Assert.True(createdMember.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingMember_ShouldReturnMember()
    {
        // Arrange
        var member = new Member("MEM002", "Jane", "Smith", "jane@example.com", new DateTime(1985, 3, 20));
        var created = await _repository.CreateAsync(member);

        // Act
        var retrieved = await _repository.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("Jane", retrieved.FirstName);
        Assert.Equal("Smith", retrieved.LastName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentMember_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = 99999;

        // Act
        var retrieved = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetByMembershipNumberAsync_ExistingMember_ShouldReturnMember()
    {
        // Arrange
        var member = new Member("MEM003", "Bob", "Johnson", "bob@example.com", new DateTime(1992, 7, 10));
        await _repository.CreateAsync(member);

        // Act
        var retrieved = await _repository.GetByMembershipNumberAsync("MEM003");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("MEM003", retrieved.MembershipNumber);
        Assert.Equal("Bob", retrieved.FirstName);
    }

    [Fact]
    public async Task GetByMembershipNumberAsync_NonExistentNumber_ShouldReturnNull()
    {
        // Arrange
        var nonExistentNumber = "NONEXISTENT";

        // Act
        var retrieved = await _repository.GetByMembershipNumberAsync(nonExistentNumber);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetByEmailAsync_ExistingMember_ShouldReturnMember()
    {
        // Arrange
        var member = new Member("MEM004", "Alice", "Brown", "alice@example.com", new DateTime(1988, 11, 25));
        await _repository.CreateAsync(member);

        // Act - Email should be case-insensitive
        var retrieved = await _repository.GetByEmailAsync("ALICE@EXAMPLE.COM");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("alice@example.com", retrieved.Email);
        Assert.Equal("Alice", retrieved.FirstName);
    }

    [Fact]
    public async Task GetByEmailAsync_NonExistentEmail_ShouldReturnNull()
    {
        // Arrange
        var nonExistentEmail = "nonexistent@example.com";

        // Act
        var retrieved = await _repository.GetByEmailAsync(nonExistentEmail);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResults()
    {
        // Arrange - Create 5 members
        await _repository.CreateAsync(new Member("MEM101", "Alice", "Anderson", "alice.a@example.com", DateTime.Now.AddYears(-30)));
        await _repository.CreateAsync(new Member("MEM102", "Bob", "Brown", "bob.b@example.com", DateTime.Now.AddYears(-25)));
        await _repository.CreateAsync(new Member("MEM103", "Charlie", "Clark", "charlie.c@example.com", DateTime.Now.AddYears(-35)));
        await _repository.CreateAsync(new Member("MEM104", "David", "Davis", "david.d@example.com", DateTime.Now.AddYears(-28)));
        await _repository.CreateAsync(new Member("MEM105", "Eve", "Evans", "eve.e@example.com", DateTime.Now.AddYears(-32)));

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
    public async Task GetPagedAsync_ActiveOnly_ShouldReturnOnlyActiveMembers()
    {
        // Arrange
        var activeMember = new Member("MEM201", "Active", "User", "active@example.com", DateTime.Now.AddYears(-25));
        await _repository.CreateAsync(activeMember);

        var inactiveMember = new Member("MEM202", "Inactive", "User", "inactive@example.com", DateTime.Now.AddYears(-30));
        var created = await _repository.CreateAsync(inactiveMember);

        // Deactivate the second member
        var toDeactivate = await _repository.GetByIdAsync(created.Id);
        toDeactivate!.Deactivate();
        await _repository.UpdateAsync(toDeactivate);

        // Act
        var activeMembers = await _repository.GetPagedAsync(pageNumber: 1, pageSize: 10, activeOnly: true);

        // Assert
        Assert.NotNull(activeMembers);
        Assert.Single(activeMembers);
        Assert.Equal("Active", activeMembers[0].FirstName);
    }

    [Fact]
    public async Task SearchByNameAsync_ByFirstName_ShouldReturnMatchingMembers()
    {
        // Arrange
        await _repository.CreateAsync(new Member("MEM301", "John", "Smith", "john.s@example.com", DateTime.Now.AddYears(-30)));
        await _repository.CreateAsync(new Member("MEM302", "John", "Doe", "john.d@example.com", DateTime.Now.AddYears(-25)));
        await _repository.CreateAsync(new Member("MEM303", "Jane", "Williams", "jane@example.com", DateTime.Now.AddYears(-28)));

        // Act
        var results = await _repository.SearchByNameAsync("John");

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.All(results, m => Assert.Equal("John", m.FirstName));
    }

    [Fact]
    public async Task SearchByNameAsync_ByLastName_ShouldReturnMatchingMembers()
    {
        // Arrange
        await _repository.CreateAsync(new Member("MEM401", "John", "Smith", "john@example.com", DateTime.Now.AddYears(-30)));
        await _repository.CreateAsync(new Member("MEM402", "Jane", "Smith", "jane@example.com", DateTime.Now.AddYears(-25)));
        await _repository.CreateAsync(new Member("MEM403", "Bob", "Johnson", "bob@example.com", DateTime.Now.AddYears(-28)));

        // Act
        var results = await _repository.SearchByNameAsync("Smith");

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.All(results, m => Assert.Equal("Smith", m.LastName));
    }

    [Fact]
    public async Task GetCountAsync_ShouldReturnTotalCount()
    {
        // Arrange
        await _repository.CreateAsync(new Member("MEM501", "Member", "One", "one@example.com", DateTime.Now.AddYears(-30)));
        await _repository.CreateAsync(new Member("MEM502", "Member", "Two", "two@example.com", DateTime.Now.AddYears(-25)));
        await _repository.CreateAsync(new Member("MEM503", "Member", "Three", "three@example.com", DateTime.Now.AddYears(-28)));

        // Act
        var count = await _repository.GetCountAsync();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetCountAsync_ActiveOnly_ShouldReturnActiveCount()
    {
        // Arrange
        await _repository.CreateAsync(new Member("MEM601", "Active", "One", "active1@example.com", DateTime.Now.AddYears(-30)));
        await _repository.CreateAsync(new Member("MEM602", "Active", "Two", "active2@example.com", DateTime.Now.AddYears(-25)));

        var inactive = await _repository.CreateAsync(new Member("MEM603", "Inactive", "One", "inactive@example.com", DateTime.Now.AddYears(-28)));
        var toDeactivate = await _repository.GetByIdAsync(inactive.Id);
        toDeactivate!.Deactivate();
        await _repository.UpdateAsync(toDeactivate);

        // Act
        var activeCount = await _repository.GetCountAsync(activeOnly: true);

        // Assert
        Assert.Equal(2, activeCount);
    }

    [Fact]
    public async Task UpdateAsync_ExistingMember_ShouldUpdateSuccessfully()
    {
        // Arrange
        var member = new Member("MEM701", "Old", "Name", "old@example.com", DateTime.Now.AddYears(-30));
        var created = await _repository.CreateAsync(member);

        // Get the member to update it
        var toUpdate = await _repository.GetByIdAsync(created.Id);
        Assert.NotNull(toUpdate);

        toUpdate.UpdateContactInfo("new@example.com", "555-1234", "123 New Street");

        // Act
        var updateResult = await _repository.UpdateAsync(toUpdate);

        // Assert
        Assert.True(updateResult);

        // Verify the update
        var updated = await _repository.GetByIdAsync(created.Id);
        Assert.NotNull(updated);
        Assert.Equal("new@example.com", updated.Email);
        Assert.Equal("555-1234", updated.PhoneNumber);
        Assert.Equal("123 New Street", updated.Address);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentMember_ShouldReturnFalse()
    {
        // Arrange
        var member = new Member("MEM801", "Test", "Member", "test@example.com", DateTime.Now.AddYears(-30));
        var created = await _repository.CreateAsync(member);

        // Manually set the ID to a non-existent value using reflection
        var idProperty = typeof(Member).GetProperty("Id",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idProperty?.SetValue(created, 99999);

        // Act
        var updateResult = await _repository.UpdateAsync(created);

        // Assert
        Assert.False(updateResult);
    }

    [Fact]
    public async Task DeleteAsync_MemberWithoutLoans_ShouldDeleteSuccessfully()
    {
        // Arrange
        var member = new Member("MEM901", "ToDelete", "Member", "delete@example.com", DateTime.Now.AddYears(-30));
        var created = await _repository.CreateAsync(member);

        // Act
        var deleteResult = await _repository.DeleteAsync(created.Id);

        // Assert
        Assert.True(deleteResult);

        // Verify deletion
        var deleted = await _repository.GetByIdAsync(created.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentMember_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = 99999;

        // Act
        var deleteResult = await _repository.DeleteAsync(nonExistentId);

        // Assert
        Assert.False(deleteResult);
    }
}
