using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DbDemo.Infrastructure.EFCore.CodeFirst.Entities;

namespace DbDemo.Infrastructure.EFCore.CodeFirst.Configuration;

/// <summary>
/// Fluent API configuration for Category entity.
///
/// CODE-FIRST PATTERN: IEntityTypeConfiguration<T>
/// ================================================
///
/// WHY SEPARATE CONFIGURATION CLASSES:
/// ✓ Keeps OnModelCreating clean and organized
/// ✓ Each entity's configuration in its own file
/// ✓ Easier to maintain and test
/// ✓ Better separation of concerns
///
/// FLUENT API vs DATA ANNOTATIONS:
/// - Data Annotations: Simple constraints on entity properties
/// - Fluent API: Complex configurations, relationships, indexes
/// - Use both: Annotations for simple, Fluent API for complex
/// </summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // Table name (optional - would default to "Categories" by convention)
        builder.ToTable("Categories");

        // Primary key (optional - "Id" is convention)
        builder.HasKey(c => c.Id);

        // Properties
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500)
            .IsRequired(false); // Optional

        // Default value for CreatedAt
        builder.Property(c => c.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd();

        // Indexes
        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("UQ_Categories_Name");

        // Relationships
        // One-to-Many: Category → Books
        builder.HasMany(c => c.Books)
            .WithOne(b => b.Category)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent deleting category if books exist
    }
}
