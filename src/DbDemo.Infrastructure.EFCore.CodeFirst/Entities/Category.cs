namespace DbDemo.Infrastructure.EFCore.CodeFirst.Entities;

/// <summary>
/// Category entity - Lookup table for book categories.
///
/// CODE-FIRST PATTERN: Clean POCO without Data Annotations
/// ========================================================
///
/// This entity is a pure POCO (Plain Old CLR Object):
/// - No Data Annotations
/// - All configuration done via Fluent API in CategoryConfiguration
/// - Navigation properties for relationships
///
/// BENEFITS OF CLEAN POCOs:
/// ✓ No dependency on System.ComponentModel.DataAnnotations
/// ✓ Entities are not polluted with infrastructure concerns
/// ✓ Single source of truth for configuration (Fluent API only)
/// ✓ Easier to test and maintain
/// </summary>
public class Category
{
    /// <summary>
    /// Primary key. Auto-incremented by database.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Category name (required, max 100 characters).
    /// Constraints configured via Fluent API.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional category description (max 500 characters).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When this category was created (UTC).
    /// Default value configured via Fluent API.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Navigation property: Books in this category (one-to-many).
    /// Relationship configured via Fluent API.
    /// </summary>
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
