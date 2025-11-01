namespace DbDemo.ConsoleApp.Models;

/// <summary>
/// DTO representing monthly loan trends from vw_MonthlyLoanTrends view.
/// Demonstrates window function usage: LAG, LEAD, moving averages.
/// </summary>
public class MonthlyLoanTrend
{
    /// <summary>
    /// Category ID.
    /// </summary>
    public int CategoryId { get; init; }

    /// <summary>
    /// Category name.
    /// </summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>
    /// Year of the loan period.
    /// </summary>
    public int Year { get; init; }

    /// <summary>
    /// Month of the loan period (1-12).
    /// </summary>
    public int Month { get; init; }

    /// <summary>
    /// Year-month formatted as yyyy-MM (e.g., "2024-10").
    /// </summary>
    public string YearMonth { get; init; } = string.Empty;

    /// <summary>
    /// Number of loans in this month for this category.
    /// </summary>
    public int LoanCount { get; init; }

    /// <summary>
    /// LAG: Loan count from previous month (same category).
    /// NULL if this is the first month with data.
    /// </summary>
    public int? PrevMonthLoans { get; init; }

    /// <summary>
    /// LEAD: Loan count from next month (same category).
    /// NULL if this is the last month with data.
    /// </summary>
    public int? NextMonthLoans { get; init; }

    /// <summary>
    /// Month-over-month growth percentage.
    /// Calculated as: ((Current - Previous) / Previous) * 100
    /// NULL if no previous month data available.
    /// </summary>
    public decimal? GrowthPercentage { get; init; }

    /// <summary>
    /// Three-month moving average (current + previous 2 months).
    /// Smooths out short-term fluctuations to identify trends.
    /// </summary>
    public decimal ThreeMonthMovingAvg { get; init; }

    /// <summary>
    /// Creates a MonthlyLoanTrend instance from database reader results.
    /// </summary>
    internal static MonthlyLoanTrend FromDatabase(
        int categoryId,
        string categoryName,
        int year,
        int month,
        string yearMonth,
        int loanCount,
        int? prevMonthLoans,
        int? nextMonthLoans,
        decimal? growthPercentage,
        decimal threeMonthMovingAvg)
    {
        return new MonthlyLoanTrend
        {
            CategoryId = categoryId,
            CategoryName = categoryName,
            Year = year,
            Month = month,
            YearMonth = yearMonth,
            LoanCount = loanCount,
            PrevMonthLoans = prevMonthLoans,
            NextMonthLoans = nextMonthLoans,
            GrowthPercentage = growthPercentage,
            ThreeMonthMovingAvg = threeMonthMovingAvg
        };
    }

    /// <summary>
    /// Returns a formatted summary of the monthly trend.
    /// </summary>
    public override string ToString()
    {
        var growthIndicator = GrowthPercentage.HasValue
            ? GrowthPercentage.Value >= 0
                ? $"↑ {GrowthPercentage.Value:+0.0}%"
                : $"↓ {GrowthPercentage.Value:0.0}%"
            : "—";

        return $"{YearMonth} - {CategoryName}: {LoanCount} loans {growthIndicator}";
    }

    /// <summary>
    /// Returns a detailed multi-line description with all trend information.
    /// </summary>
    public string ToDetailedString()
    {
        return $@"Monthly Loan Trend
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Period: {YearMonth} ({GetMonthName(Month)} {Year})
Category: {CategoryName}

Current Month: {LoanCount} loans
Previous Month: {(PrevMonthLoans.HasValue ? $"{PrevMonthLoans.Value} loans" : "N/A")}
Next Month: {(NextMonthLoans.HasValue ? $"{NextMonthLoans.Value} loans" : "N/A")}

Growth: {(GrowthPercentage.HasValue ? $"{GrowthPercentage.Value:+0.00}%" : "N/A")}
3-Month Moving Avg: {ThreeMonthMovingAvg:F2} loans";
    }

    /// <summary>
    /// Indicates whether this month shows growth compared to previous month.
    /// </summary>
    public bool IsGrowing => GrowthPercentage.HasValue && GrowthPercentage.Value > 0;

    /// <summary>
    /// Indicates whether this month shows decline compared to previous month.
    /// </summary>
    public bool IsDeclining => GrowthPercentage.HasValue && GrowthPercentage.Value < 0;

    /// <summary>
    /// Indicates whether this month shows strong growth (>20%).
    /// </summary>
    public bool IsStrongGrowth => GrowthPercentage.HasValue && GrowthPercentage.Value >= 20;

    /// <summary>
    /// Indicates whether current loan count exceeds the moving average (indicating upward trend).
    /// </summary>
    public bool IsAboveTrend => LoanCount > ThreeMonthMovingAvg;

    /// <summary>
    /// Helper method to get month name from month number.
    /// </summary>
    private static string GetMonthName(int month)
    {
        return month switch
        {
            1 => "January",
            2 => "February",
            3 => "March",
            4 => "April",
            5 => "May",
            6 => "June",
            7 => "July",
            8 => "August",
            9 => "September",
            10 => "October",
            11 => "November",
            12 => "December",
            _ => "Unknown"
        };
    }
}
