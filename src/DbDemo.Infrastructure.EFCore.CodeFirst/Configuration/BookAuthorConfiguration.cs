using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DbDemo.Infrastructure.EFCore.CodeFirst.Entities;

namespace DbDemo.Infrastructure.EFCore.CodeFirst.Configuration;

/// <summary>
/// Fluent API configuration for BookAuthor entity (junction table).
///
/// CODE-FIRST PATTERN: Composite primary key configuration
/// ========================================================
///
/// This configuration demonstrates:
/// - Composite primary key (BookId + AuthorId)
/// - Two foreign key relationships
/// - Extra properties on junction table
/// - Default values for timestamps and role
///
/// COMPOSITE PRIMARY KEY:
/// HasKey(ba => new { ba.BookId, ba.AuthorId }) defines a composite key.
/// Both columns together form the primary key (prevents duplicate book-author pairs).
///
/// WHY COMPOSITE KEY FOR JUNCTION TABLES:
/// ✓ Natural key (represents the relationship)
/// ✓ Enforces uniqueness (same book-author pair can't exist twice)
/// ✓ No need for artificial surrogate Id column
/// ✓ Better performance for queries on junction table
/// </summary>
public class BookAuthorConfiguration : IEntityTypeConfiguration<BookAuthor>
{
    public void Configure(EntityTypeBuilder<BookAuthor> builder)
    {
        // Table name
        builder.ToTable("BookAuthors");

        // Composite primary key: (BookId, AuthorId)
        // This prevents duplicate book-author pairs
        builder.HasKey(ba => new { ba.BookId, ba.AuthorId });

        // Properties
        builder.Property(ba => ba.BookId)
            .IsRequired();

        builder.Property(ba => ba.AuthorId)
            .IsRequired();

        builder.Property(ba => ba.Role)
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue("Author");

        builder.Property(ba => ba.DisplayOrder)
            .IsRequired(false); // Optional - null means unspecified order

        // Default value for CreatedAt
        builder.Property(ba => ba.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd();

        // Indexes
        // Index on AuthorId for efficient reverse queries (find all books by author)
        builder.HasIndex(ba => ba.AuthorId)
            .HasDatabaseName("IX_BookAuthors_AuthorId");

        // Index on BookId (redundant with composite PK but helps with FK constraint)
        builder.HasIndex(ba => ba.BookId)
            .HasDatabaseName("IX_BookAuthors_BookId");

        // Relationships
        // Many-to-One: BookAuthor → Book
        builder.HasOne(ba => ba.Book)
            .WithMany(b => b.BookAuthors)
            .HasForeignKey(ba => ba.BookId)
            .OnDelete(DeleteBehavior.Cascade) // Delete BookAuthor when Book is deleted
            .IsRequired();

        // Many-to-One: BookAuthor → Author
        builder.HasOne(ba => ba.Author)
            .WithMany(a => a.BookAuthors)
            .HasForeignKey(ba => ba.AuthorId)
            .OnDelete(DeleteBehavior.Cascade) // Delete BookAuthor when Author is deleted
            .IsRequired();

        // NOTE: Cascade delete on both sides means:
        // - Deleting a Book deletes all its BookAuthor entries
        // - Deleting an Author deletes all its BookAuthor entries
        // - The Book/Author records themselves remain (only junction entries deleted)
    }
}
