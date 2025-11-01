using DbDemo.ConsoleApp.Infrastructure.Repositories;
using DbDemo.ConsoleApp.Models;
using Xunit;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Integration tests for sp_GetOverdueLoans stored procedure
/// Tests that the stored procedure correctly returns overdue loan reports with output parameters
/// </summary>
[Collection("Database")]
public class OverdueLoansReportTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly LoanRepository _loanRepository;
    private readonly BookRepository _bookRepository;
    private readonly MemberRepository _memberRepository;
    private readonly CategoryRepository _categoryRepository;

    public OverdueLoansReportTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _loanRepository = new LoanRepository();
        _bookRepository = new BookRepository();
        _memberRepository = new MemberRepository();
        _categoryRepository = new CategoryRepository();
    }

    public async Task InitializeAsync()
    {
        // Cleanup before each test
        await _fixture.CleanupTableAsync("Loans");
        await _fixture.CleanupTableAsync("Books");
        await _fixture.CleanupTableAsync("Members");
        await _fixture.CleanupTableAsync("Categories");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetOverdueLoansReportAsync_NoOverdueLoans_ShouldReturnEmptyList()
    {
        // Arrange - Create test data but no overdue loans
        await CreateTestMemberAndBook();

        // Act
        var (loans, totalCount) = await _fixture.WithTransactionAsync(tx =>
            _loanRepository.GetOverdueLoansReportAsync(null, 0, tx));

        // Assert
        Assert.Empty(loans);
        Assert.Equal(0, totalCount);
    }

    [Fact]
    public async Task GetOverdueLoansReportAsync_WithOverdueLoans_ShouldReturnCorrectData()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        // Create an overdue loan (due date in the past)
        await _fixture.WithTransactionAsync(async tx =>
        {
            var loan = Loan.Create(member.Id, book.Id);
            var createdLoan = await _loanRepository.CreateAsync(loan, tx);

            // Use reflection to set past due date (like existing tests do)
            var borrowedAtProperty = typeof(Loan).GetProperty("BorrowedAt",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dueDateProperty = typeof(Loan).GetProperty("DueDate",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var statusProperty = typeof(Loan).GetProperty("Status",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Set dates to satisfy CHECK constraint: DueDate > BorrowedAt
            // Borrowed 24 days ago, due 10 days ago (standard 14-day loan period)
            borrowedAtProperty?.SetValue(createdLoan, DateTime.UtcNow.AddDays(-24));
            dueDateProperty?.SetValue(createdLoan, DateTime.UtcNow.AddDays(-10));
            statusProperty?.SetValue(createdLoan, LoanStatus.Overdue);

            await _loanRepository.UpdateAsync(createdLoan, tx);
        });

        // Act
        var (loans, totalCount) = await _fixture.WithTransactionAsync(tx =>
            _loanRepository.GetOverdueLoansReportAsync(null, 0, tx));

        // Assert
        Assert.NotEmpty(loans);
        Assert.Equal(1, totalCount);
        Assert.Single(loans);

        var report = loans[0];
        Assert.Equal(member.Id, report.MemberId);
        Assert.Equal(member.FullName, report.MemberName);
        Assert.Equal(member.Email, report.MemberEmail);
        Assert.Equal(book.Id, report.BookId);
        Assert.Equal(book.ISBN, report.ISBN);
        Assert.Equal(book.Title, report.BookTitle);
        Assert.True(report.DaysOverdue >= 10, "Should be at least 10 days overdue");
    }

    [Fact]
    public async Task GetOverdueLoansReportAsync_FilterByMinDaysOverdue_ShouldExcludeLessOverdueLoans()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        // Create two books and two loans with different overdue days
        await _fixture.WithTransactionAsync(async tx =>
        {
            var book2 = new Book("978-1-234-56789-1", "Another Book", book.CategoryId, 1);
            var createdBook2 = await _bookRepository.CreateAsync(book2, tx);

            // Loan 1: 5 days overdue
            var loan1 = Loan.Create(member.Id, book.Id);
            var created1 = await _loanRepository.CreateAsync(loan1, tx);
            SetLoanOverdue(created1, daysOverdue: 5);
            await _loanRepository.UpdateAsync(created1, tx);

            // Loan 2: 15 days overdue
            var loan2 = Loan.Create(member.Id, createdBook2.Id);
            var created2 = await _loanRepository.CreateAsync(loan2, tx);
            SetLoanOverdue(created2, daysOverdue: 15);
            await _loanRepository.UpdateAsync(created2, tx);
        });

        // Act - Filter for loans overdue by at least 10 days
        var (loans, totalCount) = await _fixture.WithTransactionAsync(tx =>
            _loanRepository.GetOverdueLoansReportAsync(null, minDaysOverdue: 10, tx));

        // Assert - Should only return the 15-day overdue loan
        Assert.Single(loans);
        Assert.Equal(1, totalCount);
        Assert.True(loans[0].DaysOverdue >= 10);
    }

    [Fact]
    public async Task GetOverdueLoansReportAsync_OutputParameterMatchesResultSetCount()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        // Create multiple overdue loans
        await _fixture.WithTransactionAsync(async tx =>
        {
            for (int i = 0; i < 3; i++)
            {
                var testBook = new Book($"978-1-234-{i:D5}-0", $"Book {i}", book.CategoryId, 1);
                var createdBook = await _bookRepository.CreateAsync(testBook, tx);

                var loan = Loan.Create(member.Id, createdBook.Id);
                var created = await _loanRepository.CreateAsync(loan, tx);
                SetLoanOverdue(created, daysOverdue: 6);
                await _loanRepository.UpdateAsync(created, tx);
            }
        });

        // Act
        var (loans, totalCount) = await _fixture.WithTransactionAsync(tx =>
            _loanRepository.GetOverdueLoansReportAsync(null, 0, tx));

        // Assert - Output parameter should match result set count
        Assert.Equal(loans.Count, totalCount);
        Assert.Equal(3, totalCount);
    }

    [Fact]
    public async Task GetOverdueLoansReportAsync_ShouldOrderByMostOverdueFirst()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        // Create loans with different overdue days
        await _fixture.WithTransactionAsync(async tx =>
        {
            // 5 days overdue
            var book1 = await _bookRepository.CreateAsync(
                new Book("978-1-111-11111-1", "Book 1", book.CategoryId, 1), tx);
            var loan1 = Loan.Create(member.Id, book1.Id);
            var created1 = await _loanRepository.CreateAsync(loan1, tx);
            SetLoanOverdue(created1, daysOverdue: 5);
            await _loanRepository.UpdateAsync(created1, tx);

            // 20 days overdue (should be first)
            var book2 = await _bookRepository.CreateAsync(
                new Book("978-2-222-22222-2", "Book 2", book.CategoryId, 1), tx);
            var loan2 = Loan.Create(member.Id, book2.Id);
            var created2 = await _loanRepository.CreateAsync(loan2, tx);
            SetLoanOverdue(created2, daysOverdue: 20);
            await _loanRepository.UpdateAsync(created2, tx);

            // 10 days overdue
            var book3 = await _bookRepository.CreateAsync(
                new Book("978-3-333-33333-3", "Book 3", book.CategoryId, 1), tx);
            var loan3 = Loan.Create(member.Id, book3.Id);
            var created3 = await _loanRepository.CreateAsync(loan3, tx);
            SetLoanOverdue(created3, daysOverdue: 10);
            await _loanRepository.UpdateAsync(created3, tx);
        });

        // Act
        var (loans, _) = await _fixture.WithTransactionAsync(tx =>
            _loanRepository.GetOverdueLoansReportAsync(null, 0, tx));

        // Assert - Should be ordered by most overdue first
        Assert.Equal(3, loans.Count);
        Assert.True(loans[0].DaysOverdue >= loans[1].DaysOverdue, "First should be most overdue");
        Assert.True(loans[1].DaysOverdue >= loans[2].DaysOverdue, "Second should be more overdue than third");
    }

    [Fact]
    public async Task GetOverdueLoansReportAsync_ShouldCalculateLateFeeCorrectly()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        await _fixture.WithTransactionAsync(async tx =>
        {
            var loan = Loan.Create(member.Id, book.Id);
            var created = await _loanRepository.CreateAsync(loan, tx);
            SetLoanOverdue(created, daysOverdue: 10);
            await _loanRepository.UpdateAsync(created, tx);
        });

        // Act
        var (loans, _) = await _fixture.WithTransactionAsync(tx =>
            _loanRepository.GetOverdueLoansReportAsync(null, 0, tx));

        // Assert
        Assert.Single(loans);
        var expectedFee = 10 * 0.50m; // 10 days * £0.50 per day
        Assert.Equal(expectedFee, loans[0].CalculatedLateFee);
    }

    // Helper methods
    private async Task<(Member Member, Book Book)> CreateTestMemberAndBook()
    {
        return await _fixture.WithTransactionAsync(async tx =>
        {
            var category = new Category("Test Category");
            var createdCategory = await _categoryRepository.CreateAsync(category, tx);

            var member = new Member(
                "MEM001",
                "John",
                "Doe",
                "test@example.com",
                new DateTime(1990, 1, 1)
            );
            var createdMember = await _memberRepository.CreateAsync(member, tx);

            var book = new Book("978-0-123-45678-9", "Test Book", createdCategory.Id, 5);
            var createdBook = await _bookRepository.CreateAsync(book, tx);

            return (createdMember, createdBook);
        });
    }

    private void SetLoanOverdue(Loan loan, int daysOverdue)
    {
        // Use reflection to set private properties (like existing tests do)
        var borrowedAtProperty = typeof(Loan).GetProperty("BorrowedAt",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dueDateProperty = typeof(Loan).GetProperty("DueDate",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var statusProperty = typeof(Loan).GetProperty("Status",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Set dates to satisfy CHECK constraint: DueDate > BorrowedAt
        // Standard loan period is 14 days, so set BorrowedAt = -(14 + daysOverdue) days
        borrowedAtProperty?.SetValue(loan, DateTime.UtcNow.AddDays(-(14 + daysOverdue)));
        dueDateProperty?.SetValue(loan, DateTime.UtcNow.AddDays(-daysOverdue));
        statusProperty?.SetValue(loan, LoanStatus.Overdue);
    }
}
