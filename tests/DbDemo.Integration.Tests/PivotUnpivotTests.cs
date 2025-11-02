using DbDemo.Application.Repositories;
using DbDemo.Infrastructure.Repositories;
using DbDemo.Domain.Entities;
using DbDemo.Application.DTOs;
using Xunit;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Integration tests for PIVOT and UNPIVOT SQL operations
/// Tests vw_MonthlyLoansByCategory (PIVOT) and vw_UnpivotedLoanStats (UNPIVOT)
/// </summary>
[Collection("Database")]
public class PivotUnpivotTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly ReportRepository _reportRepository;
    private readonly LoanRepository _loanRepository;
    private readonly BookRepository _bookRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly MemberRepository _memberRepository;

    public PivotUnpivotTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _reportRepository = new ReportRepository();
        _loanRepository = new LoanRepository();
        _bookRepository = new BookRepository();
        _categoryRepository = new CategoryRepository();
        _memberRepository = new MemberRepository();
    }

    public async Task InitializeAsync()
    {
        // Cleanup before each test
        await _fixture.CleanupTableAsync("Loans");
        await _fixture.CleanupTableAsync("Books");
        await _fixture.CleanupTableAsync("Categories");
        await _fixture.CleanupTableAsync("Members");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetMonthlyLoansPivot_WithMultipleCategories_ShouldPivotCorrectly()
    {
        // Arrange
        await CreateTestDataWithMultipleCategories();

        // Act
        var pivots = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetMonthlyLoansPivotAsync(null, tx));

        // Assert
        Assert.NotEmpty(pivots);

        // Check that we have data for the month we created loans in
        var currentMonth = pivots.FirstOrDefault(p => p.Year == DateTime.UtcNow.Year && p.Month == DateTime.UtcNow.Month);
        Assert.NotNull(currentMonth);

        // Verify that categories are pivoted as separate properties
        Assert.True(currentMonth.CategoryLoans.ContainsKey("Fiction"));
        Assert.True(currentMonth.CategoryLoans.ContainsKey("Technology"));

        // Verify total loans matches sum of categories
        var categorySum = currentMonth.CategoryLoans.Values.Sum();
        Assert.Equal(currentMonth.TotalLoans, categorySum);
    }

    [Fact]
    public async Task GetMonthlyLoansPivot_EmptyData_ShouldReturnEmpty()
    {
        // No test data created

        // Act
        var pivots = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetMonthlyLoansPivotAsync(null, tx));

        // Assert
        Assert.Empty(pivots);
    }

    [Fact]
    public async Task GetMonthlyLoansPivot_SingleCategory_ShouldShowOneColumnPopulated()
    {
        // Arrange
        await CreateTestDataWithSingleCategory("Fiction", 5);

        // Act
        var pivots = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetMonthlyLoansPivotAsync(DateTime.UtcNow.Year, tx));

        // Assert
        Assert.Single(pivots);
        var pivot = pivots[0];

        // Only Fiction should have loans
        Assert.True(pivot.GetCategoryLoanCount("Fiction") > 0);
        Assert.Equal(0, pivot.GetCategoryLoanCount("Science"));
        Assert.Equal(0, pivot.GetCategoryLoanCount("Technology"));

        // Total should equal Fiction count
        Assert.Equal(pivot.GetCategoryLoanCount("Fiction"), pivot.TotalLoans);
    }

    [Fact]
    public async Task GetUnpivotedLoanStats_ShouldConvertColumnsToRows()
    {
        // Arrange
        await CreateTestDataWithMultipleCategories();

        // Act
        var unpivoted = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetUnpivotedLoanStatsAsync(tx));

        // Assert
        Assert.NotEmpty(unpivoted);

        // Each category-month combination should be a separate row
        var yearMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var currentMonthStats = unpivoted.Where(s => s.YearMonth == yearMonth).ToList();

        Assert.NotEmpty(currentMonthStats);

        // Verify we have separate rows for each category
        var fictionStat = currentMonthStats.FirstOrDefault(s => s.CategoryName == "Fiction");
        var techStat = currentMonthStats.FirstOrDefault(s => s.CategoryName == "Technology");

        Assert.NotNull(fictionStat);
        Assert.NotNull(techStat);

        // Each row should have its loan count
        Assert.True(fictionStat.LoanCount > 0);
        Assert.True(techStat.LoanCount > 0);
    }

    [Fact]
    public async Task PivotAndUnpivot_ShouldBeReversible()
    {
        // Arrange
        await CreateTestDataWithMultipleCategories();

        // Act
        var pivots = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetMonthlyLoansPivotAsync(DateTime.UtcNow.Year, tx));

        var unpivoted = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetUnpivotedLoanStatsAsync(tx));

        // Assert
        var yearMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var currentMonthPivot = pivots.FirstOrDefault(p => p.YearMonth == yearMonth);
        var currentMonthUnpivoted = unpivoted.Where(u => u.YearMonth == yearMonth).ToList();

        Assert.NotNull(currentMonthPivot);
        Assert.NotEmpty(currentMonthUnpivoted);

        // Sum of unpivoted rows should equal total in pivot
        var unpivotedTotal = currentMonthUnpivoted.Sum(u => u.LoanCount);
        Assert.Equal(currentMonthPivot.TotalLoans, unpivotedTotal);

        // Each unpivoted row should match corresponding pivot column
        foreach (var stat in currentMonthUnpivoted)
        {
            var pivotCount = currentMonthPivot.GetCategoryLoanCount(stat.CategoryName);
            Assert.Equal(stat.LoanCount, pivotCount);
        }
    }

    // Helper methods
    private async Task CreateTestDataWithMultipleCategories()
    {
        await _fixture.WithTransactionAsync(async tx =>
        {
            // Create categories
            var fiction = await _categoryRepository.CreateAsync(new Category("Fiction"), tx);
            var tech = await _categoryRepository.CreateAsync(new Category("Technology"), tx);
            var science = await _categoryRepository.CreateAsync(new Category("Science"), tx);

            // Create books in different categories
            var book1 = await _bookRepository.CreateAsync(
                new Book("1111111111111", "Fiction Book 1", fiction.Id, 5), tx);
            var book2 = await _bookRepository.CreateAsync(
                new Book("2222222222222", "Tech Book 1", tech.Id, 5), tx);
            var book3 = await _bookRepository.CreateAsync(
                new Book("3333333333333", "Science Book 1", science.Id, 5), tx);

            // Create member
            var member = await _memberRepository.CreateAsync(
                new Member(
                    "MEM001",
                    "John",
                    "Doe",
                    "john.doe@example.com",
                    new DateTime(1990, 1, 1)
                ), tx);

            // Create loans in current month for different categories
            var loan1 = Loan.Create(member.Id, book1.Id);
            var loan2 = Loan.Create(member.Id, book2.Id);
            var loan3 = Loan.Create(member.Id, book3.Id);

            await _loanRepository.CreateAsync(loan1, tx);
            await _loanRepository.CreateAsync(loan2, tx);
            await _loanRepository.CreateAsync(loan3, tx);
        });
    }

    private async Task CreateTestDataWithSingleCategory(string categoryName, int loanCount)
    {
        await _fixture.WithTransactionAsync(async tx =>
        {
            // Create single category
            var category = await _categoryRepository.CreateAsync(new Category(categoryName), tx);

            // Create member
            var member = await _memberRepository.CreateAsync(
                new Member(
                    "MEM002",
                    "Jane",
                    "Smith",
                    "jane.smith@example.com",
                    new DateTime(1985, 5, 15)
                ), tx);

            // Create multiple books and loans in this category
            for (int i = 0; i < loanCount; i++)
            {
                var book = await _bookRepository.CreateAsync(
                    new Book(
                        $"{i:D13}",
                        $"{categoryName} Book {i + 1}",
                        category.Id,
                        5
                    ),
                    tx
                );

                var loan = Loan.Create(member.Id, book.Id);
                await _loanRepository.CreateAsync(loan, tx);
            }
        });
    }
}
