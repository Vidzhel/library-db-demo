namespace DbDemo.Application.DTOs;

using DbDemo.Domain.Entities;

/// <summary>
/// DTO for results from sp_GetLibraryStatsRollup
/// Demonstrates ROLLUP with hierarchical aggregation
/// Includes GROUPING_ID for identifying aggregation level
/// </summary>
public class RollupResult
{
    public string? CategoryName { get; init; }
    public int? LoanYear { get; init; }
    public int? LoanMonth { get; init; }
    public int TotalLoans { get; init; }
    public int AvgLoanDurationDays { get; init; }
    public int GroupingLevel { get; init; }
    public string AggregationLevel { get; init; } = string.Empty;

    /// <summary>
    /// Checks if this is a grand total row
    /// GROUPING_ID = 7 (all three columns aggregated: 111 in binary)
    /// </summary>
    public bool IsGrandTotal => GroupingLevel == 7;

    /// <summary>
    /// Checks if this is a category subtotal (year and month aggregated)
    /// GROUPING_ID = 3 (year and month aggregated: 011 in binary)
    /// </summary>
    public bool IsCategorySubtotal => GroupingLevel == 3;

    /// <summary>
    /// Checks if this is a year subtotal (month aggregated)
    /// GROUPING_ID = 1 (month aggregated: 001 in binary)
    /// </summary>
    public bool IsYearSubtotal => GroupingLevel == 1;

    /// <summary>
    /// Checks if this is detail level (no aggregation)
    /// GROUPING_ID = 0 (nothing aggregated: 000 in binary)
    /// </summary>
    public bool IsDetail => GroupingLevel == 0;

    /// <summary>
    /// Factory method to create instance from database results
    /// </summary>
    public static RollupResult FromDatabase(
        string? categoryName,
        int? loanYear,
        int? loanMonth,
        int totalLoans,
        int avgLoanDurationDays,
        int groupingLevel,
        string aggregationLevel)
    {
        return new RollupResult
        {
            CategoryName = categoryName,
            LoanYear = loanYear,
            LoanMonth = loanMonth,
            TotalLoans = totalLoans,
            AvgLoanDurationDays = avgLoanDurationDays,
            GroupingLevel = groupingLevel,
            AggregationLevel = aggregationLevel
        };
    }

    public override string ToString()
    {
        var categoryDisplay = CategoryName ?? "(All Categories)";
        var yearDisplay = LoanYear?.ToString() ?? "(All Years)";
        var monthDisplay = LoanMonth?.ToString("D2") ?? "(All Months)";

        return $"[{AggregationLevel}] {categoryDisplay} / {yearDisplay}-{monthDisplay}: " +
               $"{TotalLoans} loans, Avg {AvgLoanDurationDays} days";
    }
}
