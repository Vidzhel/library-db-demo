namespace DbDemo.ConsoleApp.Models;

/// <summary>
/// DTO representing unpivoted (normalized) loan statistics
/// Each row represents one month-category combination with its loan count
/// Created from vw_UnpivotedLoanStats UNPIVOT view
/// </summary>
public class UnpivotedLoanStat
{
    public string YearMonth { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public int LoanCount { get; init; }

    /// <summary>
    /// Factory method to create instance from database query results
    /// </summary>
    public static UnpivotedLoanStat FromDatabase(
        string yearMonth,
        string categoryName,
        int loanCount)
    {
        return new UnpivotedLoanStat
        {
            YearMonth = yearMonth,
            CategoryName = categoryName,
            LoanCount = loanCount
        };
    }

    /// <summary>
    /// Gets year from YearMonth string (format: "yyyy-MM")
    /// </summary>
    public int GetYear()
    {
        if (string.IsNullOrEmpty(YearMonth) || YearMonth.Length < 4)
            return 0;
        return int.TryParse(YearMonth[..4], out var year) ? year : 0;
    }

    /// <summary>
    /// Gets month from YearMonth string (format: "yyyy-MM")
    /// </summary>
    public int GetMonth()
    {
        if (string.IsNullOrEmpty(YearMonth) || YearMonth.Length < 7)
            return 0;
        return int.TryParse(YearMonth[5..7], out var month) ? month : 0;
    }

    /// <summary>
    /// Human-readable display format
    /// </summary>
    public override string ToString()
    {
        return $"{YearMonth} | {CategoryName}: {LoanCount} loans";
    }
}
