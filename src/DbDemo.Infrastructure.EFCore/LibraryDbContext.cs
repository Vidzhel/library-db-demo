using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using DbDemo.Infrastructure.EFCore.EFModels;

namespace DbDemo.Infrastructure.EFCore;

/// <summary>
/// Entity Framework Core DbContext for the Library database.
///
/// This context demonstrates comprehensive EF Core features:
/// - Database-first approach with scaffolded entities
/// - External transaction support for integration with ADO.NET
/// - Global query filters for soft delete
/// - Change tracking behavior configuration
/// - Spatial data support with NetTopologySuite
/// - Support for database views and computed columns
/// </summary>
/// <remarks>
/// IMPORTANT: This DbContext uses ANEMIC EF entity models (EFModels/ folder).
/// The repository layer maps these to RICH Domain entities (DbDemo.Domain/).
///
/// Why separate EF entities from Domain entities?
/// 1. EF entities are optimized for ORM (public setters, parameterless constructors)
/// 2. Domain entities enforce business rules and invariants
/// 3. Allows independent evolution of persistence and domain models
/// 4. Prevents EF concerns from leaking into the domain layer
///
/// See README.md for detailed explanation of this architectural decision.
/// </remarks>
public partial class LibraryDbContext : DbContext
{
    /// <summary>
    /// Creates a new instance of LibraryDbContext.
    /// Use this constructor when creating the context manually with options.
    /// </summary>
    /// <param name="options">Context configuration options</param>
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    #region DbSets - Core Entities

    /// <summary>Authors table - Contains author information</summary>
    public virtual DbSet<Author> Authors { get; set; }

    /// <summary>Books table - Main catalog with JSON metadata and soft delete support</summary>
    public virtual DbSet<Book> Books { get; set; }

    /// <summary>BookAuthors junction table - Many-to-many relationship with additional properties</summary>
    public virtual DbSet<BookAuthor> BookAuthors { get; set; }

    /// <summary>BooksAudit table - Immutable audit trail (populated by database trigger)</summary>
    public virtual DbSet<BooksAudit> BooksAudits { get; set; }

    /// <summary>Categories table - Hierarchical self-referencing structure</summary>
    public virtual DbSet<Category> Categories { get; set; }

    /// <summary>LibraryBranches table - Physical locations with spatial data (GEOGRAPHY)</summary>
    public virtual DbSet<LibraryBranch> LibraryBranches { get; set; }

    /// <summary>Loans table - Borrowing records with computed columns</summary>
    public virtual DbSet<Loan> Loans { get; set; }

    /// <summary>Members table - Library member information with computed Age column</summary>
    public virtual DbSet<Member> Members { get; set; }

    /// <summary>SystemStatistics table - Time-series analytics data</summary>
    public virtual DbSet<SystemStatistic> SystemStatistics { get; set; }

    /// <summary>__MigrationsHistory table - DbUp migration tracking (read-only)</summary>
    public virtual DbSet<__MigrationsHistory> __MigrationsHistories { get; set; }

    #endregion

    #region DbSets - Database Views

    /// <summary>View: Books with extracted JSON metadata (tags, genre, series)</summary>
    public virtual DbSet<vw_BooksWithMetadatum> vw_BooksWithMetadata { get; set; }

    /// <summary>View: Branch distances calculated using spatial geography functions</summary>
    public virtual DbSet<vw_BranchDistance> vw_BranchDistances { get; set; }

    /// <summary>View: Demonstration of computed columns (FullName, Age, etc.)</summary>
    public virtual DbSet<vw_ComputedColumnsDemo> vw_ComputedColumnsDemos { get; set; }

    /// <summary>View: Daily statistics aggregated from hourly data</summary>
    public virtual DbSet<vw_DailyStatistic> vw_DailyStatistics { get; set; }

    /// <summary>View: Hourly statistics from SystemStatistics table</summary>
    public virtual DbSet<vw_HourlyStatistic> vw_HourlyStatistics { get; set; }

    /// <summary>View: Library dashboard summary with key metrics</summary>
    public virtual DbSet<vw_LibraryDashboard> vw_LibraryDashboards { get; set; }

    /// <summary>View: Monthly loan trends using window functions (ROW_NUMBER, RANK)</summary>
    public virtual DbSet<vw_MonthlyLoanTrend> vw_MonthlyLoanTrends { get; set; }

    /// <summary>View: Monthly loans by category (demonstrates PIVOT)</summary>
    public virtual DbSet<vw_MonthlyLoansByCategory> vw_MonthlyLoansByCategories { get; set; }

    /// <summary>View: Popular books ranked by loan count</summary>
    public virtual DbSet<vw_PopularBook> vw_PopularBooks { get; set; }

    /// <summary>View: Top books overall using window functions</summary>
    public virtual DbSet<vw_TopBooksOverall> vw_TopBooksOveralls { get; set; }

    /// <summary>View: Unpivoted loan statistics (demonstrates UNPIVOT)</summary>
    public virtual DbSet<vw_UnpivotedLoanStat> vw_UnpivotedLoanStats { get; set; }

    #endregion

