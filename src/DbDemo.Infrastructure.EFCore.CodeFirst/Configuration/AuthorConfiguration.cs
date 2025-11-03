using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DbDemo.Infrastructure.EFCore.CodeFirst.Entities;

namespace DbDemo.Infrastructure.EFCore.CodeFirst.Configuration;

/// <summary>
/// Fluent API configuration for Author entity.
///
/// CODE-FIRST PATTERN: Computed columns and unique indexes
/// ========================================================
///
/// This configuration demonstrates:
/// - Computed column (FullName = FirstName + ' ' + LastName)
/// - Unique index on Email
/// - Default values for timestamps
/// - Automatic update of UpdatedAt on changes
/// - Many-to-many relationship configuration
/// </summary>
public class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        // Table name
        builder.ToTable("Authors");

        // Primary key
        builder.HasKey(a => a.Id);

        // Properties
        builder.Property(a => a.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Bio)
            .HasMaxLength(2000)
            .IsRequired(false);

        // Computed column: FullName = FirstName + ' ' + LastName
        // SQL Server specific syntax
        builder.Property(a => a.FullName)
            .HasMaxLength(201)
            .HasComputedColumnSql("[FirstName] + ' ' + [LastName]", stored: true);
        // stored: true means the value is computed once and stored (faster queries)
        // stored: false means computed on every read (always up-to-date)

        // Default values for timestamps
        builder.Property(a => a.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAddOrUpdate();
        // ValueGeneratedOnAddOrUpdate: EF updates this on SaveChanges

        // Indexes
        // Unique index on Email (no two authors can have same email)
        builder.HasIndex(a => a.Email)
            .IsUnique()
            .HasDatabaseName("UQ_Authors_Email");

        // Non-unique index on LastName for efficient queries like "find all authors with LastName = 'Smith'"
        builder.HasIndex(a => a.LastName)
            .HasDatabaseName("IX_Authors_LastName");

        // Composite index on FirstName + LastName for efficient full name searches
        builder.HasIndex(a => new { a.FirstName, a.LastName })
            .HasDatabaseName("IX_Authors_FullName");

        // Relationships
        // Many-to-Many: Author ↔ Book (via BookAuthor)
        // Configured in BookAuthor entity (junction table owns the relationship)
        builder.HasMany(a => a.BookAuthors)
            .WithOne(ba => ba.Author)
            .HasForeignKey(ba => ba.AuthorId)
            .OnDelete(DeleteBehavior.Cascade); // Delete BookAuthor entries when Author is deleted
    }
}
