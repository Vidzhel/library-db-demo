using DbDemo.Application.Repositories;
using DbDemo.Infrastructure.Repositories;
using DbDemo.Domain.Entities;
using DbDemo.Application.DTOs;
using DbDemo.Application.Services;
using Xunit;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Integration tests for LoanService
///
/// ⚠️ WARNING: These tests demonstrate the anti-pattern of multi-step operations WITHOUT transactions.
/// The LoanService deliberately omits transaction support to show the dangers.
/// See docs/20-transaction-problem.md for detailed explanation.
/// </summary>
public class LoanServiceTests : IClassFixture<DatabaseTestFixture>
{
    private readonly DatabaseTestFixture _fixture;
    private readonly LoanService _loanService;
    private readonly BookRepository _bookRepository;
    private readonly MemberRepository _memberRepository;
    private readonly LoanRepository _loanRepository;

    public LoanServiceTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _bookRepository = new BookRepository();
        _memberRepository = new MemberRepository();
        _loanRepository = new LoanRepository();
        _loanService = new LoanService(
            _loanRepository,
            _bookRepository,
            _memberRepository,
            _fixture.ConnectionString);
    }

    [Fact]
    public async Task CreateLoanAsync_WithValidData_CreatesLoanSuccessfully()
    {
        // Arrange
        await CleanupAsync();

        var category = await CreateTestCategoryAsync();
        var book = await CreateTestBookAsync(category.Id, availableCopies: 5);
        var member = await CreateTestMemberAsync();

        var initialAvailableCopies = book.AvailableCopies;

        // Act
        var loan = await _fixture.WithTransactionAsync(tx => _loanService.CreateLoanAsync(member.Id, book.Id, tx));

        // Assert
        Assert.NotNull(loan);
        Assert.True(loan.Id > 0);
        Assert.Equal(member.Id, loan.MemberId);
        Assert.Equal(book.Id, loan.BookId);
        Assert.Equal(LoanStatus.Active, loan.Status);

        // Verify book inventory was decremented
        var updatedBook = await _fixture.WithTransactionAsync(tx => _bookRepository.GetByIdAsync(book.Id, tx));
        Assert.NotNull(updatedBook);
        Assert.Equal(initialAvailableCopies - 1, updatedBook.AvailableCopies);
    }

    [Fact]
    public async Task CreateLoanAsync_WhenMemberInactive_ThrowsInvalidOperationException()
    {
        // Arrange
        await CleanupAsync();

        var category = await CreateTestCategoryAsync();
        var book = await CreateTestBookAsync(category.Id);
        var member = await CreateTestMemberAsync();

        // Deactivate member
        member.Deactivate();
        await _fixture.WithTransactionAsync(tx => _memberRepository.UpdateAsync(member, tx));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _fixture.WithTransactionAsync(tx => _loanService.CreateLoanAsync(member.Id, book.Id, tx)));
        Assert.Contains("is not active", exception.Message);
    }

    [Fact]
    public async Task CreateLoanAsync_WhenBookNotAvailable_ThrowsInvalidOperationException()
    {
        // Arrange
        await CleanupAsync();

        var category = await CreateTestCategoryAsync();
        var book = await CreateTestBookAsync(category.Id, availableCopies: 0);
        var member = await CreateTestMemberAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _fixture.WithTransactionAsync(tx => _loanService.CreateLoanAsync(member.Id, book.Id, tx)));
        Assert.Contains("is not available", exception.Message);
    }

    [Fact]
    public async Task ReturnLoanAsync_WithValidLoan_ReturnsLoanSuccessfully()
    {
        // Arrange
        await CleanupAsync();

        var category = await CreateTestCategoryAsync();
        var book = await CreateTestBookAsync(category.Id, availableCopies: 5);
        var member = await CreateTestMemberAsync();

        var loan = await _fixture.WithTransactionAsync(tx => _loanService.CreateLoanAsync(member.Id, book.Id, tx));
        var availableCopiesAfterBorrow = (await _fixture.WithTransactionAsync(tx => _bookRepository.GetByIdAsync(book.Id, tx)))!.AvailableCopies;

        // Act
        var returnedLoan = await _fixture.WithTransactionAsync(tx => _loanService.ReturnLoanAsync(loan.Id, tx));

        // Assert
        Assert.NotNull(returnedLoan);
        Assert.NotNull(returnedLoan.ReturnedAt);
        Assert.Equal(LoanStatus.Returned, returnedLoan.Status);

        // Verify book inventory was incremented
        var updatedBook = await _fixture.WithTransactionAsync(tx => _bookRepository.GetByIdAsync(book.Id, tx));
        Assert.NotNull(updatedBook);
        Assert.Equal(availableCopiesAfterBorrow + 1, updatedBook.AvailableCopies);
    }

    [Fact]
    public async Task RenewLoanAsync_WithValidLoan_RenewsSuccessfully()
    {
        // Arrange
        await CleanupAsync();

        var category = await CreateTestCategoryAsync();
        var book = await CreateTestBookAsync(category.Id);
        var member = await CreateTestMemberAsync();

        var loan = await _fixture.WithTransactionAsync(tx => _loanService.CreateLoanAsync(member.Id, book.Id, tx));
        var originalDueDate = loan.DueDate;
        var originalRenewalCount = loan.RenewalCount;

        // Act
        var renewedLoan = await _fixture.WithTransactionAsync(tx => _loanService.RenewLoanAsync(loan.Id, tx));

        // Assert
        Assert.NotNull(renewedLoan);
        Assert.True(renewedLoan.DueDate > originalDueDate);
        Assert.Equal(originalRenewalCount + 1, renewedLoan.RenewalCount);
    }

    [Fact]
    public async Task RenewLoanAsync_WhenMaxRenewalsReached_ThrowsInvalidOperationException()
    {
        // Arrange
        await CleanupAsync();

        var category = await CreateTestCategoryAsync();
        var book = await CreateTestBookAsync(category.Id);
        var member = await CreateTestMemberAsync();

        var loan = await _fixture.WithTransactionAsync(tx => _loanService.CreateLoanAsync(member.Id, book.Id, tx));

        // Renew maximum number of times (default is 2 renewals allowed)
        await _fixture.WithTransactionAsync(tx => _loanService.RenewLoanAsync(loan.Id, tx));
        await _fixture.WithTransactionAsync(tx => _loanService.RenewLoanAsync(loan.Id, tx));

        // Act & Assert - third renewal should fail
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _fixture.WithTransactionAsync(tx => _loanService.RenewLoanAsync(loan.Id, tx)));
        Assert.Contains("cannot be renewed", exception.Message);
    }

    [Fact]
    public async Task GetActiveLoansByMemberAsync_ReturnsOnlyActiveLoans()
    {
        // Arrange
        await CleanupAsync();

        var category = await CreateTestCategoryAsync();
        var book1 = await CreateTestBookAsync(category.Id);
        var book2 = await CreateTestBookAsync(category.Id);
        var book3 = await CreateTestBookAsync(category.Id);
        var member = await CreateTestMemberAsync();

        var loan1 = await _fixture.WithTransactionAsync(tx => _loanService.CreateLoanAsync(member.Id, book1.Id, tx));
        var loan2 = await _fixture.WithTransactionAsync(tx => _loanService.CreateLoanAsync(member.Id, book2.Id, tx));
        var loan3 = await _fixture.WithTransactionAsync(tx => _loanService.CreateLoanAsync(member.Id, book3.Id, tx));

        // Return one loan
        await _fixture.WithTransactionAsync(tx => _loanService.ReturnLoanAsync(loan2.Id, tx));

        // Act
        var activeLoans = await _fixture.WithTransactionAsync(tx => _loanService.GetActiveLoansByMemberAsync(member.Id, tx));

        // Assert
        Assert.Equal(2, activeLoans.Count);
        Assert.Contains(activeLoans, l => l.Id == loan1.Id);
        Assert.Contains(activeLoans, l => l.Id == loan3.Id);
        Assert.DoesNotContain(activeLoans, l => l.Id == loan2.Id);
    }

    // ⚠️ NOTE: Tests for demonstrating partial failure/data inconsistency
    // would require simulating database failures (e.g., connection drops, constraint violations).
    // In Commit 22 (with transactions), we'll add tests that verify rollback behavior.
    // For now, these tests verify the "happy path" works, but the service is vulnerable
    // to partial failures that would leave inconsistent data.

    #region Test Helper Methods

    private async Task CleanupAsync()
    {
        await _fixture.CleanupTableAsync("Loans");
        await _fixture.CleanupTableAsync("BookAuthors");
        await _fixture.CleanupTableAsync("Books");
        await _fixture.CleanupTableAsync("Members");
        await _fixture.CleanupTableAsync("Authors");
        await _fixture.CleanupTableAsync("Categories");
    }

    private async Task<Category> CreateTestCategoryAsync()
    {
        var categoryRepo = new CategoryRepository();
        var category = new Category("Test Category", "Test Description");
        return await _fixture.WithTransactionAsync(tx => categoryRepo.CreateAsync(category, tx));
    }

    private async Task<Book> CreateTestBookAsync(int categoryId, int availableCopies = 5)
    {
        var book = new Book(
            isbn: $"978{Random.Shared.Next(1000000000, 2000000000)}",
            title: $"Test Book {Guid.NewGuid():N}",
            categoryId: categoryId,
            totalCopies: availableCopies);

        return await _fixture.WithTransactionAsync(tx => _bookRepository.CreateAsync(book, tx));
    }

    private async Task<Member> CreateTestMemberAsync()
    {
        var member = new Member(
            membershipNumber: $"MEM{Random.Shared.Next(10000, 99999)}",
            firstName: "Test",
            lastName: "Member",
            email: $"test{Guid.NewGuid():N}@example.com",
            dateOfBirth: DateTime.UtcNow.AddYears(-30));

        return await _fixture.WithTransactionAsync(tx => _memberRepository.CreateAsync(member, tx));
    }

    #endregion
}
