using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DbDemo.Infrastructure.EFCore.CodeFirst.Entities;

namespace DbDemo.Infrastructure.EFCore.CodeFirst.Configuration;

/// <summary>
/// Fluent API configuration for Book entity.
///
/// CODE-FIRST PATTERN: Global query filters and soft delete
/// ==========================================================
///
/// This configuration demonstrates:
/// - Global query filter (automatically exclude soft-deleted books)
/// - Unique index on ISBN
/// - Foreign key relationship to Category
/// - Default values and auto-update timestamps
/// - Soft delete pattern configuration
///
/// GLOBAL QUERY FILTER:
/// HasQueryFilter() automatically adds a WHERE clause to ALL queries.
/// Example: _context.Books.ToList() → SELECT * FROM Books WHERE IsDeleted = 0
///
/// To bypass filter: _context.Books.IgnoreQueryFilters().ToList()
/// </summary>
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        // Table name
        builder.ToTable("Books");

        // Primary key
        builder.HasKey(b => b.Id);

        // Properties
        builder.Property(b => b.ISBN)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Subtitle)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(b => b.CategoryId)
            .IsRequired();

        builder.Property(b => b.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(b => b.DeletedAt)
            .IsRequired(false);

        // Default value for CreatedAt
        builder.Property(b => b.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd();

        // Auto-update UpdatedAt on changes
        builder.Property(b => b.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAddOrUpdate();

        // Indexes
        // Unique index on ISBN (no two books can have same ISBN)
        builder.HasIndex(b => b.ISBN)
            .IsUnique()
            .HasDatabaseName("UQ_Books_ISBN");

        // Index on CategoryId for efficient queries like "find all books in category X"
        builder.HasIndex(b => b.CategoryId)
            .HasDatabaseName("IX_Books_CategoryId");

        // Filtered index: Only index active (non-deleted) books for better performance
        builder.HasIndex(b => b.Title)
            .HasDatabaseName("IX_Books_Title")
            .HasFilter("[IsDeleted] = 0");

        // Composite index on CategoryId + IsDeleted for efficient category queries
        builder.HasIndex(b => new { b.CategoryId, b.IsDeleted })
            .HasDatabaseName("IX_Books_CategoryId_IsDeleted");

        // Relationships
        // Many-to-One: Book → Category
        builder.HasOne(b => b.Category)
            .WithMany(c => c.Books)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict) // Prevent deleting category if books exist
            .IsRequired();

        // Many-to-Many: Book ↔ Author (via BookAuthor)
        builder.HasMany(b => b.BookAuthors)
            .WithOne(ba => ba.Book)
            .HasForeignKey(ba => ba.BookId)
            .OnDelete(DeleteBehavior.Cascade); // Delete BookAuthor entries when Book is deleted

        // GLOBAL QUERY FILTER: Automatically exclude soft-deleted books
        // This applies to ALL queries unless explicitly ignored with IgnoreQueryFilters()
        builder.HasQueryFilter(b => !b.IsDeleted);

        // How it works:
        // _context.Books.ToList()
        // → SELECT * FROM Books WHERE IsDeleted = 0  (filter applied automatically)
        //
        // _context.Books.IgnoreQueryFilters().ToList()
        // → SELECT * FROM Books  (filter bypassed)
    }
}
