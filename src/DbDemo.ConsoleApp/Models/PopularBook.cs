namespace DbDemo.ConsoleApp.Models;

/// <summary>
/// DTO representing a popular book entry from vw_PopularBooks view.
/// Demonstrates window function usage: ROW_NUMBER, RANK, DENSE_RANK.
/// </summary>
public class PopularBook
{
    /// <summary>
    /// The book ID.
    /// </summary>
    public int BookId { get; init; }

    /// <summary>
    /// ISBN of the book.
    /// </summary>
    public string ISBN { get; init; } = string.Empty;

    /// <summary>
    /// Book title.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Book subtitle (optional).
    /// </summary>
    public string? Subtitle { get; init; }

    /// <summary>
    /// Category ID the book belongs to.
    /// </summary>
    public int CategoryId { get; init; }

    /// <summary>
    /// Category name.
    /// </summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>
    /// Total number of times this book has been loaned.
    /// </summary>
    public int TotalLoans { get; init; }

    /// <summary>
    /// ROW_NUMBER: Unique sequential number within category (1, 2, 3, 4...).
    /// No duplicates, even for ties.
    /// </summary>
    public long RowNumber { get; init; }

    /// <summary>
    /// RANK: Ranking with gaps for ties (1, 2, 2, 4...).
    /// Books with same loan count get same rank, but next rank skips.
    /// </summary>
    public long Rank { get; init; }

    /// <summary>
    /// DENSE_RANK: Ranking without gaps (1, 2, 2, 3...).
    /// Books with same loan count get same rank, next rank is sequential.
    /// </summary>
    public long DenseRank { get; init; }

    /// <summary>
    /// Global ranking across all categories.
    /// </summary>
    public long GlobalRowNumber { get; init; }

    /// <summary>
    /// Creates a PopularBook instance from database reader results.
    /// </summary>
    internal static PopularBook FromDatabase(
        int bookId,
        string isbn,
        string title,
        string? subtitle,
        int categoryId,
        string categoryName,
        int totalLoans,
        long rowNumber,
        long rank,
        long denseRank,
        long globalRowNumber)
    {
        return new PopularBook
        {
            BookId = bookId,
            ISBN = isbn,
            Title = title,
            Subtitle = subtitle,
            CategoryId = categoryId,
            CategoryName = categoryName,
            TotalLoans = totalLoans,
            RowNumber = rowNumber,
            Rank = rank,
            DenseRank = denseRank,
            GlobalRowNumber = globalRowNumber
        };
    }

    /// <summary>
    /// Returns a formatted summary of the book's popularity.
    /// </summary>
    public override string ToString()
    {
        return $"#{GlobalRowNumber} \"{Title}\" ({CategoryName}) - {TotalLoans} loans";
    }

    /// <summary>
    /// Returns a detailed multi-line description with all ranking information.
    /// </summary>
    public string ToDetailedString()
    {
        return $@"Book: ""{Title}""{(Subtitle != null ? $" - {Subtitle}" : "")}
ISBN: {ISBN}
Category: {CategoryName}
Total Loans: {TotalLoans}
Rankings:
  • Row Number (in category): {RowNumber}
  • Rank (with gaps): {Rank}
  • Dense Rank (no gaps): {DenseRank}
  • Global Row Number: {GlobalRowNumber}";
    }

    /// <summary>
    /// Indicates whether this book is a top performer in its category (top 5).
    /// </summary>
    public bool IsTopInCategory => RowNumber <= 5;

    /// <summary>
    /// Indicates whether this book is globally popular (top 10 overall).
    /// </summary>
    public bool IsGloballyPopular => GlobalRowNumber <= 10;
}
