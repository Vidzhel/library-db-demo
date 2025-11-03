namespace DbDemo.Infrastructure.EFCore.CodeFirst.Entities;

/// <summary>
/// BookAuthor entity - Junction table for many-to-many relationship between Books and Authors.
///
/// CODE-FIRST PATTERN: Clean junction table with composite key
/// ============================================================
///
/// This entity demonstrates:
/// - Pure POCO without Data Annotations
/// - Composite primary key (BookId + AuthorId) configured via Fluent API
/// - Extra properties on junction table (Role, DisplayOrder)
/// - Two foreign key relationships
///
/// WHY EXPLICIT JUNCTION TABLE:
/// EF Core 5+ can handle many-to-many automatically, but explicit junction tables give you:
/// ✓ Full control over the relationship
/// ✓ Can add extra properties (Role, DisplayOrder, etc.)
/// ✓ Can query junction table directly
/// ✓ More flexible for future changes
/// </summary>
public class BookAuthor
{
    /// <summary>
    /// Foreign key to Book.
    /// Part 1 of composite primary key.
    /// </summary>
    public int BookId { get; set; }

    /// <summary>
    /// Foreign key to Author.
    /// Part 2 of composite primary key.
    /// </summary>
    public int AuthorId { get; set; }

    /// <summary>
    /// Author's role for this book.
    /// Examples: "Primary Author", "Co-Author", "Editor", "Translator"
    /// Optional, max 50 characters, default "Author".
    /// </summary>
    public string Role { get; set; } = "Author";

    /// <summary>
    /// Display order for authors of this book (optional).
    /// Allows sorting authors: 1 = first author, 2 = second, etc.
    /// Null means unspecified order.
    /// </summary>
    public int? DisplayOrder { get; set; }

    /// <summary>
    /// When this book-author relationship was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Navigation property: The book in this relationship (many-to-one).
    /// </summary>
    public Book Book { get; set; } = null!;

    /// <summary>
    /// Navigation property: The author in this relationship (many-to-one).
    /// </summary>
    public Author Author { get; set; } = null!;
}
