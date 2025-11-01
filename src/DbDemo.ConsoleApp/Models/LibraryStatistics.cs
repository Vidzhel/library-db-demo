namespace DbDemo.ConsoleApp.Models;

/// <summary>
/// DTO representing comprehensive library statistics by category
/// Returned by temp table, table variable, and CTE stored procedures
/// Used to compare performance characteristics of different SQL approaches
/// </summary>
public class LibraryStatistics
{
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int TotalBooks { get; init; }
    public int TotalLoans { get; init; }
    public int ActiveLoans { get; init; }
    public decimal AverageLoansPerBook { get; init; }
    public string? MostPopularBookTitle { get; init; }

    /// <summary>
    /// Factory method to create instance from database query results
    /// </summary>
    public static LibraryStatistics FromDatabase(
        int categoryId,
        string categoryName,
        int totalBooks,
        int totalLoans,
        int activeLoans,
        decimal averageLoansPerBook,
        string? mostPopularBookTitle)
    {
        return new LibraryStatistics
        {
            CategoryId = categoryId,
            CategoryName = categoryName,
            TotalBooks = totalBooks,
            TotalLoans = totalLoans,
            ActiveLoans = activeLoans,
            AverageLoansPerBook = averageLoansPerBook,
            MostPopularBookTitle = mostPopularBookTitle
        };
    }

    /// <summary>
    /// Gets the number of returned loans
    /// </summary>
    public int ReturnedLoans => TotalLoans - ActiveLoans;

    /// <summary>
    /// Calculates return rate percentage
    /// </summary>
    public decimal ReturnRate
    {
        get
        {
            if (TotalLoans == 0) return 0;
            return Math.Round((decimal)ReturnedLoans / TotalLoans * 100, 2);
        }
    }

    /// <summary>
    /// Checks if category has any activity
    /// </summary>
    public bool HasActivity => TotalLoans > 0;

    /// <summary>
    /// Human-readable display format
    /// </summary>
    public override string ToString()
    {
        return $"{CategoryName}: {TotalBooks} books, {TotalLoans} loans ({ActiveLoans} active), " +
               $"Avg: {AverageLoansPerBook:F2} loans/book";
    }
}
