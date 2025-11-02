using DbDemo.Application.Repositories;
using DbDemo.Infrastructure.Repositories;
using DbDemo.Domain.Entities;
using DbDemo.Application.DTOs;
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
        _repository = new LoanRepository();
        _memberRepository = new MemberRepository();
        _bookRepository = new BookRepository();
        _categoryRepository = new CategoryRepository();
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
        var category = await _fixture.WithTransactionAsync(tx => _categoryRepository.CreateAsync(new Category("Test Category"), tx));
        var member = await _fixture.WithTransactionAsync(tx => _memberRepository.CreateAsync(
            new Member("MEM001", "Test", "User", "test@example.com", DateTime.Now.AddYears(-25)), tx)
        );
        var book = await _fixture.WithTransactionAsync(tx => _bookRepository.CreateAsync(
            new Book("978-1234567890", "Test Book", category.Id, totalCopies: 1), tx)
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
        var createdLoan = await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(loan, tx));

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
        var created = await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(loan, tx));

        // Act
        var retrieved = await _fixture.WithTransactionAsync(tx => _repository.GetByIdAsync(created.Id, tx));

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
        var retrieved = await _fixture.WithTransactionAsync(tx => _repository.GetByIdAsync(nonExistentId, tx));

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
            await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(loan, tx));
            // Small delay to ensure different timestamps
            await Task.Delay(10);
        }

        // Act - Get page 1 with 3 items
        var page1 = await _fixture.WithTransactionAsync(tx => _repository.GetPagedAsync(pageNumber: 1, pageSize: 3, tx));

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
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(activeLoan, tx));

        // Create and return a loan
        var returnedLoan = Loan.Create(member.Id, book.Id);
        var created = await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(returnedLoan, tx));
        var toReturn = await _fixture.WithTransactionAsync(tx => _repository.GetByIdAsync(created.Id, tx));
        toReturn!.Return();
        await _fixture.WithTransactionAsync(tx => _repository.UpdateAsync(toReturn, tx));

        // Act
        var activeLoans = await _fixture.WithTransactionAsync(tx => _repository.GetActiveLoansByMemberIdAsync(member.Id, tx));

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

        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(overdueLoan, tx));

        // Act
        var overdueLoans = await _fixture.WithTransactionAsync(tx => _repository.GetOverdueLoansAsync(tx));

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

        var created = await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(loan, tx));

        // Return the loan
        var toReturn = await _fixture.WithTransactionAsync(tx => _repository.GetByIdAsync(created.Id, tx));
        toReturn!.Return();
        await _fixture.WithTransactionAsync(tx => _repository.UpdateAsync(toReturn, tx));

        // Act
        var overdueLoans = await _fixture.WithTransactionAsync(tx => _repository.GetOverdueLoansAsync(tx));

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
            await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(loan, tx));
        }

        // Create another member and loan to ensure filtering works
        var otherMember = await _fixture.WithTransactionAsync(tx => _memberRepository.CreateAsync(
            new Member("MEM002", "Other", "User", "other@example.com", DateTime.Now.AddYears(-30)), tx)
        );
        var otherLoan = Loan.Create(otherMember.Id, book.Id);
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(otherLoan, tx));

        // Act
        var memberLoans = await _fixture.WithTransactionAsync(tx => _repository.GetLoanHistoryByMemberIdAsync(member.Id, tx));

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
            await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(loan, tx));
        }

        // Create another book and loan to ensure filtering works
        var category = await _fixture.WithTransactionAsync(tx => _categoryRepository.GetByIdAsync(1, tx));
        var otherBook = await _fixture.WithTransactionAsync(tx => _bookRepository.CreateAsync(
            new Book("978-0987654321", "Other Book", book.CategoryId, totalCopies: 1), tx)
        );
        var otherLoan = Loan.Create(member.Id, otherBook.Id);
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(otherLoan, tx));

        // Act
        var bookLoans = await _fixture.WithTransactionAsync(tx => _repository.GetLoanHistoryByBookIdAsync(book.Id, tx));

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

        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(Loan.Create(member.Id, book.Id), tx));
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(Loan.Create(member.Id, book.Id), tx));
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(Loan.Create(member.Id, book.Id), tx));

        // Act
        var count = await _fixture.WithTransactionAsync(tx => _repository.GetCountAsync(status: null, tx));

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetCountAsync_ByStatus_ShouldReturnFilteredCount()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        // Create 2 active loans
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(Loan.Create(member.Id, book.Id), tx));
        await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(Loan.Create(member.Id, book.Id), tx));

        // Create 1 returned loan
        var returnedLoan = Loan.Create(member.Id, book.Id);
        var created = await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(returnedLoan, tx));
        var toReturn = await _fixture.WithTransactionAsync(tx => _repository.GetByIdAsync(created.Id, tx));
        toReturn!.Return();
        await _fixture.WithTransactionAsync(tx => _repository.UpdateAsync(toReturn, tx));

        // Act
        var activeCount = await _fixture.WithTransactionAsync(tx => _repository.GetCountAsync(LoanStatus.Active, tx));
        var returnedCount = await _fixture.WithTransactionAsync(tx => _repository.GetCountAsync(LoanStatus.Returned, tx));

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
        var created = await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(loan, tx));

        // Get the loan and return it
        var toReturn = await _fixture.WithTransactionAsync(tx => _repository.GetByIdAsync(created.Id, tx));
        Assert.NotNull(toReturn);
        toReturn.Return();

        // Act
        var updateResult = await _fixture.WithTransactionAsync(tx => _repository.UpdateAsync(toReturn, tx));

        // Assert
        Assert.True(updateResult);

        // Verify the update
        var updated = await _fixture.WithTransactionAsync(tx => _repository.GetByIdAsync(created.Id, tx));
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
        var created = await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(loan, tx));

        // Manually set the ID to a non-existent value using reflection
        var idProperty = typeof(Loan).GetProperty("Id",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        idProperty?.SetValue(created, 99999);

        // Act
        var updateResult = await _fixture.WithTransactionAsync(tx => _repository.UpdateAsync(created, tx));

        // Assert
        Assert.False(updateResult);
    }

    [Fact]
    public async Task DeleteAsync_ExistingLoan_ShouldDeleteSuccessfully()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();
        var loan = Loan.Create(member.Id, book.Id);
        var created = await _fixture.WithTransactionAsync(tx => _repository.CreateAsync(loan, tx));

        // Act
        var deleteResult = await _fixture.WithTransactionAsync(tx => _repository.DeleteAsync(created.Id, tx));

        // Assert
        Assert.True(deleteResult);

        // Verify deletion
        var deleted = await _fixture.WithTransactionAsync(tx => _repository.GetByIdAsync(created.Id, tx));
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentLoan_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = 99999;

        // Act
        var deleteResult = await _fixture.WithTransactionAsync(tx => _repository.DeleteAsync(nonExistentId, tx));

        // Assert
        Assert.False(deleteResult);
    }
}
