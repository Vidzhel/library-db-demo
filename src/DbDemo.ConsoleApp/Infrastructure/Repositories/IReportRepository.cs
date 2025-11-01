using DbDemo.ConsoleApp.Models;
using Microsoft.Data.SqlClient;

namespace DbDemo.ConsoleApp.Infrastructure.Repositories;

/// <summary>
/// Repository interface for reporting and analytics operations using window functions
/// Provides access to views that demonstrate advanced SQL analytics capabilities
/// All methods require an active SqlTransaction to ensure proper transaction management
/// </summary>
public interface IReportRepository
{
    /// <summary>
    /// Retrieves popular books with ranking information
    /// Uses vw_PopularBooks which demonstrates ROW_NUMBER, RANK, DENSE_RANK
    /// </summary>
    /// <param name="topN">Optional: Return only top N books per category (by RowNumber)</param>
    /// <param name="categoryId">Optional: Filter to specific category</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of popular books with ranking information</returns>
    Task<List<PopularBook>> GetPopularBooksAsync(
        int? topN,
        int? categoryId,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves monthly loan trends with LAG/LEAD analysis
    /// Uses vw_MonthlyLoanTrends which demonstrates LAG, LEAD, moving averages
    /// </summary>
    /// <param name="categoryId">Optional: Filter to specific category</param>
    /// <param name="startDate">Optional: Filter trends from this date onwards</param>
    /// <param name="endDate">Optional: Filter trends up to this date</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of monthly loan trends</returns>
    Task<List<MonthlyLoanTrend>> GetMonthlyLoanTrendsAsync(
        int? categoryId,
        DateTime? startDate,
        DateTime? endDate,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the top books overall (across all categories)
    /// Uses vw_TopBooksOverall for global rankings
    /// </summary>
    /// <param name="topN">Number of top books to retrieve</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of top books ordered by global ranking</returns>
    Task<List<PopularBook>> GetTopBooksOverallAsync(
        int topN,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves monthly loan statistics with categories pivoted as columns
    /// Uses vw_MonthlyLoansByCategory which demonstrates PIVOT operator
    /// </summary>
    /// <param name="year">Optional: Filter to specific year</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of monthly loan statistics with pivoted categories</returns>
    Task<List<MonthlyLoanPivot>> GetMonthlyLoansPivotAsync(
        int? year,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves unpivoted (normalized) loan statistics
    /// Uses vw_UnpivotedLoanStats which demonstrates UNPIVOT operator
    /// </summary>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of loan statistics in normalized row format</returns>
    Task<List<UnpivotedLoanStat>> GetUnpivotedLoanStatsAsync(
        SqlTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves library statistics using #TempTable approach
    /// Calls sp_GetLibraryStatsWithTempTable stored procedure
    /// Best for: Large datasets, multiple operations, complex transformations
    /// </summary>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of library statistics by category</returns>
    Task<List<LibraryStatistics>> GetLibraryStatsWithTempTableAsync(
        SqlTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves library statistics using @TableVariable approach
    /// Calls sp_GetLibraryStatsWithTableVariable stored procedure
    /// Best for: Small datasets, single operation, simple logic
    /// </summary>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of library statistics by category</returns>
    Task<List<LibraryStatistics>> GetLibraryStatsWithTableVariableAsync(
        SqlTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves library statistics using CTE approach
    /// Calls sp_GetLibraryStatsWithCTE stored procedure
    /// Best for: Single-use queries, inline calculations, query optimization
    /// </summary>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of library statistics by category</returns>
    Task<List<LibraryStatistics>> GetLibraryStatsWithCTEAsync(
        SqlTransaction transaction,
        CancellationToken cancellationToken = default);
}