    /// <summary>
    /// Configures the entity models and their relationships.
    /// This method is called automatically when the context is initialized.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Author entity
        modelBuilder.Entity<Author>(entity =>
        {
            // Composite index for efficient name searches
            entity.HasIndex(e => new { e.LastName, e.FirstName }, "IX_Authors_Name")
                .HasFillFactor(90);

            // Default values for audit columns
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            // Computed column: FullName = FirstName + ' ' + LastName (persisted)
            entity.Property(e => e.FullName)
                .HasComputedColumnSql("(([FirstName]+' ')+[LastName])", stored: true);
        });

        // Configure Book entity
        modelBuilder.Entity<Book>(entity =>
        {
            // Trigger for audit trail
            entity.ToTable(tb => tb.HasTrigger("TR_Books_Audit"));

            // Index on foreign key
            entity.HasIndex(e => e.CategoryId, "IX_Books_CategoryId")
                .HasFillFactor(80);

            // Filtered index for available books (IsDeleted = 0 AND AvailableCopies > 0)
            entity.HasIndex(e => new { e.IsDeleted, e.AvailableCopies }, "IX_Books_IsDeleted_AvailableCopies")
                .HasFilter("([IsDeleted]=(0) AND [AvailableCopies]>(0))")
                .HasFillFactor(80);

            // Filtered index for non-deleted books by title
            entity.HasIndex(e => e.Title, "IX_Books_Title")
                .HasFilter("([IsDeleted]=(0))")
                .HasFillFactor(80);

            // Unique index on ISBN
            entity.HasIndex(e => e.ISBN, "UQ_Books_ISBN")
                .IsUnique()
                .HasFillFactor(90);

            // Default values
            entity.Property(e => e.AvailableCopies).HasDefaultValue(1);
            entity.Property(e => e.TotalCopies).HasDefaultValue(1);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            // Computed columns
            entity.Property(e => e.PublishedDecade)
                .HasComputedColumnSql("(case when [PublishedDate] IS NULL then NULL else (datepart(year,[PublishedDate])/(10))*(10) end)", stored: true);
            entity.Property(e => e.YearPublished)
                .HasComputedColumnSql("(datepart(year,[PublishedDate]))", stored: false);

            // Relationship: Book -> Category (required)
            entity.HasOne(d => d.Category)
                .WithMany(p => p.Books)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Books_Categories");
        });

        // Configure BookAuthor entity (many-to-many junction with extra properties)
        modelBuilder.Entity<BookAuthor>(entity =>
        {
            // Index on AuthorId for reverse lookups
            entity.HasIndex(e => e.AuthorId, "IX_BookAuthors_AuthorId")
                .HasFillFactor(80);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            // Relationships with cascade delete (if book or author deleted, remove junction)
            entity.HasOne(d => d.Author)
                .WithMany(p => p.BookAuthors)
                .HasConstraintName("FK_BookAuthors_Authors");

            entity.HasOne(d => d.Book)
                .WithMany(p => p.BookAuthors)
                .HasConstraintName("FK_BookAuthors_Books");
        });

        // Configure BooksAudit entity (read-only, populated by trigger)
        modelBuilder.Entity<BooksAudit>(entity =>
        {
            entity.Property(e => e.ChangedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ChangedBy).HasDefaultValueSql("(suser_sname())");
        });

        // Configure Category entity (hierarchical self-referencing)
        modelBuilder.Entity<Category>(entity =>
        {
            // Filtered index on ParentCategoryId (only non-null values)
            entity.HasIndex(e => e.ParentCategoryId, "IX_Categories_ParentCategoryId")
                .HasFilter("([ParentCategoryId] IS NOT NULL)")
                .HasFillFactor(80);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            // Self-referencing relationship
            entity.HasOne(d => d.ParentCategory)
                .WithMany(p => p.InverseParentCategory)
                .HasConstraintName("FK_Categories_ParentCategory");
        });

