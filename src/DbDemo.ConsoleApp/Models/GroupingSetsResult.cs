namespace DbDemo.ConsoleApp.Models;

/// <summary>
/// DTO for results from sp_GetLibraryStatsGroupingSets
/// Demonstrates GROUPING SETS with multiple aggregation dimensions
/// Includes GROUPING() indicators to identify aggregation level
/// </summary>
public class GroupingSetsResult
{
    public string? CategoryName { get; init; }
    public int? LoanYear { get; init; }
    public int? LoanMonth { get; init; }
    public int TotalLoans { get; init; }
    public int UniqueMembers { get; init; }
    public int UniqueBooksLoaned { get; init; }
    public int IsCategoryAggregated { get; init; }
    public int IsYearAggregated { get; init; }
    public int IsMonthAggregated { get; init; }

    /// <summary>
    /// Determines the aggregation level based on GROUPING() values
    /// </summary>
    public string AggregationLevel
    {
        get
        {
            // All aggregated = Grand Total
            if (IsCategoryAggregated == 1 && IsYearAggregated == 1 && IsMonthAggregated == 1)
                return "Grand Total";

            // Category only (time aggregated)
            if (IsCategoryAggregated == 0 && IsYearAggregated == 1 && IsMonthAggregated == 1)
                return "Category Subtotal";

            // Time only (category aggregated, month still considered part of year)
            if (IsCategoryAggregated == 1 && IsYearAggregated == 0)
                return "Time Period Subtotal";

            // Detail level (Category + Year + Month)
            if (IsCategoryAggregated == 0 && IsYearAggregated == 0 && IsMonthAggregated == 0)
                return "Detail";

            return "Other Subtotal";
        }
    }

    /// <summary>
    /// Checks if this is a grand total row (all dimensions aggregated)
    /// </summary>
    public bool IsGrandTotal => IsCategoryAggregated == 1 && IsYearAggregated == 1 && IsMonthAggregated == 1;

    /// <summary>
    /// Checks if this is a detail row (no dimensions aggregated)
    /// </summary>
    public bool IsDetail => IsCategoryAggregated == 0 && IsYearAggregated == 0 && IsMonthAggregated == 0;

    /// <summary>
    /// Factory method to create instance from database results
    /// </summary>
    public static GroupingSetsResult FromDatabase(
        string? categoryName,
        int? loanYear,
        int? loanMonth,
        int totalLoans,
        int uniqueMembers,
        int uniqueBooksLoaned,
        int isCategoryAggregated,
        int isYearAggregated,
        int isMonthAggregated)
    {
        return new GroupingSetsResult
        {
            CategoryName = categoryName,
            LoanYear = loanYear,
            LoanMonth = loanMonth,
            TotalLoans = totalLoans,
            UniqueMembers = uniqueMembers,
            UniqueBooksLoaned = uniqueBooksLoaned,
            IsCategoryAggregated = isCategoryAggregated,
            IsYearAggregated = isYearAggregated,
            IsMonthAggregated = isMonthAggregated
        };
    }

    public override string ToString()
    {
        return $"{AggregationLevel}: {CategoryName ?? "(All)"} / " +
               $"{LoanYear?.ToString() ?? "(All)"}-{LoanMonth?.ToString("D2") ?? "(All)"} - " +
               $"{TotalLoans} loans";
    }
}
