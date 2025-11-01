using DbDemo.ConsoleApp.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DbDemo.ConsoleApp.Infrastructure.Repositories;

/// <summary>
/// ADO.NET implementation of IReportRepository
/// Demonstrates querying views with window functions for analytics and reporting
/// </summary>
public class ReportRepository : IReportRepository
{
    public async Task<List<PopularBook>> GetPopularBooksAsync(
        int? topN,
        int? categoryId,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT
                BookId,
                ISBN,
                Title,
                Subtitle,
                CategoryId,
                CategoryName,
                TotalLoans,
                RowNumber,
                Rank,
                DenseRank,
                GlobalRowNumber
            FROM dbo.vw_PopularBooks
            WHERE 1=1";

        // Add optional filters
        if (topN.HasValue)
        {
            sql += " AND RowNumber <= @TopN";
        }

        if (categoryId.HasValue)
        {
            sql += " AND CategoryId = @CategoryId";
        }

        sql += " ORDER BY CategoryName, RowNumber;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);

        if (topN.HasValue)
        {
            command.Parameters.Add("@TopN", SqlDbType.Int).Value = topN.Value;
        }

        if (categoryId.HasValue)
        {
            command.Parameters.Add("@CategoryId", SqlDbType.Int).Value = categoryId.Value;
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var books = new List<PopularBook>();
        while (await reader.ReadAsync(cancellationToken))
        {
            books.Add(MapReaderToPopularBook(reader));
        }

        return books;
    }

    public async Task<List<MonthlyLoanTrend>> GetMonthlyLoanTrendsAsync(
        int? categoryId,
        DateTime? startDate,
        DateTime? endDate,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT
                CategoryId,
                CategoryName,
                Year,
                Month,
                YearMonth,
                LoanCount,
                PrevMonthLoans,
                NextMonthLoans,
                GrowthPercentage,
                ThreeMonthMovingAvg
            FROM dbo.vw_MonthlyLoanTrends
            WHERE 1=1";

        // Add optional filters
        if (categoryId.HasValue)
        {
            sql += " AND CategoryId = @CategoryId";
        }

        if (startDate.HasValue)
        {
            sql += " AND (CAST(Year AS VARCHAR) + '-' + RIGHT('0' + CAST(Month AS VARCHAR), 2)) >= @StartDate";
        }

        if (endDate.HasValue)
        {
            sql += " AND (CAST(Year AS VARCHAR) + '-' + RIGHT('0' + CAST(Month AS VARCHAR), 2)) <= @EndDate";
        }

        sql += " ORDER BY CategoryName, Year, Month;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);

        if (categoryId.HasValue)
        {
            command.Parameters.Add("@CategoryId", SqlDbType.Int).Value = categoryId.Value;
        }

        if (startDate.HasValue)
        {
            command.Parameters.Add("@StartDate", SqlDbType.NVarChar, 7).Value = startDate.Value.ToString("yyyy-MM");
        }

        if (endDate.HasValue)
        {
            command.Parameters.Add("@EndDate", SqlDbType.NVarChar, 7).Value = endDate.Value.ToString("yyyy-MM");
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var trends = new List<MonthlyLoanTrend>();
        while (await reader.ReadAsync(cancellationToken))
        {
            trends.Add(MapReaderToMonthlyLoanTrend(reader));
        }

        return trends;
    }

    public async Task<List<PopularBook>> GetTopBooksOverallAsync(
        int topN,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        if (topN < 1)
        {
            throw new ArgumentException("Top N must be at least 1", nameof(topN));
        }

        const string sql = @"
            SELECT TOP (@TopN)
                BookId,
                ISBN,
                Title,
                Subtitle,
                CategoryId,
                CategoryName,
                TotalLoans,
                RowNumber,
                Rank,
                DenseRank,
                GlobalRowNumber
            FROM dbo.vw_TopBooksOverall
            ORDER BY GlobalRowNumber;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@TopN", SqlDbType.Int).Value = topN;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var books = new List<PopularBook>();
        while (await reader.ReadAsync(cancellationToken))
        {
            books.Add(MapReaderToPopularBook(reader));
        }

        return books;
    }

    /// <summary>
    /// Maps a SqlDataReader row to a PopularBook entity
    /// </summary>
    private static PopularBook MapReaderToPopularBook(SqlDataReader reader)
    {
        var bookId = reader.GetInt32(0);
        var isbn = reader.GetString(1);
        var title = reader.GetString(2);
        var subtitle = reader.IsDBNull(3) ? null : reader.GetString(3);
        var categoryId = reader.GetInt32(4);
        var categoryName = reader.GetString(5);
        var totalLoans = reader.GetInt32(6);
        var rowNumber = reader.GetInt64(7);
        var rank = reader.GetInt64(8);
        var denseRank = reader.GetInt64(9);
        var globalRowNumber = reader.GetInt64(10);

        return PopularBook.FromDatabase(
            bookId,
            isbn,
            title,
            subtitle,
            categoryId,
            categoryName,
            totalLoans,
            rowNumber,
            rank,
            denseRank,
            globalRowNumber
        );
    }

    /// <summary>
    /// Maps a SqlDataReader row to a MonthlyLoanTrend entity
    /// </summary>
    private static MonthlyLoanTrend MapReaderToMonthlyLoanTrend(SqlDataReader reader)
    {
        var categoryId = reader.GetInt32(0);
        var categoryName = reader.GetString(1);
        var year = reader.GetInt32(2);
        var month = reader.GetInt32(3);
        var yearMonth = reader.GetString(4);
        var loanCount = reader.GetInt32(5);
        var prevMonthLoans = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6);
        var nextMonthLoans = reader.IsDBNull(7) ? null : (int?)reader.GetInt32(7);
        var growthPercentage = reader.IsDBNull(8) ? null : (decimal?)reader.GetDecimal(8);
        var threeMonthMovingAvg = reader.GetDecimal(9);

        return MonthlyLoanTrend.FromDatabase(
            categoryId,
            categoryName,
            year,
            month,
            yearMonth,
            loanCount,
            prevMonthLoans,
            nextMonthLoans,
            growthPercentage,
            threeMonthMovingAvg
        );
    }

    public async Task<List<MonthlyLoanPivot>> GetMonthlyLoansPivotAsync(
        int? year,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT
                [Year],
                [Month],
                YearMonth,
                [Fiction],
                [Non-Fiction],
                [Science],
                [History],
                [Technology],
                [Biography],
                [Children],
                TotalLoans
            FROM dbo.vw_MonthlyLoansByCategory
            WHERE 1=1";

        if (year.HasValue)
        {
            sql += " AND [Year] = @Year";
        }

        sql += " ORDER BY [Year], [Month];";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);

        if (year.HasValue)
        {
            command.Parameters.Add("@Year", SqlDbType.Int).Value = year.Value;
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var pivots = new List<MonthlyLoanPivot>();
        while (await reader.ReadAsync(cancellationToken))
        {
            pivots.Add(MapReaderToMonthlyLoanPivot(reader));
        }

        return pivots;
    }

    public async Task<List<UnpivotedLoanStat>> GetUnpivotedLoanStatsAsync(
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                YearMonth,
                CategoryName,
                LoanCount
            FROM dbo.vw_UnpivotedLoanStats
            ORDER BY YearMonth, CategoryName;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var stats = new List<UnpivotedLoanStat>();
        while (await reader.ReadAsync(cancellationToken))
        {
            stats.Add(MapReaderToUnpivotedLoanStat(reader));
        }

        return stats;
    }

    /// <summary>
    /// Maps a SqlDataReader row to a MonthlyLoanPivot entity
    /// Handles dynamic category columns from PIVOT operation
    /// </summary>
    private static MonthlyLoanPivot MapReaderToMonthlyLoanPivot(SqlDataReader reader)
    {
        var year = reader.GetInt32(0);
        var month = reader.GetInt32(1);
        var yearMonth = reader.GetString(2);

        // Extract category loan counts from pivoted columns
        var categoryLoans = new Dictionary<string, int>
        {
            ["Fiction"] = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
            ["Non-Fiction"] = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
            ["Science"] = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
            ["History"] = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
            ["Technology"] = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
            ["Biography"] = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
            ["Children"] = reader.IsDBNull(9) ? 0 : reader.GetInt32(9)
        };

        var totalLoans = reader.GetInt32(10);

        return MonthlyLoanPivot.FromDatabase(
            year,
            month,
            yearMonth,
            categoryLoans,
            totalLoans
        );
    }

    /// <summary>
    /// Maps a SqlDataReader row to an UnpivotedLoanStat entity
    /// </summary>
    private static UnpivotedLoanStat MapReaderToUnpivotedLoanStat(SqlDataReader reader)
    {
        var yearMonth = reader.GetString(0);
        var categoryName = reader.GetString(1);
        var loanCount = reader.GetInt32(2);

        return UnpivotedLoanStat.FromDatabase(
            yearMonth,
            categoryName,
            loanCount
        );
    }

    public async Task<List<LibraryStatistics>> GetLibraryStatsWithTempTableAsync(
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = "EXEC dbo.sp_GetLibraryStatsWithTempTable;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var stats = new List<LibraryStatistics>();
        while (await reader.ReadAsync(cancellationToken))
        {
            stats.Add(MapReaderToLibraryStatistics(reader));
        }

        return stats;
    }

    public async Task<List<LibraryStatistics>> GetLibraryStatsWithTableVariableAsync(
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = "EXEC dbo.sp_GetLibraryStatsWithTableVariable;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var stats = new List<LibraryStatistics>();
        while (await reader.ReadAsync(cancellationToken))
        {
            stats.Add(MapReaderToLibraryStatistics(reader));
        }

        return stats;
    }

    public async Task<List<LibraryStatistics>> GetLibraryStatsWithCTEAsync(
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = "EXEC dbo.sp_GetLibraryStatsWithCTE;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var stats = new List<LibraryStatistics>();
        while (await reader.ReadAsync(cancellationToken))
        {
            stats.Add(MapReaderToLibraryStatistics(reader));
        }

        return stats;
    }

    /// <summary>
    /// Maps a SqlDataReader row to a LibraryStatistics entity
    /// Shared mapper for all three temp table approaches
    /// </summary>
    private static LibraryStatistics MapReaderToLibraryStatistics(SqlDataReader reader)
    {
        var categoryId = reader.GetInt32(0);
        var categoryName = reader.GetString(1);
        var totalBooks = reader.GetInt32(2);
        var totalLoans = reader.GetInt32(3);
        var activeLoans = reader.GetInt32(4);
        var averageLoansPerBook = reader.GetDecimal(5);
        var mostPopularBookTitle = reader.IsDBNull(6) ? null : reader.GetString(6);

        return LibraryStatistics.FromDatabase(
            categoryId,
            categoryName,
            totalBooks,
            totalLoans,
            activeLoans,
            averageLoansPerBook,
            mostPopularBookTitle
        );
    }

    public async Task<List<GroupingSetsResult>> GetLibraryStatsGroupingSetsAsync(
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = "EXEC dbo.sp_GetLibraryStatsGroupingSets;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GroupingSetsResult>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapReaderToGroupingSetsResult(reader));
        }

        return results;
    }

