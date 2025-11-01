using DbDemo.ConsoleApp.Infrastructure.Repositories;
using DbDemo.ConsoleApp.Models;
using Xunit;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Integration tests for advanced aggregations (GROUPING SETS, ROLLUP, CUBE)
/// Tests multi-dimensional reporting and hierarchical aggregation features
/// </summary>
[Collection("Database")]
public class AdvancedAggregationsTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly ReportRepository _reportRepository;
    private readonly BookRepository _bookRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly MemberRepository _memberRepository;
    private readonly LoanRepository _loanRepository;

    public AdvancedAggregationsTests(DatabaseTestFixture fixture)
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
        // Cleanup before each test
        await _fixture.CleanupTableAsync("Loans");
        await _fixture.CleanupTableAsync("Books");
        await _fixture.CleanupTableAsync("Categories");
        await _fixture.CleanupTableAsync("Members");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetLibraryStatsGroupingSets_ShouldReturnAllAggregationLevels()
    {
        // Arrange
        await CreateTestData();

        // Act
        var results = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetLibraryStatsGroupingSetsAsync(tx));

        // Assert
        Assert.NotEmpty(results);

        // Should have grand total (all aggregated)
        Assert.Contains(results, r => r.IsGrandTotal);

        // Should have category-level aggregations (time aggregated)
        Assert.Contains(results, r => r.CategoryName != null &&
                                     r.LoanYear == null &&
                                     r.LoanMonth == null);

        // Should have time-level aggregations (category aggregated)
        Assert.Contains(results, r => r.CategoryName == null &&
                                     r.LoanYear != null);

        // Should have detail level (no aggregation)
        Assert.Contains(results, r => r.IsDetail);
    }

    [Fact]
    public async Task GetLibraryStatsGroupingSets_ShouldHaveCorrectGroupingIndicators()
    {
        // Arrange
        await CreateTestData();

        // Act
        var results = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetLibraryStatsGroupingSetsAsync(tx));

        // Assert - Grand total has all dimensions aggregated
        var grandTotal = results.First(r => r.IsGrandTotal);
        Assert.Equal(1, grandTotal.IsCategoryAggregated);
        Assert.Equal(1, grandTotal.IsYearAggregated);
        Assert.Equal(1, grandTotal.IsMonthAggregated);

        // Assert - Detail has no dimensions aggregated
        var detail = results.First(r => r.IsDetail);
        Assert.Equal(0, detail.IsCategoryAggregated);
        Assert.Equal(0, detail.IsYearAggregated);
        Assert.Equal(0, detail.IsMonthAggregated);
    }

    [Fact]
    public async Task GetLibraryStatsRollup_ShouldHaveHierarchicalSubtotals()
    {
        // Arrange
        await CreateTestData();

        // Act
        var results = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetLibraryStatsRollupAsync(tx));

        // Assert
        Assert.NotEmpty(results);

        // Should have grand total (GROUPING_ID = 7)
        Assert.Contains(results, r => r.IsGrandTotal);

        // Should have category subtotals (GROUPING_ID = 3)
        Assert.Contains(results, r => r.IsCategorySubtotal);

        // Should have year subtotals (GROUPING_ID = 1)
        Assert.Contains(results, r => r.IsYearSubtotal);

        // Should have detail level (GROUPING_ID = 0)
        Assert.Contains(results, r => r.IsDetail);

        // Verify aggregation level labels are set
        Assert.Contains(results, r => r.AggregationLevel == "Grand Total");
        Assert.Contains(results, r => r.AggregationLevel == "Subtotal (Category)");
    }

    [Fact]
    public async Task GetLibraryStatsRollup_ShouldHaveCorrectGroupingIdValues()
    {
        // Arrange
        await CreateTestData();

        // Act
        var results = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetLibraryStatsRollupAsync(tx));

        // Assert - Verify GROUPING_ID values match expected levels
        var grandTotal = results.First(r => r.IsGrandTotal);
        Assert.Equal(7, grandTotal.GroupingLevel); // 111 binary = 7

        var categorySubtotal = results.First(r => r.IsCategorySubtotal);
        Assert.Equal(3, categorySubtotal.GroupingLevel); // 011 binary = 3

        var detail = results.First(r => r.IsDetail);
        Assert.Equal(0, detail.GroupingLevel); // 000 binary = 0
    }

    [Fact]
    public async Task GetLibraryStatsCube_ShouldReturnAllCombinations()
    {
        // Arrange
        await CreateTestData();

        // Act
        var results = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetLibraryStatsCubeAsync(tx));

        // Assert - CUBE of 3 dimensions should have 2^3 = 8 grouping combinations
        var distinctGroupingIds = results.Select(r => r.GroupingId).Distinct().ToList();

        // Should have multiple different GROUPING_ID values (0-7)
        Assert.True(distinctGroupingIds.Count >= 2,
            $"Expected multiple grouping combinations, got {distinctGroupingIds.Count}");

        // Should have grand total (all aggregated)
        Assert.Contains(results, r => r.IsGrandTotal);
        Assert.Contains(results, r => r.GroupingId == 7);

        // Should have detail level (none aggregated)
        Assert.Contains(results, r => r.IsDetail);
        Assert.Contains(results, r => r.GroupingId == 0);
    }

    [Fact]
    public async Task GetLibraryStatsCube_ShouldHaveCorrectGroupingIndicators()
    {
        // Arrange
        await CreateTestData();

        // Act
        var results = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetLibraryStatsCubeAsync(tx));

        // Assert - Grand total should have all dimensions aggregated
        var grandTotal = results.First(r => r.IsGrandTotal);
        Assert.Equal(1, grandTotal.IsCategoryAggregated);
        Assert.Equal(1, grandTotal.IsYearAggregated);
        Assert.Equal(1, grandTotal.IsStatusAggregated);
        Assert.Equal(3, grandTotal.AggregatedDimensionCount);

        // Assert - Detail should have no dimensions aggregated
        var detail = results.First(r => r.IsDetail);
        Assert.Equal(0, detail.IsCategoryAggregated);
        Assert.Equal(0, detail.IsYearAggregated);
        Assert.Equal(0, detail.IsStatusAggregated);
        Assert.Equal(0, detail.AggregatedDimensionCount);
    }

    [Fact]
    public async Task GetDashboardSummary_ShouldHaveCategoryAndYearSubtotals()
    {
        // Arrange
        await CreateTestData();

        // Act
        var results = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetDashboardSummaryAsync(tx));

        // Assert
        Assert.NotEmpty(results);

        // Should have grand total
        Assert.Contains(results, r => r.IsGrandTotal);

        // Should have category subtotals
        Assert.Contains(results, r => r.IsCategorySubtotal);

        // Should have detail level (category + year)
        Assert.Contains(results, r => r.IsDetail);

        // Verify aggregation level descriptions
        Assert.Contains(results, r => r.AggregationLevel == "Grand Total");
        Assert.Contains(results, r => r.AggregationLevel == "Category Subtotal");
        Assert.Contains(results, r => r.AggregationLevel == "Category-Year Detail");
    }

    [Fact]
    public async Task GetDashboardSummary_ShouldHaveGrandTotalWithAllLoans()
    {
        // Arrange
        await CreateTestData();

        // Act
        var results = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetDashboardSummaryAsync(tx));

        // Assert - Grand total should sum all loans
        var grandTotal = results.First(r => r.IsGrandTotal);

        Assert.True(grandTotal.TotalLoans > 0, "Grand total should have loans");
        Assert.True(grandTotal.UniqueMembers > 0, "Grand total should have members");
        Assert.True(grandTotal.UniqueBooks > 0, "Grand total should have books");

        // Grand total should equal or exceed any category subtotal
        var categorySubtotals = results.Where(r => r.IsCategorySubtotal).ToList();
        foreach (var subtotal in categorySubtotals)
        {
            Assert.True(grandTotal.TotalLoans >= subtotal.TotalLoans,
                $"Grand total ({grandTotal.TotalLoans}) should be >= category subtotal ({subtotal.TotalLoans})");
        }
    }

    [Fact]
    public async Task DashboardSummary_ShouldCalculatePercentagesCorrectly()
    {
        // Arrange
        await CreateTestData();

        // Act
        var results = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetDashboardSummaryAsync(tx));

        // Assert
        var grandTotal = results.First(r => r.IsGrandTotal);

        // Percentages should sum to ~100% (allowing for rounding)
        var totalPercentage = grandTotal.ActiveLoanPercentage +
                             grandTotal.ReturnedLoanPercentage +
                             grandTotal.OverdueLoanPercentage;

        Assert.True(totalPercentage >= 99 && totalPercentage <= 101,
            $"Total percentage should be ~100%, got {totalPercentage}");

        // Computed metrics should be reasonable
        Assert.True(grandTotal.AvgLoansPerMember >= 0, "Avg loans per member should be >= 0");
        Assert.True(grandTotal.AvgLoansPerBook >= 0, "Avg loans per book should be >= 0");
    }

    // Helper methods
    private async Task CreateTestData()
    {
        await _fixture.WithTransactionAsync(async tx =>
        {
            // Create categories
            var fiction = await _categoryRepository.CreateAsync(new Category("Fiction"), tx);
            var tech = await _categoryRepository.CreateAsync(new Category("Technology"), tx);
            var science = await _categoryRepository.CreateAsync(new Category("Science"), tx);

            // Create members
            var member1 = await _memberRepository.CreateAsync(
                new Member("MEM001", "Alice", "Smith", "alice@example.com", new DateTime(1990, 1, 1)), tx);
            var member2 = await _memberRepository.CreateAsync(
                new Member("MEM002", "Bob", "Jones", "bob@example.com", new DateTime(1985, 5, 15)), tx);

            // Create books
            var book1 = await _bookRepository.CreateAsync(new Book("3000000000001", "Fiction Book 1", fiction.Id, 5), tx);
            var book2 = await _bookRepository.CreateAsync(new Book("3000000000002", "Tech Book 1", tech.Id, 3), tx);
            var book3 = await _bookRepository.CreateAsync(new Book("3000000000003", "Science Book 1", science.Id, 4), tx);
            var book4 = await _bookRepository.CreateAsync(new Book("3000000000004", "Fiction Book 2", fiction.Id, 5), tx);

            // Create loans with different statuses
            var loan1 = Loan.Create(member1.Id, book1.Id);
            var loan2 = Loan.Create(member1.Id, book2.Id);
            var loan3 = Loan.Create(member2.Id, book3.Id);
            var loan4 = Loan.Create(member2.Id, book4.Id);

            await _loanRepository.CreateAsync(loan1, tx);
            await _loanRepository.CreateAsync(loan2, tx);
            await _loanRepository.CreateAsync(loan3, tx);
            await _loanRepository.CreateAsync(loan4, tx);

            // Return some loans to create varied status data
            var returnedLoan = await _loanRepository.GetByIdAsync(loan1.Id, tx);
            if (returnedLoan != null)
            {
                returnedLoan.Return();
                await _loanRepository.UpdateAsync(returnedLoan, tx);
            }
        });
    }
}
