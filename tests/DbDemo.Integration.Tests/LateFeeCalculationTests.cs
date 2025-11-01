using DbDemo.ConsoleApp.Infrastructure.Repositories;
using DbDemo.ConsoleApp.Models;
using Xunit;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Integration tests for fn_CalculateLateFee scalar function
/// Tests that the scalar function correctly calculates late fees for loans
/// </summary>
[Collection("Database")]
public class LateFeeCalculationTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly LoanRepository _loanRepository;
    private readonly BookRepository _bookRepository;
    private readonly MemberRepository _memberRepository;
    private readonly CategoryRepository _categoryRepository;

    public LateFeeCalculationTests(DatabaseTestFixture fixture)
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
    public async Task CalculateLateFeeAsync_NonExistentLoan_ShouldReturnZero()
    {
        // Arrange
        var nonExistentLoanId = 999999;

        // Act
        var fee = await _fixture.WithTransactionAsync(tx =>
            _loanRepository.CalculateLateFeeAsync(nonExistentLoanId, tx));

        // Assert
        Assert.Equal(0.00m, fee);
    }

    [Fact]
    public async Task CalculateLateFeeAsync_NotOverdueLoan_ShouldReturnZero()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        var loanId = await _fixture.WithTransactionAsync(async tx =>
        {
            var loan = Loan.Create(member.Id, book.Id);
            var created = await _loanRepository.CreateAsync(loan, tx);
            return created.Id;
        });

        // Act - Loan is not overdue (due date in future)
        var fee = await _fixture.WithTransactionAsync(tx =>
            _loanRepository.CalculateLateFeeAsync(loanId, tx));

        // Assert
        Assert.Equal(0.00m, fee);
    }

    [Fact]
    public async Task CalculateLateFeeAsync_OverdueLoanNotReturned_ShouldCalculateCorrectFee()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        var loanId = await _fixture.WithTransactionAsync(async tx =>
        {
            var loan = Loan.Create(member.Id, book.Id);
            var created = await _loanRepository.CreateAsync(loan, tx);

            // Set loan as overdue by 10 days using reflection
            SetLoanOverdue(created, daysOverdue: 10);
            await _loanRepository.UpdateAsync(created, tx);

            return created.Id;
        });

        // Act
        var fee = await _fixture.WithTransactionAsync(tx =>
            _loanRepository.CalculateLateFeeAsync(loanId, tx));

        // Assert
        var expectedFee = 10 * 0.50m; // 10 days * £0.50 per day
        Assert.Equal(expectedFee, fee);
    }

    [Fact]
    public async Task CalculateLateFeeAsync_OverdueLoanReturned_ShouldCalculateBasedOnReturnDate()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        var loanId = await _fixture.WithTransactionAsync(async tx =>
        {
            var loan = Loan.Create(member.Id, book.Id);
            var created = await _loanRepository.CreateAsync(loan, tx);

            // Set loan as overdue and returned late using reflection
            var borrowedAtProperty = typeof(Loan).GetProperty("BorrowedAt",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dueDateProperty = typeof(Loan).GetProperty("DueDate",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var returnedAtProperty = typeof(Loan).GetProperty("ReturnedAt",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var statusProperty = typeof(Loan).GetProperty("Status",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Set dates to satisfy CHECK constraint: DueDate > BorrowedAt
            // Borrowed 29 days ago, due 15 days ago, returned 5 days ago (10 days late)
            var borrowedAt = DateTime.UtcNow.AddDays(-29);
            var dueDate = DateTime.UtcNow.AddDays(-15);
            var returnDate = DateTime.UtcNow.AddDays(-5);

            borrowedAtProperty?.SetValue(created, borrowedAt);
            dueDateProperty?.SetValue(created, dueDate);
            returnedAtProperty?.SetValue(created, returnDate);
            statusProperty?.SetValue(created, LoanStatus.ReturnedLate);

            await _loanRepository.UpdateAsync(created, tx);

            return created.Id;
        });

        // Act
        var fee = await _fixture.WithTransactionAsync(tx =>
            _loanRepository.CalculateLateFeeAsync(loanId, tx));

        // Assert
        var expectedFee = 10 * 0.50m; // 10 days late (due 15 days ago, returned 5 days ago)
        Assert.Equal(expectedFee, fee);
    }

    [Theory]
    [InlineData(1, 0.50)]
    [InlineData(7, 3.50)]
    [InlineData(14, 7.00)]
    [InlineData(30, 15.00)]
    public async Task CalculateLateFeeAsync_VariousDaysOverdue_ShouldCalculateCorrectly(int daysOverdue, decimal expectedFee)
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        var loanId = await _fixture.WithTransactionAsync(async tx =>
        {
            var loan = Loan.Create(member.Id, book.Id);
            var created = await _loanRepository.CreateAsync(loan, tx);

            SetLoanOverdue(created, daysOverdue);
            await _loanRepository.UpdateAsync(created, tx);

            return created.Id;
        });

        // Act
        var fee = await _fixture.WithTransactionAsync(tx =>
            _loanRepository.CalculateLateFeeAsync(loanId, tx));

        // Assert
        Assert.Equal(expectedFee, fee);
    }

    [Fact]
    public async Task CalculateLateFeeAsync_MatchesStoredProcedureCalculation()
    {
        // Arrange
        var (member, book) = await CreateTestMemberAndBook();

        var loanId = await _fixture.WithTransactionAsync(async tx =>
        {
            var loan = Loan.Create(member.Id, book.Id);
            var created = await _loanRepository.CreateAsync(loan, tx);

            SetLoanOverdue(created, daysOverdue: 12);
            await _loanRepository.UpdateAsync(created, tx);

            return created.Id;
        });

        // Act
        var (scalarFee, storedProcFee) = await _fixture.WithTransactionAsync(async tx =>
        {
            // Calculate using scalar function
            var feeFromScalar = await _loanRepository.CalculateLateFeeAsync(loanId, tx);

            // Calculate using stored procedure
            var (loans, _) = await _loanRepository.GetOverdueLoansReportAsync(null, 0, tx);
            var feeFromProc = loans.FirstOrDefault(l => l.LoanId == loanId)?.CalculatedLateFee ?? 0m;

            return (feeFromScalar, feeFromProc);
        });

        // Assert - Both methods should calculate the same fee
        Assert.Equal(storedProcFee, scalarFee);
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
