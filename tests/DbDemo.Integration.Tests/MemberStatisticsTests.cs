using DbDemo.Application.Repositories;
using DbDemo.Infrastructure.Repositories;
using DbDemo.Domain.Entities;
using DbDemo.Application.DTOs;
using Xunit;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Integration tests for fn_GetMemberStatistics table-valued function
/// Tests that the TVF correctly aggregates member statistics from loan history
/// </summary>
[Collection("Database")]
public class MemberStatisticsTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly MemberRepository _memberRepository;
    private readonly BookRepository _bookRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly LoanRepository _loanRepository;

    public MemberStatisticsTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _memberRepository = new MemberRepository();
        _bookRepository = new BookRepository();
        _categoryRepository = new CategoryRepository();
        _loanRepository = new LoanRepository();
    }

    public async Task InitializeAsync()
    {
        // Cleanup before each test (respecting FK constraints)
        await _fixture.CleanupTableAsync("Loans");
        await _fixture.CleanupTableAsync("Books");
        await _fixture.CleanupTableAsync("Members");
        await _fixture.CleanupTableAsync("Categories");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetStatisticsAsync_MemberWithNoLoans_ShouldReturnZeroStatistics()
    {
        // Arrange
        var member = await CreateTestMember("MEM001", "John", "Doe");

        // Act
        var stats = await _fixture.WithTransactionAsync(tx =>
            _memberRepository.GetStatisticsAsync(member.Id, tx));

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(member.Id, stats.MemberId);
        Assert.Equal(0, stats.TotalBooksLoaned);
        Assert.Equal(0, stats.ActiveLoans);
        Assert.Equal(0, stats.OverdueLoans);
        Assert.Equal(0, stats.ReturnedLateCount);
        Assert.Equal(0.00m, stats.TotalLateFees);
        Assert.Equal(0.00m, stats.UnpaidLateFees);
        Assert.Null(stats.AvgLoanDurationDays);
        Assert.Null(stats.LastBorrowDate);
        Assert.Equal(0, stats.TotalRenewals);
        Assert.Equal(0, stats.LostOrDamagedCount);
    }

    [Fact]
    public async Task GetStatisticsAsync_MemberWithActiveLoans_ShouldCountCorrectly()
    {
        // Arrange
        var member = await CreateTestMember("MEM002", "Jane", "Smith");
        var books = await CreateTestBooks(3);

        // Create 3 active loans
        await _fixture.WithTransactionAsync(async tx =>
        {
            foreach (var book in books)
            {
                var loan = Loan.Create(member.Id, book.Id);
                await _loanRepository.CreateAsync(loan, tx);
            }
        });

        // Act
        var stats = await _fixture.WithTransactionAsync(tx =>
            _memberRepository.GetStatisticsAsync(member.Id, tx));

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(3, stats.TotalBooksLoaned);
        Assert.Equal(3, stats.ActiveLoans);
        Assert.Equal(0, stats.OverdueLoans);
        Assert.NotNull(stats.LastBorrowDate);
    }

    [Fact]
    public async Task GetStatisticsAsync_MemberWithOverdueLoans_ShouldCountOverdue()
    {
        // Arrange
        var member = await CreateTestMember("MEM003", "Bob", "Johnson");
        var books = await CreateTestBooks(5);

        // Create 2 active loans and 3 overdue loans
        await _fixture.WithTransactionAsync(async tx =>
        {
            for (int i = 0; i < 5; i++)
            {
                var loan = Loan.Create(member.Id, books[i].Id);
                var created = await _loanRepository.CreateAsync(loan, tx);

                // Make first 3 loans overdue
                if (i < 3)
                {
                    SetLoanStatus(created, LoanStatus.Overdue, daysOverdue: 5);
                    await _loanRepository.UpdateAsync(created, tx);
                }
            }
        });

        // Act
        var stats = await _fixture.WithTransactionAsync(tx =>
            _memberRepository.GetStatisticsAsync(member.Id, tx));

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(5, stats.TotalBooksLoaned);
        Assert.Equal(2, stats.ActiveLoans); // 2 still active
        Assert.Equal(3, stats.OverdueLoans); // 3 overdue
    }

    [Fact]
    public async Task GetStatisticsAsync_MemberWithReturnedLoans_ShouldCalculateAvgDuration()
    {
        // Arrange
        var member = await CreateTestMember("MEM004", "Alice", "Williams");
        var books = await CreateTestBooks(3);

        await _fixture.WithTransactionAsync(async tx =>
        {
            // Loan 1: borrowed 20 days ago, returned after 10 days
            var loan1 = Loan.Create(member.Id, books[0].Id);
            var created1 = await _loanRepository.CreateAsync(loan1, tx);
            SetReturnedLoan(created1, borrowedDaysAgo: 20, returnedDaysAgo: 10);
            await _loanRepository.UpdateAsync(created1, tx);

            // Loan 2: borrowed 30 days ago, returned after 12 days
            var loan2 = Loan.Create(member.Id, books[1].Id);
            var created2 = await _loanRepository.CreateAsync(loan2, tx);
            SetReturnedLoan(created2, borrowedDaysAgo: 30, returnedDaysAgo: 18);
            await _loanRepository.UpdateAsync(created2, tx);

            // Loan 3: currently active (should not affect average)
            var loan3 = Loan.Create(member.Id, books[2].Id);
            await _loanRepository.CreateAsync(loan3, tx);
        });

        // Act
        var stats = await _fixture.WithTransactionAsync(tx =>
            _memberRepository.GetStatisticsAsync(member.Id, tx));

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(3, stats.TotalBooksLoaned);
        Assert.Equal(1, stats.ActiveLoans);
        Assert.NotNull(stats.AvgLoanDurationDays);
        // Average of 10 and 12 days = 11 days
        Assert.Equal(11, stats.AvgLoanDurationDays!.Value);
    }

    [Fact]
    public async Task GetStatisticsAsync_MemberWithLateFees_ShouldSumCorrectly()
    {
        // Arrange
        var member = await CreateTestMember("MEM005", "Charlie", "Brown");
        var books = await CreateTestBooks(3);

        await _fixture.WithTransactionAsync(async tx =>
        {
            // Loan 1: £5.00 late fee, unpaid
            var loan1 = Loan.Create(member.Id, books[0].Id);
            var created1 = await _loanRepository.CreateAsync(loan1, tx);
            SetLateFeeLoan(created1, lateFee: 5.00m, isFeePaid: false);
            await _loanRepository.UpdateAsync(created1, tx);

            // Loan 2: £3.50 late fee, paid
            var loan2 = Loan.Create(member.Id, books[1].Id);
            var created2 = await _loanRepository.CreateAsync(loan2, tx);
            SetLateFeeLoan(created2, lateFee: 3.50m, isFeePaid: true);
            await _loanRepository.UpdateAsync(created2, tx);

            // Loan 3: £2.00 late fee, unpaid
            var loan3 = Loan.Create(member.Id, books[2].Id);
            var created3 = await _loanRepository.CreateAsync(loan3, tx);
            SetLateFeeLoan(created3, lateFee: 2.00m, isFeePaid: false);
            await _loanRepository.UpdateAsync(created3, tx);
        });

        // Act
        var stats = await _fixture.WithTransactionAsync(tx =>
            _memberRepository.GetStatisticsAsync(member.Id, tx));

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(10.50m, stats.TotalLateFees); // 5 + 3.5 + 2
        Assert.Equal(7.00m, stats.UnpaidLateFees); // 5 + 2
    }

    [Fact]
    public async Task GetStatisticsAsync_MemberWithReturnedLateLoans_ShouldCount()
    {
        // Arrange
        var member = await CreateTestMember("MEM006", "Diana", "Prince");
        var books = await CreateTestBooks(4);

        await _fixture.WithTransactionAsync(async tx =>
        {
            // 2 loans returned on time
            for (int i = 0; i < 2; i++)
            {
                var loan = Loan.Create(member.Id, books[i].Id);
                var created = await _loanRepository.CreateAsync(loan, tx);
                SetLoanStatus(created, LoanStatus.Returned, daysOverdue: 0);
                await _loanRepository.UpdateAsync(created, tx);
            }

            // 2 loans returned late
            for (int i = 2; i < 4; i++)
            {
                var loan = Loan.Create(member.Id, books[i].Id);
                var created = await _loanRepository.CreateAsync(loan, tx);
                SetLoanStatus(created, LoanStatus.ReturnedLate, daysOverdue: 5);
                await _loanRepository.UpdateAsync(created, tx);
            }
        });

        // Act
        var stats = await _fixture.WithTransactionAsync(tx =>
            _memberRepository.GetStatisticsAsync(member.Id, tx));

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(4, stats.TotalBooksLoaned);
        Assert.Equal(2, stats.ReturnedLateCount);
    }

    [Fact]
    public async Task GetStatisticsAsync_MemberWithRenewals_ShouldCountTotal()
    {
        // Arrange
        var member = await CreateTestMember("MEM007", "Eve", "Anderson");
        var books = await CreateTestBooks(3);

        await _fixture.WithTransactionAsync(async tx =>
        {
            // Loan 1: 2 renewals
            var loan1 = Loan.Create(member.Id, books[0].Id);
            var created1 = await _loanRepository.CreateAsync(loan1, tx);
            SetRenewalCount(created1, 2);
            await _loanRepository.UpdateAsync(created1, tx);

            // Loan 2: 1 renewal
            var loan2 = Loan.Create(member.Id, books[1].Id);
            var created2 = await _loanRepository.CreateAsync(loan2, tx);
            SetRenewalCount(created2, 1);
            await _loanRepository.UpdateAsync(created2, tx);

            // Loan 3: no renewals
            var loan3 = Loan.Create(member.Id, books[2].Id);
            await _loanRepository.CreateAsync(loan3, tx);
        });

        // Act
        var stats = await _fixture.WithTransactionAsync(tx =>
            _memberRepository.GetStatisticsAsync(member.Id, tx));

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(3, stats.TotalRenewals); // 2 + 1 + 0
    }

    [Fact]
    public async Task GetStatisticsAsync_MemberWithLostAndDamagedBooks_ShouldCount()
    {
        // Arrange
        var member = await CreateTestMember("MEM008", "Frank", "Miller");
        var books = await CreateTestBooks(3);

        await _fixture.WithTransactionAsync(async tx =>
        {
            // Lost book
            var loan1 = Loan.Create(member.Id, books[0].Id);
            var created1 = await _loanRepository.CreateAsync(loan1, tx);
            SetLoanStatus(created1, LoanStatus.Lost, daysOverdue: 0);
            await _loanRepository.UpdateAsync(created1, tx);

            // Damaged book
            var loan2 = Loan.Create(member.Id, books[1].Id);
            var created2 = await _loanRepository.CreateAsync(loan2, tx);
            SetLoanStatus(created2, LoanStatus.Damaged, daysOverdue: 0);
            await _loanRepository.UpdateAsync(created2, tx);

            // Normal active loan
            var loan3 = Loan.Create(member.Id, books[2].Id);
            await _loanRepository.CreateAsync(loan3, tx);
        });

        // Act
        var stats = await _fixture.WithTransactionAsync(tx =>
            _memberRepository.GetStatisticsAsync(member.Id, tx));

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(3, stats.TotalBooksLoaned);
        Assert.Equal(2, stats.LostOrDamagedCount);
    }

    [Fact]
    public async Task GetStatisticsAsync_ComplexMemberHistory_ShouldAggregateCorrectly()
    {
        // Arrange - Create a member with comprehensive loan history
        var member = await CreateTestMember("MEM009", "Grace", "Hopper");
        var books = await CreateTestBooks(10);

        await _fixture.WithTransactionAsync(async tx =>
        {
            // 2 active loans (no overdue)
            for (int i = 0; i < 2; i++)
            {
                var loan = Loan.Create(member.Id, books[i].Id);
                await _loanRepository.CreateAsync(loan, tx);
            }

            // 3 overdue loans with fees
            for (int i = 2; i < 5; i++)
            {
                var loan = Loan.Create(member.Id, books[i].Id);
                var created = await _loanRepository.CreateAsync(loan, tx);
                SetLoanStatus(created, LoanStatus.Overdue, daysOverdue: 7);
                SetLateFeeLoan(created, lateFee: 3.50m, isFeePaid: false);
                await _loanRepository.UpdateAsync(created, tx);
            }

            // 2 returned on time
            for (int i = 5; i < 7; i++)
            {
                var loan = Loan.Create(member.Id, books[i].Id);
                var created = await _loanRepository.CreateAsync(loan, tx);
                SetReturnedLoan(created, borrowedDaysAgo: 20, returnedDaysAgo: 10);
                await _loanRepository.UpdateAsync(created, tx);
            }

            // 2 returned late with paid fees
            for (int i = 7; i < 9; i++)
            {
                var loan = Loan.Create(member.Id, books[i].Id);
                var created = await _loanRepository.CreateAsync(loan, tx);
                SetLoanStatus(created, LoanStatus.ReturnedLate, daysOverdue: 5);
                SetLateFeeLoan(created, lateFee: 2.50m, isFeePaid: true);
                SetReturnedLoan(created, borrowedDaysAgo: 25, returnedDaysAgo: 5);
                await _loanRepository.UpdateAsync(created, tx);
            }

            // 1 lost book
            var lostLoan = Loan.Create(member.Id, books[9].Id);
            var createdLost = await _loanRepository.CreateAsync(lostLoan, tx);
            SetLoanStatus(createdLost, LoanStatus.Lost, daysOverdue: 0);
            await _loanRepository.UpdateAsync(createdLost, tx);
        });

        // Act
        var stats = await _fixture.WithTransactionAsync(tx =>
            _memberRepository.GetStatisticsAsync(member.Id, tx));

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(10, stats.TotalBooksLoaned);
        Assert.Equal(2, stats.ActiveLoans);
        Assert.Equal(3, stats.OverdueLoans);
        Assert.Equal(2, stats.ReturnedLateCount);
        Assert.Equal(15.50m, stats.TotalLateFees); // (3 * 3.50) + (2 * 2.50)
        Assert.Equal(10.50m, stats.UnpaidLateFees); // 3 * 3.50
        Assert.Equal(1, stats.LostOrDamagedCount);
    }

    // Helper methods
    private async Task<Member> CreateTestMember(string membershipNumber, string firstName, string lastName)
    {
        return await _fixture.WithTransactionAsync(async tx =>
        {
            var member = new Member(
                membershipNumber,
                firstName,
                lastName,
                $"{firstName.ToLower()}.{lastName.ToLower()}@example.com",
                new DateTime(1990, 1, 1)
            );
            return await _memberRepository.CreateAsync(member, tx);
        });
    }

    private async Task<List<Book>> CreateTestBooks(int count)
    {
        return await _fixture.WithTransactionAsync(async tx =>
        {
            var category = new Category("Test Category");
            var createdCategory = await _categoryRepository.CreateAsync(category, tx);

            var books = new List<Book>();
            for (int i = 0; i < count; i++)
            {
                var isbn = $"978-0-123-{i:D5}-0";
                var book = new Book(isbn, $"Test Book {i + 1}", createdCategory.Id, 5);
                var createdBook = await _bookRepository.CreateAsync(book, tx);
                books.Add(createdBook);
            }

            return books;
        });
    }

    private void SetLoanStatus(Loan loan, LoanStatus status, int daysOverdue)
    {
        var borrowedAtProperty = typeof(Loan).GetProperty("BorrowedAt",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dueDateProperty = typeof(Loan).GetProperty("DueDate",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var statusProperty = typeof(Loan).GetProperty("Status",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        statusProperty?.SetValue(loan, status);

        if (daysOverdue > 0)
        {
            // Set dates to satisfy CHECK constraint: DueDate > BorrowedAt
            // Standard loan period is 14 days, so set BorrowedAt = -(14 + daysOverdue) days
            borrowedAtProperty?.SetValue(loan, DateTime.UtcNow.AddDays(-(14 + daysOverdue)));
            dueDateProperty?.SetValue(loan, DateTime.UtcNow.AddDays(-daysOverdue));
        }
    }

    private void SetReturnedLoan(Loan loan, int borrowedDaysAgo, int returnedDaysAgo)
    {
        var borrowedAtProperty = typeof(Loan).GetProperty("BorrowedAt",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dueDateProperty = typeof(Loan).GetProperty("DueDate",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var returnedAtProperty = typeof(Loan).GetProperty("ReturnedAt",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var statusProperty = typeof(Loan).GetProperty("Status",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var borrowedAt = DateTime.UtcNow.AddDays(-borrowedDaysAgo);
        var returnedAt = DateTime.UtcNow.AddDays(-returnedDaysAgo);

        borrowedAtProperty?.SetValue(loan, borrowedAt);
        // Set DueDate to ensure CHECK constraint is satisfied (DueDate > BorrowedAt)
        // Assume 14 day loan period
        dueDateProperty?.SetValue(loan, borrowedAt.AddDays(14));
        returnedAtProperty?.SetValue(loan, returnedAt);

        // Only set status to Returned if it hasn't been explicitly set to ReturnedLate
        if (loan.Status != LoanStatus.ReturnedLate)
        {
            statusProperty?.SetValue(loan, LoanStatus.Returned);
        }
    }

    private void SetLateFeeLoan(Loan loan, decimal lateFee, bool isFeePaid)
    {
        var lateFeeProperty = typeof(Loan).GetProperty("LateFee",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var isFeePaidProperty = typeof(Loan).GetProperty("IsFeePaid",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        lateFeeProperty?.SetValue(loan, lateFee);
        isFeePaidProperty?.SetValue(loan, isFeePaid);
    }

    private void SetRenewalCount(Loan loan, int renewalCount)
    {
        var renewalCountProperty = typeof(Loan).GetProperty("RenewalCount",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        renewalCountProperty?.SetValue(loan, renewalCount);
    }
}
