namespace DbDemo.Infrastructure.EFCore.CodeFirst.Entities;

/// <summary>
/// Book entity - Core entity representing a book in the library.
///
/// CODE-FIRST PATTERN: Clean POCO with behavior methods
/// =====================================================
///
/// This entity demonstrates:
/// - Pure POCO without Data Annotations
/// - All configuration via Fluent API (BookConfiguration)
/// - Soft delete pattern with behavior methods
/// - Foreign key relationship (Category)
/// - Many-to-many relationship (Author via BookAuthor)
///
/// SEMI-RICH ENTITY:
/// Unlike anemic POCOs, this entity has behavior methods:
/// - SoftDelete() - Encapsulates soft delete logic
/// - Restore() - Reverses soft delete
///
/// This shows Code-First entities can have domain logic.
/// </summary>
public class Book
{
    /// <summary>
    /// Primary key. Auto-incremented by database.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ISBN (International Standard Book Number).
    /// Must be unique (configured via Fluent API).
    /// Required, max 20 characters.
    /// </summary>
    public string ISBN { get; set; } = string.Empty;

    /// <summary>
    /// Book title (required, max 200 characters).
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional subtitle (max 200 characters).
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Foreign key to Category.
    /// Required - every book must have a category.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// When this book record was created (UTC).
    /// Default value configured via Fluent API (GETUTCDATE()).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this book record was last updated (UTC).
    /// Automatically updated on SaveChanges (configured via Fluent API).
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Soft delete flag.
    /// When true, book is "deleted" but not physically removed from database.
    /// Global query filter excludes deleted books automatically.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// When this book was soft-deleted (nullable).
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Navigation property: The category this book belongs to (many-to-one).
    /// </summary>
    public Category Category { get; set; } = null!;

    /// <summary>
    /// Navigation property: Authors of this book (many-to-many via BookAuthor).
    /// </summary>
    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();

    /// <summary>
    /// Soft delete this book.
    /// Sets IsDeleted flag and records deletion timestamp.
    /// </summary>
    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Restore a soft-deleted book.
    /// Clears IsDeleted flag and deletion timestamp.
    /// </summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
    }
}
