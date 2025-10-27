using DbDemo.ConsoleApp.Infrastructure.Repositories;
using DbDemo.ConsoleApp.Models;
using Xunit;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Integration tests for LoanRepository
/// Tests actual database operations using the Docker SQL Server instance
/// Requires Members and Books to be present for testing
/// </summary>
[Collection("Database")]
public class LoanRepositoryTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly LoanRepository _repository;
    private readonly MemberRepository _memberRepository;
    private readonly BookRepository _bookRepository;
    private readonly CategoryRepository _categoryRepository;

    public LoanRepositoryTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _repository = new LoanRepository(_fixture.ConnectionString);
        _memberRepository = new MemberRepository(_fixture.ConnectionString);
        _bookRepository = new BookRepository(_fixture.ConnectionString);
        _categoryRepository = new CategoryRepository(_fixture.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        // Cleanup before each test (respect FK constraints)
        await _fixture.CleanupTableAsync("Loans");
        await _fixture.CleanupTableAsync("BookAuthors");
        await _fixture.CleanupTableAsync("Books");
        await _fixture.CleanupTableAsync("Members");
        await _fixture.CleanupTableAsync("Categories");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Member member, Book book)> CreateTestMemberAndBook()
    {
        var category = await _categoryRepository.CreateAsync(new Category("Test Category"));
        var member = await _memberRepository.CreateAsync(
            new Member("MEM001", "Test", "User", "test@example.com", DateTime.Now.AddYears(-25))
        );
        var book = await _bookRepository.CreateAsync(
            new Book("978-1234567890", "Test Book", category.Id, totalCopies: 1)
        );
        return (member, book);
    }

    [Fact]
    public async Task CreateAsync_ValidLoan_ShouldInsertAndReturnLoanWithId()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();
        var loan = Loan.Create(member.Id, book.Id);

        // Act
        var createdLoan = await _repository.CreateAsync(loan);

        // Assert
        Assert.NotNull(createdLoan);
        Assert.True(createdLoan.Id > 0, "Created loan should have an ID assigned");
        Assert.Equal(member.Id, createdLoan.MemberId);
        Assert.Equal(book.Id, createdLoan.BookId);
        Assert.Equal(LoanStatus.Active, createdLoan.Status);
        Assert.Null(createdLoan.ReturnedAt);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingLoan_ShouldReturnLoan()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();
        var loan = Loan.Create(member.Id, book.Id);
        var created = await _repository.CreateAsync(loan);

        // Act
        var retrieved = await _repository.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal(member.Id, retrieved.MemberId);
        Assert.Equal(book.Id, retrieved.BookId);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentLoan_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = 99999;

        // Act
        var retrieved = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResults()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        // Create 5 loans
        for (int i = 0; i < 5; i++)
        {
            var loan = Loan.Create(member.Id, book.Id);
            await _repository.CreateAsync(loan);
            // Small delay to ensure different timestamps
            await Task.Delay(10);
        }

        // Act - Get page 1 with 3 items
        var page1 = await _repository.GetPagedAsync(pageNumber: 1, pageSize: 3);

        // Assert
        Assert.NotNull(page1);
        Assert.Equal(3, page1.Count);
        // Should be ordered by BorrowedAt DESC (most recent first)
    }

    [Fact]
    public async Task GetActiveLoansByMemberIdAsync_ShouldReturnOnlyActiveLoans()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        // Create active loan
        var activeLoan = Loan.Create(member.Id, book.Id);
        await _repository.CreateAsync(activeLoan);

        // Create and return a loan
        var returnedLoan = Loan.Create(member.Id, book.Id);
        var created = await _repository.CreateAsync(returnedLoan);
        var toReturn = await _repository.GetByIdAsync(created.Id);
        toReturn!.Return();
        await _repository.UpdateAsync(toReturn);

        // Act
        var activeLoans = await _repository.GetActiveLoansByMemberIdAsync(member.Id);

        // Assert
        Assert.NotNull(activeLoans);
        Assert.Single(activeLoans);
        Assert.Null(activeLoans[0].ReturnedAt);
    }

    [Fact]
    public async Task GetOverdueLoansAsync_ShouldReturnOverdueLoans()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        // Create a loan with a past due date using reflection
        var overdueLoan = Loan.Create(member.Id, book.Id);

        // Use reflection to set DueDate to the past BEFORE inserting
        var dueDateProperty = typeof(Loan).GetProperty("DueDate",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var borrowedAtProperty = typeof(Loan).GetProperty("BorrowedAt",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var pastDate = DateTime.UtcNow.AddDays(-10);
        borrowedAtProperty?.SetValue(overdueLoan, pastDate);
        dueDateProperty?.SetValue(overdueLoan, pastDate.AddDays(5)); // Due 5 days ago

        await _repository.CreateAsync(overdueLoan);

        // Act
        var overdueLoans = await _repository.GetOverdueLoansAsync();

        // Assert
        Assert.NotNull(overdueLoans);
        Assert.Single(overdueLoans);
        Assert.Null(overdueLoans[0].ReturnedAt);
    }

    [Fact]
    public async Task GetOverdueLoansAsync_ShouldNotReturnReturnedLoans()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        // Create an overdue loan using reflection to set dates in the past
        var loan = Loan.Create(member.Id, book.Id);

        var dueDateProperty = typeof(Loan).GetProperty("DueDate",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var borrowedAtProperty = typeof(Loan).GetProperty("BorrowedAt",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var pastDate = DateTime.UtcNow.AddDays(-10);
        borrowedAtProperty?.SetValue(loan, pastDate);
        dueDateProperty?.SetValue(loan, pastDate.AddDays(5)); // Due 5 days ago

        var created = await _repository.CreateAsync(loan);

        // Return the loan
        var toReturn = await _repository.GetByIdAsync(created.Id);
        toReturn!.Return();
        await _repository.UpdateAsync(toReturn);

        // Act
        var overdueLoans = await _repository.GetOverdueLoansAsync();

        // Assert - Should be empty because the loan was returned
        Assert.NotNull(overdueLoans);
        Assert.Empty(overdueLoans);
    }

    [Fact]
    public async Task GetLoanHistoryByMemberIdAsync_ShouldReturnAllMemberLoans()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        // Create 3 loans for the member
        for (int i = 0; i < 3; i++)
        {
            var loan = Loan.Create(member.Id, book.Id);
            await _repository.CreateAsync(loan);
        }

        // Create another member and loan to ensure filtering works
        var otherMember = await _memberRepository.CreateAsync(
            new Member("MEM002", "Other", "User", "other@example.com", DateTime.Now.AddYears(-30))
        );
        var otherLoan = Loan.Create(otherMember.Id, book.Id);
        await _repository.CreateAsync(otherLoan);

        // Act
        var memberLoans = await _repository.GetLoanHistoryByMemberIdAsync(member.Id);

        // Assert
        Assert.NotNull(memberLoans);
        Assert.Equal(3, memberLoans.Count);
        Assert.All(memberLoans, l => Assert.Equal(member.Id, l.MemberId));
    }

    [Fact]
    public async Task GetLoanHistoryByBookIdAsync_ShouldReturnAllBookLoans()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        // Create 3 loans for the book
        for (int i = 0; i < 3; i++)
        {
            var loan = Loan.Create(member.Id, book.Id);
            await _repository.CreateAsync(loan);
        }

        // Create another book and loan to ensure filtering works
        var category = await _categoryRepository.GetByIdAsync(1);
        var otherBook = await _bookRepository.CreateAsync(
            new Book("978-0987654321", "Other Book", book.CategoryId, totalCopies: 1)
        );
        var otherLoan = Loan.Create(member.Id, otherBook.Id);
        await _repository.CreateAsync(otherLoan);

        // Act
        var bookLoans = await _repository.GetLoanHistoryByBookIdAsync(book.Id);

        // Assert
        Assert.NotNull(bookLoans);
        Assert.Equal(3, bookLoans.Count);
        Assert.All(bookLoans, l => Assert.Equal(book.Id, l.BookId));
    }

    [Fact]
    public async Task GetCountAsync_ShouldReturnTotalCount()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        await _repository.CreateAsync(Loan.Create(member.Id, book.Id));
        await _repository.CreateAsync(Loan.Create(member.Id, book.Id));
        await _repository.CreateAsync(Loan.Create(member.Id, book.Id));

        // Act
        var count = await _repository.GetCountAsync();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetCountAsync_ByStatus_ShouldReturnFilteredCount()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        // Create 2 active loans
        await _repository.CreateAsync(Loan.Create(member.Id, book.Id));
        await _repository.CreateAsync(Loan.Create(member.Id, book.Id));

        // Create 1 returned loan
        var returnedLoan = Loan.Create(member.Id, book.Id);
        var created = await _repository.CreateAsync(returnedLoan);
        var toReturn = await _repository.GetByIdAsync(created.Id);
        toReturn!.Return();
        await _repository.UpdateAsync(toReturn);

        // Act
        var activeCount = await _repository.GetCountAsync(LoanStatus.Active);
        var returnedCount = await _repository.GetCountAsync(LoanStatus.Returned);

        // Assert
        Assert.Equal(2, activeCount);
        Assert.Equal(1, returnedCount);
    }

    [Fact]
    public async Task UpdateAsync_ExistingLoan_ShouldUpdateSuccessfully()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();
        var loan = Loan.Create(member.Id, book.Id);
        var created = await _repository.CreateAsync(loan);

        // Get the loan and return it
        var toReturn = await _repository.GetByIdAsync(created.Id);
        Assert.NotNull(toReturn);
        toReturn.Return();

        // Act
        var updateResult = await _repository.UpdateAsync(toReturn);

        // Assert
        Assert.True(updateResult);

        // Verify the update
        var updated = await _repository.GetByIdAsync(created.Id);
        Assert.NotNull(updated);
        Assert.NotNull(updated.ReturnedAt);
        Assert.NotEqual(LoanStatus.Active, updated.Status);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentLoan_ShouldReturnFalse()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();
        var loan = Loan.Create(member.Id, book.Id);
        var created = await _repository.CreateAsync(loan);

        // Manually set the ID to a non-existent value using reflection
        var idProperty = typeof(Loan).GetProperty("Id",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idProperty?.SetValue(created, 99999);

        // Act
        var updateResult = await _repository.UpdateAsync(created);

        // Assert
        Assert.False(updateResult);
    }

    [Fact]
    public async Task DeleteAsync_ExistingLoan_ShouldDeleteSuccessfully()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();
        var loan = Loan.Create(member.Id, book.Id);
        var created = await _repository.CreateAsync(loan);

        // Act
        var deleteResult = await _repository.DeleteAsync(created.Id);

        // Assert
        Assert.True(deleteResult);

        // Verify deletion
        var deleted = await _repository.GetByIdAsync(created.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentLoan_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = 99999;

        // Act
        var deleteResult = await _repository.DeleteAsync(nonExistentId);

        // Assert
        Assert.False(deleteResult);
    }
}
