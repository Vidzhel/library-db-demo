namespace DbDemo.Application.DTOs;

using DbDemo.Domain.Entities;

/// <summary>
/// DTO representing monthly loan statistics with categories pivoted as columns
/// Each category becomes a separate property/column with its loan count
/// Created from vw_MonthlyLoansByCategory PIVOT view
/// </summary>
public class MonthlyLoanPivot
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string YearMonth { get; init; } = string.Empty;

    // Category loan counts - populated dynamically from PIVOT columns
    public Dictionary<string, int> CategoryLoans { get; init; } = new();

    public int TotalLoans { get; init; }

    /// <summary>
    /// Factory method to create instance from database query results
    /// </summary>
    public static MonthlyLoanPivot FromDatabase(
        int year,
        int month,
        string yearMonth,
        Dictionary<string, int> categoryLoans,
        int totalLoans)
    {
        return new MonthlyLoanPivot
        {
            Year = year,
            Month = month,
            YearMonth = yearMonth,
            CategoryLoans = categoryLoans,
            TotalLoans = totalLoans
        };
    }

    /// <summary>
    /// Gets loan count for a specific category
    /// </summary>
    public int GetCategoryLoanCount(string categoryName)
    {
        return CategoryLoans.TryGetValue(categoryName, out var count) ? count : 0;
    }

    /// <summary>
    /// Gets all categories that have loans in this period
    /// </summary>
    public IEnumerable<string> GetActiveCategories()
    {
        return CategoryLoans.Where(kvp => kvp.Value > 0).Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Calculates percentage of total loans for a specific category
    /// </summary>
    public decimal GetCategoryPercentage(string categoryName)
    {
        if (TotalLoans == 0) return 0;
        var count = GetCategoryLoanCount(categoryName);
        return Math.Round((decimal)count / TotalLoans * 100, 2);
    }

    /// <summary>
    /// Human-readable display format
    /// </summary>
    public override string ToString()
    {
        var categories = string.Join(", ", CategoryLoans.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        return $"{YearMonth} - Total: {TotalLoans} ({categories})";
    }
}
