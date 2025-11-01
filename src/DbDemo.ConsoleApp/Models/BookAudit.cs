namespace DbDemo.ConsoleApp.Models;

/// <summary>
/// Represents an audit record for changes made to the Books table.
/// This model is populated by the TR_Books_Audit database trigger.
/// </summary>
public class BookAudit
{
    public int AuditId { get; init; }
    public int BookId { get; init; }
    public string Action { get; init; } = string.Empty; // 'INSERT', 'UPDATE', 'DELETE'

    // Old values (populated for UPDATE and DELETE)
    public string? OldISBN { get; init; }
    public string? OldTitle { get; init; }
    public int? OldAvailableCopies { get; init; }
    public int? OldTotalCopies { get; init; }

    // New values (populated for INSERT and UPDATE)
    public string? NewISBN { get; init; }
    public string? NewTitle { get; init; }
    public int? NewAvailableCopies { get; init; }
    public int? NewTotalCopies { get; init; }

    // Metadata
    public DateTime ChangedAt { get; init; }
    public string ChangedBy { get; init; } = string.Empty;

    /// <summary>
    /// Creates a BookAudit instance from a database reader.
    /// </summary>
    internal static BookAudit FromDatabase(
        int auditId,
        int bookId,
        string action,
        string? oldISBN,
        string? newISBN,
        string? oldTitle,
        string? newTitle,
        int? oldAvailableCopies,
        int? newAvailableCopies,
        int? oldTotalCopies,
        int? newTotalCopies,
        DateTime changedAt,
        string changedBy)
    {
        return new BookAudit
        {
            AuditId = auditId,
            BookId = bookId,
            Action = action,
            OldISBN = oldISBN,
            NewISBN = newISBN,
            OldTitle = oldTitle,
            NewTitle = newTitle,
            OldAvailableCopies = oldAvailableCopies,
            NewAvailableCopies = newAvailableCopies,
            OldTotalCopies = oldTotalCopies,
            NewTotalCopies = newTotalCopies,
            ChangedAt = changedAt,
            ChangedBy = changedBy
        };
    }

    /// <summary>
    /// Returns a formatted string describing the changes made in this audit entry.
    /// </summary>
    public string GetChangeDescription()
    {
        return Action switch
        {
            "INSERT" => $"Book created: '{NewTitle}' (ISBN: {NewISBN})",
            "DELETE" => $"Book deleted: '{OldTitle}' (ISBN: {OldISBN})",
            "UPDATE" => GetUpdateDescription(),
            _ => $"Unknown action: {Action}"
        };
    }

    private string GetUpdateDescription()
    {
        var changes = new List<string>();

        if (OldTitle != NewTitle)
            changes.Add($"Title: '{OldTitle}' → '{NewTitle}'");

        if (OldISBN != NewISBN)
            changes.Add($"ISBN: {OldISBN} → {NewISBN}");

        if (OldAvailableCopies != NewAvailableCopies)
            changes.Add($"Available Copies: {OldAvailableCopies} → {NewAvailableCopies}");

        if (OldTotalCopies != NewTotalCopies)
            changes.Add($"Total Copies: {OldTotalCopies} → {NewTotalCopies}");

        return changes.Count > 0
            ? $"Book updated: {string.Join(", ", changes)}"
            : "Book updated (no tracked fields changed)";
    }

    public override string ToString()
    {
        return $"[{ChangedAt:yyyy-MM-dd HH:mm:ss}] {GetChangeDescription()} (by {ChangedBy})";
    }
}
