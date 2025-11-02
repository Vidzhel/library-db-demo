namespace DbDemo.Application.DTOs;

using DbDemo.Domain.Entities;

/// <summary>
/// DTO for results from sp_GetDashboardSummary
/// Simplified dashboard statistics using ROLLUP
/// Provides key metrics at category and year aggregation levels
/// </summary>
public class DashboardSummary
{
    public string? CategoryName { get; init; }
    public int? LoanYear { get; init; }
    public int TotalLoans { get; init; }
    public int UniqueMembers { get; init; }
    public int UniqueBooks { get; init; }
    public int ActiveLoans { get; init; }
    public int ReturnedLoans { get; init; }
    public int OverdueLoans { get; init; }
    public int AvgLoanDurationDays { get; init; }
    public int IsCategoryGrouped { get; init; }
    public int IsYearGrouped { get; init; }

    /// <summary>
    /// Determines the aggregation level based on GROUPING() values
    /// </summary>
    public string AggregationLevel
    {
        get
        {
            if (IsCategoryGrouped == 1 && IsYearGrouped == 1)
                return "Grand Total";

            if (IsCategoryGrouped == 0 && IsYearGrouped == 1)
                return "Category Subtotal";

            if (IsCategoryGrouped == 0 && IsYearGrouped == 0)
                return "Category-Year Detail";

            return "Unknown";
        }
    }

    /// <summary>
    /// Percentage of active loans out of total loans
    /// </summary>
    public decimal ActiveLoanPercentage
    {
        get
        {
            if (TotalLoans == 0) return 0;
            return Math.Round((decimal)ActiveLoans / TotalLoans * 100, 2);
        }
    }

    /// <summary>
    /// Percentage of returned loans out of total loans
    /// </summary>
    public decimal ReturnedLoanPercentage
    {
        get
        {
            if (TotalLoans == 0) return 0;
            return Math.Round((decimal)ReturnedLoans / TotalLoans * 100, 2);
        }
    }

    /// <summary>
    /// Percentage of overdue loans out of total loans
    /// </summary>
    public decimal OverdueLoanPercentage
    {
        get
        {
            if (TotalLoans == 0) return 0;
            return Math.Round((decimal)OverdueLoans / TotalLoans * 100, 2);
        }
    }

    /// <summary>
    /// Average loans per unique member
    /// </summary>
    public decimal AvgLoansPerMember
    {
        get
        {
            if (UniqueMembers == 0) return 0;
            return Math.Round((decimal)TotalLoans / UniqueMembers, 2);
        }
    }

    /// <summary>
    /// Average loans per unique book
    /// </summary>
    public decimal AvgLoansPerBook
    {
        get
        {
            if (UniqueBooks == 0) return 0;
            return Math.Round((decimal)TotalLoans / UniqueBooks, 2);
        }
    }

    /// <summary>
    /// Checks if this is a grand total row
    /// </summary>
    public bool IsGrandTotal => IsCategoryGrouped == 1 && IsYearGrouped == 1;

    /// <summary>
    /// Checks if this is a category subtotal
    /// </summary>
    public bool IsCategorySubtotal => IsCategoryGrouped == 0 && IsYearGrouped == 1;

    /// <summary>
    /// Checks if this is detail level
    /// </summary>
    public bool IsDetail => IsCategoryGrouped == 0 && IsYearGrouped == 0;

    /// <summary>
    /// Factory method to create instance from database results
    /// </summary>
    public static DashboardSummary FromDatabase(
        string? categoryName,
        int? loanYear,
        int totalLoans,
        int uniqueMembers,
        int uniqueBooks,
        int activeLoans,
        int returnedLoans,
        int overdueLoans,
        int avgLoanDurationDays,
        int isCategoryGrouped,
        int isYearGrouped)
    {
        return new DashboardSummary
        {
            CategoryName = categoryName,
            LoanYear = loanYear,
            TotalLoans = totalLoans,
            UniqueMembers = uniqueMembers,
            UniqueBooks = uniqueBooks,
            ActiveLoans = activeLoans,
            ReturnedLoans = returnedLoans,
            OverdueLoans = overdueLoans,
            AvgLoanDurationDays = avgLoanDurationDays,
            IsCategoryGrouped = isCategoryGrouped,
            IsYearGrouped = isYearGrouped
        };
    }

    public override string ToString()
    {
        var categoryDisplay = CategoryName ?? "(All Categories)";
        var yearDisplay = LoanYear?.ToString() ?? "(All Years)";

        return $"[{AggregationLevel}] {categoryDisplay} / {yearDisplay}: " +
               $"{TotalLoans} total ({ActiveLoans} active, {ReturnedLoans} returned, {OverdueLoans} overdue), " +
               $"{UniqueMembers} members, {UniqueBooks} books";
    }
}
