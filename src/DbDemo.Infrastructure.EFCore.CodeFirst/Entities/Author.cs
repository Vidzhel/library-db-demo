namespace DbDemo.Infrastructure.EFCore.CodeFirst.Entities;

/// <summary>
/// Author entity - Represents a book author.
///
/// CODE-FIRST PATTERN: Clean POCO without Data Annotations
/// ========================================================
///
/// This entity demonstrates:
/// - Pure POCO with no framework dependencies
/// - All configuration via Fluent API (AuthorConfiguration)
/// - Computed column (FullName) configured via Fluent API
/// - Many-to-many relationship via junction table (BookAuthor)
/// </summary>
public class Author
{
    /// <summary>
    /// Primary key. Auto-incremented by database.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Author's first name (required, max 100 characters).
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Author's last name (required, max 100 characters).
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Author's email address (unique, max 200 characters).
    /// Unique index configured via Fluent API.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Optional biography (max 2000 characters).
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// When this author record was created (UTC).
    /// Default value configured via Fluent API.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this author record was last updated (UTC).
    /// Updated automatically on SaveChanges (configured via Fluent API).
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property: Books by this author (many-to-many via BookAuthor).
    /// </summary>
    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();

    /// <summary>
    /// Computed column: Full name (FirstName + ' ' + LastName).
    /// Configured as computed column via Fluent API.
    /// Database calculates and stores this value.
    /// </summary>
    public string? FullName { get; set; }
}