    public async Task<List<RollupResult>> GetLibraryStatsRollupAsync(
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = "EXEC dbo.sp_GetLibraryStatsRollup;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<RollupResult>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapReaderToRollupResult(reader));
        }

        return results;
    }

    public async Task<List<CubeResult>> GetLibraryStatsCubeAsync(
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = "EXEC dbo.sp_GetLibraryStatsCube;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<CubeResult>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapReaderToCubeResult(reader));
        }

        return results;
    }

    public async Task<List<DashboardSummary>> GetDashboardSummaryAsync(
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = "EXEC dbo.sp_GetDashboardSummary;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<DashboardSummary>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapReaderToDashboardSummary(reader));
        }

        return results;
    }

    /// <summary>
    /// Maps a SqlDataReader row to a GroupingSetsResult entity
    /// </summary>
    private static GroupingSetsResult MapReaderToGroupingSetsResult(SqlDataReader reader)
    {
        var categoryName = reader.IsDBNull(0) ? null : reader.GetString(0);
        var loanYear = reader.IsDBNull(1) ? null : (int?)reader.GetInt32(1);
        var loanMonth = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2);
        var totalLoans = reader.GetInt32(3);
        var uniqueMembers = reader.GetInt32(4);
        var uniqueBooksLoaned = reader.GetInt32(5);
        var isCategoryAggregated = (int)reader.GetByte(6); // GROUPING() returns BIT
        var isYearAggregated = (int)reader.GetByte(7); // GROUPING() returns BIT
        var isMonthAggregated = (int)reader.GetByte(8); // GROUPING() returns BIT

        return GroupingSetsResult.FromDatabase(
            categoryName,
            loanYear,
            loanMonth,
            totalLoans,
            uniqueMembers,
            uniqueBooksLoaned,
            isCategoryAggregated,
            isYearAggregated,
            isMonthAggregated
        );
    }

    /// <summary>
    /// Maps a SqlDataReader row to a RollupResult entity
    /// </summary>
    private static RollupResult MapReaderToRollupResult(SqlDataReader reader)
    {
        var categoryName = reader.IsDBNull(0) ? null : reader.GetString(0);
        var loanYear = reader.IsDBNull(1) ? null : (int?)reader.GetInt32(1);
        var loanMonth = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2);
        var totalLoans = reader.GetInt32(3);
        var avgLoanDurationDays = reader.GetInt32(4);
        var groupingLevel = reader.GetInt32(5);
        var aggregationLevel = reader.GetString(6);

        return RollupResult.FromDatabase(
            categoryName,
            loanYear,
            loanMonth,
            totalLoans,
            avgLoanDurationDays,
            groupingLevel,
            aggregationLevel
        );
    }

    /// <summary>
    /// Maps a SqlDataReader row to a CubeResult entity
    /// </summary>
    private static CubeResult MapReaderToCubeResult(SqlDataReader reader)
    {
        var categoryName = reader.IsDBNull(0) ? null : reader.GetString(0);
        var loanYear = reader.IsDBNull(1) ? null : (int?)reader.GetInt32(1);
        var loanStatus = reader.IsDBNull(2) ? null : reader.GetString(2);
        var totalLoans = reader.GetInt32(3);
        var isCategoryAggregated = (int)reader.GetByte(4); // GROUPING() returns BIT
        var isYearAggregated = (int)reader.GetByte(5); // GROUPING() returns BIT
        var isStatusAggregated = (int)reader.GetByte(6); // GROUPING() returns BIT
        var groupingId = reader.GetInt32(7);

        return CubeResult.FromDatabase(
            categoryName,
            loanYear,
            loanStatus,
            totalLoans,
            isCategoryAggregated,
            isYearAggregated,
            isStatusAggregated,
            groupingId
        );
    }

    /// <summary>
    /// Maps a SqlDataReader row to a DashboardSummary entity
    /// </summary>
    private static DashboardSummary MapReaderToDashboardSummary(SqlDataReader reader)
    {
        var categoryName = reader.IsDBNull(0) ? null : reader.GetString(0);
        var loanYear = reader.IsDBNull(1) ? null : (int?)reader.GetInt32(1);
        var totalLoans = reader.GetInt32(2);
        var uniqueMembers = reader.GetInt32(3);
        var uniqueBooks = reader.GetInt32(4);
        var activeLoans = reader.GetInt32(5);
        var returnedLoans = reader.GetInt32(6);
        var overdueLoans = reader.GetInt32(7);
        var avgLoanDurationDays = reader.GetInt32(8);
        var isCategoryGrouped = (int)reader.GetByte(9); // GROUPING() returns BIT
        var isYearGrouped = (int)reader.GetByte(10); // GROUPING() returns BIT

        return DashboardSummary.FromDatabase(
            categoryName,
            loanYear,
            totalLoans,
            uniqueMembers,
            uniqueBooks,
            activeLoans,
            returnedLoans,
            overdueLoans,
            avgLoanDurationDays,
            isCategoryGrouped,
            isYearGrouped
        );
    }
}
