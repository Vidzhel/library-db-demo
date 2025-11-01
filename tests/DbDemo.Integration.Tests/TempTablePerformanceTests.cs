using DbDemo.ConsoleApp.Infrastructure.Repositories;
using DbDemo.ConsoleApp.Models;
using System.Diagnostics;
using Xunit;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Integration tests for temporary table performance comparison
/// Tests #TempTable, @TableVariable, and CTE approaches
/// Compares results and performance characteristics
/// </summary>
[Collection("Database")]
public class TempTablePerformanceTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly ReportRepository _reportRepository;
    private readonly BookRepository _bookRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly MemberRepository _memberRepository;
    private readonly LoanRepository _loanRepository;

    public TempTablePerformanceTests(DatabaseTestFixture fixture)
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
    public async Task GetLibraryStats_AllThreeMethods_ShouldReturnSameResults()
    {
        // Arrange
        await CreateTestData();

        // Act
        var tempTableResults = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetLibraryStatsWithTempTableAsync(tx));

        var tableVariableResults = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetLibraryStatsWithTableVariableAsync(tx));

        var cteResults = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetLibraryStatsWithCTEAsync(tx));

        // Assert - All three methods should return same results
        Assert.Equal(tempTableResults.Count, tableVariableResults.Count);
        Assert.Equal(tempTableResults.Count, cteResults.Count);

        for (int i = 0; i < tempTableResults.Count; i++)
        {
            var tempStat = tempTableResults[i];
            var varStat = tableVariableResults[i];
            var cteStat = cteResults[i];

            Assert.Equal(tempStat.CategoryId, varStat.CategoryId);
            Assert.Equal(tempStat.CategoryId, cteStat.CategoryId);
            Assert.Equal(tempStat.CategoryName, varStat.CategoryName);
            Assert.Equal(tempStat.TotalBooks, varStat.TotalBooks);
            Assert.Equal(tempStat.TotalLoans, varStat.TotalLoans);
            Assert.Equal(tempStat.ActiveLoans, varStat.ActiveLoans);
        }
    }

    [Fact]
    public async Task GetLibraryStatsWithTempTable_ShouldCalculateCorrectly()
    {
        // Arrange
        await CreateTestData();

        // Act
        var results = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetLibraryStatsWithTempTableAsync(tx));

        // Assert
        Assert.NotEmpty(results);

        // Verify Fiction category statistics
        var fiction = results.FirstOrDefault(s => s.CategoryName == "Fiction");
        Assert.NotNull(fiction);
        Assert.True(fiction.TotalBooks > 0);
        Assert.True(fiction.TotalLoans > 0);
        Assert.True(fiction.AverageLoansPerBook >= 0);
    }

    [Fact]
    public async Task GetLibraryStatsWithTableVariable_ShouldCalculateCorrectly()
    {
        // Arrange
        await CreateTestData();

        // Act
        var results = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetLibraryStatsWithTableVariableAsync(tx));

        // Assert
        Assert.NotEmpty(results);

        // Verify total loans across all categories
        var totalLoans = results.Sum(s => s.TotalLoans);
        Assert.True(totalLoans > 0);
    }

    [Fact]
    public async Task GetLibraryStatsWithCTE_ShouldCalculateCorrectly()
    {
        // Arrange
        await CreateTestData();

        // Act
        var results = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetLibraryStatsWithCTEAsync(tx));

        // Assert
        Assert.NotEmpty(results);

        // Verify all categories are included
        Assert.Contains(results, s => s.CategoryName == "Fiction");
        Assert.Contains(results, s => s.CategoryName == "Technology");
    }

    [Fact]
    public async Task TempTable_WithLargeDataSet_ShouldPerformWell()
    {
        // Arrange - Create larger dataset
        await CreateLargeTestData();

        // Act
        var sw = Stopwatch.StartNew();
        var results = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetLibraryStatsWithTempTableAsync(tx));
        sw.Stop();

        // Assert
        Assert.NotEmpty(results);
        // Performance check: Should complete in reasonable time (< 5 seconds)
        Assert.True(sw.ElapsedMilliseconds < 5000,
            $"Query took too long: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task ComparePerformance_AllMethods_ShouldTrackTiming()
    {
        // Arrange
        await CreateTestData();
        var comparisons = new List<PerformanceComparison>();

        // Act - Measure TempTable
        var sw = Stopwatch.StartNew();
        var tempResults = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetLibraryStatsWithTempTableAsync(tx));
        sw.Stop();
        comparisons.Add(PerformanceComparison.Create("TempTable", sw.ElapsedMilliseconds, tempResults.Count));

        // Act - Measure TableVariable
        sw.Restart();
        var varResults = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetLibraryStatsWithTableVariableAsync(tx));
        sw.Stop();
        comparisons.Add(PerformanceComparison.Create("TableVariable", sw.ElapsedMilliseconds, varResults.Count));

        // Act - Measure CTE
        sw.Restart();
        var cteResults = await _fixture.WithTransactionAsync(tx =>
            _reportRepository.GetLibraryStatsWithCTEAsync(tx));
        sw.Stop();
        comparisons.Add(PerformanceComparison.Create("CTE", sw.ElapsedMilliseconds, cteResults.Count));

        // Assert - All methods completed and returned same row counts
        Assert.Equal(3, comparisons.Count);
        Assert.All(comparisons, c => Assert.True(c.ExecutionTimeMs >= 0));
        Assert.All(comparisons, c => Assert.True(c.RowsProcessed > 0));
        Assert.True(comparisons.All(c => c.RowsProcessed == comparisons[0].RowsProcessed));

        // Output performance comparison (for informational purposes)
        foreach (var comparison in comparisons.OrderBy(c => c.ExecutionTimeMs))
        {
            // Console.WriteLine($"{comparison.MethodName}: {comparison.ExecutionTimeMs}ms ({comparison.Throughput:F2} rows/sec)");
        }
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

            // Create books in each category
            var book1 = await _bookRepository.CreateAsync(new Book("1000000000001", "Fiction Book 1", fiction.Id, 5), tx);
            var book2 = await _bookRepository.CreateAsync(new Book("1000000000002", "Tech Book 1", tech.Id, 3), tx);
            var book3 = await _bookRepository.CreateAsync(new Book("1000000000003", "Science Book 1", science.Id, 4), tx);

            // Create member
            var member = await _memberRepository.CreateAsync(
                new Member("MEM001", "John", "Doe", "john@example.com", new DateTime(1990, 1, 1)), tx);

            // Create loans
            var loan1 = Loan.Create(member.Id, book1.Id);
            var loan2 = Loan.Create(member.Id, book2.Id);
            var loan3 = Loan.Create(member.Id, book3.Id);

            await _loanRepository.CreateAsync(loan1, tx);
            await _loanRepository.CreateAsync(loan2, tx);
            await _loanRepository.CreateAsync(loan3, tx);
        });
    }

    private async Task CreateLargeTestData()
    {
        await _fixture.WithTransactionAsync(async tx =>
        {
            // Create multiple categories
            var categories = new List<Category>();
            for (int i = 0; i < 5; i++)
            {
                categories.Add(await _categoryRepository.CreateAsync(
                    new Category($"Category{i}"), tx));
            }

            // Create multiple members
            var members = new List<Member>();
            for (int i = 0; i < 3; i++)
            {
                members.Add(await _memberRepository.CreateAsync(
                    new Member($"MEM{i:D3}", $"First{i}", $"Last{i}",
                        $"user{i}@example.com", new DateTime(1990 + i, 1, 1)), tx));
            }

            // Create books and loans (10 books per category)
            long isbnBase = 2000000000000L;
            foreach (var category in categories)
            {
                for (int bookNum = 0; bookNum < 10; bookNum++)
                {
                    var isbn = (isbnBase + category.Id * 1000 + bookNum).ToString();
                    var book = await _bookRepository.CreateAsync(
                        new Book(isbn, $"Book {bookNum} in {category.Name}", category.Id, 5), tx);

                    // Create 1-3 loans for each book
                    var loanCount = (bookNum % 3) + 1;
                    for (int loanNum = 0; loanNum < loanCount; loanNum++)
                    {
                        var member = members[loanNum % members.Count];
                        var loan = Loan.Create(member.Id, book.Id);
                        await _loanRepository.CreateAsync(loan, tx);
                    }
                }
            }
        });
    }
}