        // Configure LibraryBranch entity (spatial data with NetTopologySuite)
        modelBuilder.Entity<LibraryBranch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LibraryB__3214EC07F515167F");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            // Spatial data configuration for GEOGRAPHY column
            // The Location property is of type Point from NetTopologySuite
            // EF Core will map this to SQL Server's GEOGRAPHY type
        });

        // Configure Loan entity
        modelBuilder.Entity<Loan>(entity =>
        {
            // Indexes for common queries
            entity.HasIndex(e => e.BookId, "IX_Loans_BookId")
                .HasFillFactor(70);

            entity.HasIndex(e => e.MemberId, "IX_Loans_MemberId")
                .HasFillFactor(70);

            // Filtered index for overdue loans (not returned, status active/overdue)
            entity.HasIndex(e => e.DueDate, "IX_Loans_Overdue")
                .HasFilter("([ReturnedAt] IS NULL AND ([Status] IN ((0), (2))))")
                .HasFillFactor(70);

            // Filtered index for active loans by status and due date
            entity.HasIndex(e => new { e.Status, e.DueDate }, "IX_Loans_Status_DueDate")
                .HasFilter("([ReturnedAt] IS NULL)")
                .HasFillFactor(70);

            // Filtered index for unpaid fees
            entity.HasIndex(e => new { e.IsFeePaid, e.LateFee }, "IX_Loans_UnpaidFees")
                .HasFilter("([IsFeePaid]=(0) AND [LateFee] IS NOT NULL AND [LateFee]>(0))")
                .HasFillFactor(80);

            // Default values
            entity.Property(e => e.BorrowedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MaxRenewalsAllowed).HasDefaultValue(2);

            // Computed column: DaysOverdue (non-persisted, calculated on query)
            entity.Property(e => e.DaysOverdue)
                .HasComputedColumnSql("(case when [ReturnedAt] IS NULL AND getdate()>[DueDate] then datediff(day,[DueDate],getdate()) else (0) end)", stored: false);

            // Relationships (NO ACTION on delete to preserve history)
            entity.HasOne(d => d.Book)
                .WithMany(p => p.Loans)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Loans_Books");

            entity.HasOne(d => d.Member)
                .WithMany(p => p.Loans)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Loans_Members");
        });

        // Configure Member entity
        modelBuilder.Entity<Member>(entity =>
        {
            // Filtered index on active members with expiration date
            entity.HasIndex(e => new { e.IsActive, e.MembershipExpiresAt }, "IX_Members_IsActive")
                .HasFilter("([IsActive]=(1))")
                .HasFillFactor(80);

            // Unique indexes
            entity.HasIndex(e => e.Email, "UQ_Members_Email")
                .IsUnique()
                .HasFillFactor(90);

            entity.HasIndex(e => e.MembershipNumber, "UQ_Members_MembershipNumber")
                .IsUnique()
                .HasFillFactor(90);

            // Default values
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MaxBooksAllowed).HasDefaultValue(5);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MemberSince).HasDefaultValueSql("(sysutcdatetime())");

            // Computed column: Age (non-persisted)
            entity.Property(e => e.Age)
                .HasComputedColumnSql("(datediff(year,[DateOfBirth],getdate()))", stored: false);
        });

        // Configure SystemStatistic entity
        modelBuilder.Entity<SystemStatistic>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SystemSt__3214EC07EFD0C855");
        });

        // Configure __MigrationsHistory entity (DbUp tracking table)
        modelBuilder.Entity<__MigrationsHistory>(entity =>
        {
            entity.HasKey(e => e.MigrationVersion).HasName("PK_MigrationsHistory");
        });

        #region Configure Database Views

        // All views are read-only (keyless entities)
        modelBuilder.Entity<vw_BooksWithMetadatum>(entity =>
        {
            entity.ToView("vw_BooksWithMetadata");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<vw_BranchDistance>(entity =>
        {
            entity.ToView("vw_BranchDistances");
        });

        modelBuilder.Entity<vw_ComputedColumnsDemo>(entity =>
        {
            entity.ToView("vw_ComputedColumnsDemo");
        });

        modelBuilder.Entity<vw_DailyStatistic>(entity =>
        {
            entity.ToView("vw_DailyStatistics");
        });

        modelBuilder.Entity<vw_HourlyStatistic>(entity =>
        {
            entity.ToView("vw_HourlyStatistics");
        });

        modelBuilder.Entity<vw_LibraryDashboard>(entity =>
        {
            entity.ToView("vw_LibraryDashboard");
        });

        modelBuilder.Entity<vw_MonthlyLoanTrend>(entity =>
        {
            entity.ToView("vw_MonthlyLoanTrends");
        });

        modelBuilder.Entity<vw_MonthlyLoansByCategory>(entity =>
        {
            entity.ToView("vw_MonthlyLoansByCategory");
        });

        modelBuilder.Entity<vw_PopularBook>(entity =>
        {
            entity.ToView("vw_PopularBooks");
        });

        modelBuilder.Entity<vw_TopBooksOverall>(entity =>
        {
            entity.ToView("vw_TopBooksOverall");
        });

        modelBuilder.Entity<vw_UnpivotedLoanStat>(entity =>
        {
            entity.ToView("vw_UnpivotedLoanStats");
        });

        #endregion

        #region Global Query Filters

        // Global query filter for soft delete on Books table
        // This filter is automatically applied to all queries unless explicitly disabled with IgnoreQueryFilters()
        modelBuilder.Entity<Book>().HasQueryFilter(b => !b.IsDeleted);

        // Note: Other entities (LibraryBranch) also have IsDeleted but may need different filter logic
        // For demonstration, we only apply global filter to Books table

        #endregion

        // Call partial method for additional configuration
        OnModelCreatingPartial(modelBuilder);
    }

    /// <summary>
    /// Partial method hook for additional model configuration.
    /// Can be implemented in a separate partial class file.
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    /// <summary>
    /// Configures EF Core behavior and options.
    /// </summary>
    /// <param name="optionsBuilder">Options builder</param>
    /// <remarks>
    /// This is intentionally left empty (--no-onconfiguring flag during scaffolding).
    /// Configuration is done externally when creating DbContextOptions.
    /// This allows for better testability and flexibility.
    /// </remarks>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Configuration is done externally via DbContextOptions
        // This method is intentionally left empty
    }
}
