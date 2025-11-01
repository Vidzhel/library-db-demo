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
}
