using DbDemo.Application.Repositories;
using DbDemo.Infrastructure.Repositories;
using DbDemo.Domain.Entities;
using DbDemo.Application.DTOs;
using Xunit;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Integration tests for window function views (vw_PopularBooks, vw_MonthlyLoanTrends)
/// Tests that window functions (ROW_NUMBER, RANK, DENSE_RANK, LAG, LEAD) work correctly
/// </summary>
[Collection("Database")]
public class WindowFunctionTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly ReportRepository _reportRepository;
    private readonly BookRepository _bookRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly MemberRepository _memberRepository;
    private readonly LoanRepository _loanRepository;

    public WindowFunctionTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _reportRepository = new ReportRepository();
        _bookRepository = new BookRepository();
        _categoryRepository = new CategoryRepository();
        _memberRepository = new MemberRepository();
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
    public async Task GetPopularBooksAsync_WithNoLoans_ShouldReturnBooksWithZeroLoans()
    {
        // Arrange
        var category = await CreateTestCategory("Fiction");
        await CreateTestBooks(category.Id, 3);

        // Act
        var popularBooks = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetPopularBooksAsync(null, null, tx));

        // Assert
        Assert.NotEmpty(popularBooks);
        Assert.All(popularBooks, book => Assert.Equal(0, book.TotalLoans));
    }

    [Fact]
    public async Task GetPopularBooksAsync_RowNumber_ShouldBeUniqueAndSequential()
    {
        // Arrange
        var category = await CreateTestCategory("SciFi");
        var books = await CreateTestBooks(category.Id, 5);
        var member = await CreateTestMember();

        // Create loans: Book 0 gets 3 loans, Book 1 gets 2 loans, Book 2 gets 2 loans (tie), Book 3 gets 1 loan, Book 4 gets 0
        await CreateLoansForBook(member.Id, books[0].Id, 3);
        await CreateLoansForBook(member.Id, books[1].Id, 2);
        await CreateLoansForBook(member.Id, books[2].Id, 2);
        await CreateLoansForBook(member.Id, books[3].Id, 1);

        // Act
        var popularBooks = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetPopularBooksAsync(null, category.Id, tx));

        // Assert
        Assert.Equal(5, popularBooks.Count);

        // ROW_NUMBER should be unique: 1, 2, 3, 4, 5
        var rowNumbers = popularBooks.Select(b => b.RowNumber).OrderBy(r => r).ToList();
        Assert.Equal(new long[] { 1, 2, 3, 4, 5 }, rowNumbers);

        // First book should have most loans
        var topBook = popularBooks.First(b => b.RowNumber == 1);
        Assert.Equal(3, topBook.TotalLoans);
    }

    [Fact]
    public async Task GetPopularBooksAsync_Rank_ShouldHandleTiesWithGaps()
    {
        // Arrange
        var category = await CreateTestCategory("Mystery");
        var books = await CreateTestBooks(category.Id, 4);
        var member = await CreateTestMember();

        // Create tie scenario: 3 loans, 2 loans, 2 loans (tie), 1 loan
        // Expected RANK: 1, 2, 2, 4 (gap after tie)
        await CreateLoansForBook(member.Id, books[0].Id, 3);
        await CreateLoansForBook(member.Id, books[1].Id, 2);
        await CreateLoansForBook(member.Id, books[2].Id, 2);
        await CreateLoansForBook(member.Id, books[3].Id, 1);

        // Act
        var popularBooks = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetPopularBooksAsync(null, category.Id, tx));

        // Assert - sorted by RowNumber for predictable ordering
        var sorted = popularBooks.OrderBy(b => b.RowNumber).ToList();

        // Book with 3 loans: Rank 1
        Assert.Equal(1, sorted[0].Rank);

        // Books with 2 loans: Both Rank 2
        Assert.Equal(2, sorted[1].Rank);
        Assert.Equal(2, sorted[2].Rank);

        // Book with 1 loan: Rank 4 (gap because of tie at rank 2)
        Assert.Equal(4, sorted[3].Rank);
    }

    [Fact]
    public async Task GetPopularBooksAsync_DenseRank_ShouldHandleTiesWithoutGaps()
    {
        // Arrange
        var category = await CreateTestCategory("Romance");
        var books = await CreateTestBooks(category.Id, 4);
        var member = await CreateTestMember();

        // Create tie scenario: 3 loans, 2 loans, 2 loans (tie), 1 loan
        // Expected DENSE_RANK: 1, 2, 2, 3 (no gap after tie)
        await CreateLoansForBook(member.Id, books[0].Id, 3);
        await CreateLoansForBook(member.Id, books[1].Id, 2);
        await CreateLoansForBook(member.Id, books[2].Id, 2);
        await CreateLoansForBook(member.Id, books[3].Id, 1);

        // Act
        var popularBooks = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetPopularBooksAsync(null, category.Id, tx));

        // Assert - sorted by RowNumber
        var sorted = popularBooks.OrderBy(b => b.RowNumber).ToList();

        // Book with 3 loans: DenseRank 1
        Assert.Equal(1, sorted[0].DenseRank);

        // Books with 2 loans: Both DenseRank 2
        Assert.Equal(2, sorted[1].DenseRank);
        Assert.Equal(2, sorted[2].DenseRank);

        // Book with 1 loan: DenseRank 3 (no gap)
        Assert.Equal(3, sorted[3].DenseRank);
    }

    [Fact]
    public async Task GetPopularBooksAsync_TopN_ShouldFilterCorrectly()
    {
        // Arrange
        var category = await CreateTestCategory("History");
        var books = await CreateTestBooks(category.Id, 10);
        var member = await CreateTestMember();

        // Create varied loan counts
        for (int i = 0; i < books.Count; i++)
        {
            await CreateLoansForBook(member.Id, books[i].Id, 10 - i);
        }

        // Act - Get top 3
        var top3 = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetPopularBooksAsync(topN: 3, categoryId: null, tx));

        // Assert
        Assert.Equal(3, top3.Count);
        Assert.All(top3, book => Assert.True(book.RowNumber <= 3));
    }

    [Fact]
    public async Task GetTopBooksOverallAsync_ShouldReturnTopNAcrossAllCategories()
    {
        // Arrange
        var cat1 = await CreateTestCategory("Cat1");
        var cat2 = await CreateTestCategory("Cat2");
        var books1 = await CreateTestBooks(cat1.Id, 3);
        var books2 = await CreateTestBooks(cat2.Id, 3);
        var member = await CreateTestMember();

        // Cat1: 5, 3, 1 loans
        await CreateLoansForBook(member.Id, books1[0].Id, 5);
        await CreateLoansForBook(member.Id, books1[1].Id, 3);
        await CreateLoansForBook(member.Id, books1[2].Id, 1);

        // Cat2: 4, 2, 0 loans
        await CreateLoansForBook(member.Id, books2[0].Id, 4);
        await CreateLoansForBook(member.Id, books2[1].Id, 2);

        // Act - Get top 3 overall
        var top3 = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetTopBooksOverallAsync(3, tx));

        // Assert
        Assert.Equal(3, top3.Count);
        Assert.Equal(1, top3[0].GlobalRowNumber); // 5 loans
        Assert.Equal(2, top3[1].GlobalRowNumber); // 4 loans
        Assert.Equal(3, top3[2].GlobalRowNumber); // 3 loans
    }

    [Fact]
    public async Task GetMonthlyLoanTrendsAsync_WithSequentialMonths_ShouldCalculateLagAndLead()
    {
        // Arrange
        var category = await CreateTestCategory("Tech");
        var book = (await CreateTestBooks(category.Id, 1))[0];
        var member = await CreateTestMember();

        // Create loans in 3 consecutive months
        await CreateLoansInMonth(member.Id, book.Id, new DateTime(2024, 1, 15), 5);
        await CreateLoansInMonth(member.Id, book.Id, new DateTime(2024, 2, 15), 7);
        await CreateLoansInMonth(member.Id, book.Id, new DateTime(2024, 3, 15), 6);

        // Act
        var trends = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetMonthlyLoanTrendsAsync(category.Id, null, null, tx));

        // Assert
        var sorted = trends.OrderBy(t => t.Year).ThenBy(t => t.Month).ToList();
        Assert.Equal(3, sorted.Count);

        // January: no prev, next is Feb
        Assert.Null(sorted[0].PrevMonthLoans);
        Assert.Equal(7, sorted[0].NextMonthLoans);

        // February: prev is Jan, next is Mar
        Assert.Equal(5, sorted[1].PrevMonthLoans);
        Assert.Equal(6, sorted[1].NextMonthLoans);

        // March: prev is Feb, no next
        Assert.Equal(7, sorted[2].PrevMonthLoans);
        Assert.Null(sorted[2].NextMonthLoans);
    }

    [Fact]
    public async Task GetMonthlyLoanTrendsAsync_ShouldCalculateGrowthPercentage()
    {
        // Arrange
        var category = await CreateTestCategory("Business");
        var book = (await CreateTestBooks(category.Id, 1))[0];
        var member = await CreateTestMember();

        // Month 1: 10 loans, Month 2: 15 loans (50% growth)
        await CreateLoansInMonth(member.Id, book.Id, new DateTime(2024, 5, 15), 10);
        await CreateLoansInMonth(member.Id, book.Id, new DateTime(2024, 6, 15), 15);

        // Act
        var trends = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetMonthlyLoanTrendsAsync(category.Id, null, null, tx));

        // Assert
        var sorted = trends.OrderBy(t => t.Year).ThenBy(t => t.Month).ToList();

        // First month: no growth (no previous data)
        Assert.Null(sorted[0].GrowthPercentage);

        // Second month: 50% growth ((15-10)/10 * 100)
        Assert.NotNull(sorted[1].GrowthPercentage);
        Assert.Equal(50.00m, sorted[1].GrowthPercentage!.Value);
    }

    [Fact]
    public async Task GetMonthlyLoanTrendsAsync_ShouldCalculateMovingAverage()
    {
        // Arrange
        var category = await CreateTestCategory("Art");
        var book = (await CreateTestBooks(category.Id, 1))[0];
        var member = await CreateTestMember();

        // Create 4 months: 10, 20, 30, 40
        await CreateLoansInMonth(member.Id, book.Id, new DateTime(2024, 1, 1), 10);
        await CreateLoansInMonth(member.Id, book.Id, new DateTime(2024, 2, 1), 20);
        await CreateLoansInMonth(member.Id, book.Id, new DateTime(2024, 3, 1), 30);
        await CreateLoansInMonth(member.Id, book.Id, new DateTime(2024, 4, 1), 40);

        // Act
        var trends = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetMonthlyLoanTrendsAsync(category.Id, null, null, tx));

        // Assert
        var sorted = trends.OrderBy(t => t.Year).ThenBy(t => t.Month).ToList();

        // Month 1: only 1 month (10/1 = 10)
        Assert.Equal(10.00m, sorted[0].ThreeMonthMovingAvg);

        // Month 2: 2 months ((10+20)/2 = 15)
        Assert.Equal(15.00m, sorted[1].ThreeMonthMovingAvg);

        // Month 3: 3 months ((10+20+30)/3 = 20)
        Assert.Equal(20.00m, sorted[2].ThreeMonthMovingAvg);

        // Month 4: 3 months window ((20+30+40)/3 = 30)
        Assert.Equal(30.00m, sorted[3].ThreeMonthMovingAvg);
    }

    // Helper methods
    private async Task<Category> CreateTestCategory(string name)
    {
        return await _fixture.WithTransactionAsync(async tx =>
        {
            var category = new Category(name);
            return await _categoryRepository.CreateAsync(category, tx);
        });
    }

    private async Task<List<Book>> CreateTestBooks(int categoryId, int count)
    {
        return await _fixture.WithTransactionAsync(async tx =>
        {
            var books = new List<Book>();
            for (int i = 0; i < count; i++)
            {
                // Create valid 13-digit ISBN: use unique sequential number
                long baseIsbn = 1000000000000L + (categoryId * 1000) + i;
                var isbn = baseIsbn.ToString();
                var book = new Book(isbn, $"Book {i + 1} Cat {categoryId}", categoryId, 5);
                var created = await _bookRepository.CreateAsync(book, tx);
                books.Add(created);
            }
            return books;
        });
    }

    private async Task<Member> CreateTestMember()
    {
        return await _fixture.WithTransactionAsync(async tx =>
        {
            var member = new Member(
                $"MEM{Guid.NewGuid().ToString().Substring(0, 8)}",
                "Test",
                "User",
                $"test{Guid.NewGuid().ToString().Substring(0, 8)}@example.com",
                new DateTime(1990, 1, 1)
            );
            return await _memberRepository.CreateAsync(member, tx);
        });
    }

    private async Task CreateLoansForBook(int memberId, int bookId, int count)
    {
        await _fixture.WithTransactionAsync(async tx =>
        {
            for (int i = 0; i < count; i++)
            {
                var loan = Loan.Create(memberId, bookId);
                await _loanRepository.CreateAsync(loan, tx);
            }
        });
    }

    private async Task CreateLoansInMonth(int memberId, int bookId, DateTime monthDate, int count)
    {
        await _fixture.WithTransactionAsync(async tx =>
        {
            for (int i = 0; i < count; i++)
            {
                var loan = Loan.Create(memberId, bookId);
                var created = await _loanRepository.CreateAsync(loan, tx);

                // Set BorrowedAt to specific month using reflection
                var borrowedAtProperty = typeof(Loan).GetProperty("BorrowedAt",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                borrowedAtProperty?.SetValue(created, monthDate);

                await _loanRepository.UpdateAsync(created, tx);
            }
        });
    }
}
