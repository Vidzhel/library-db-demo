# Entity Framework Core: Code-First Development Complete Guide

## Table of Contents

1. [Introduction](#introduction)
2. [Code-First Fundamentals](#code-first-fundamentals)
3. [Code-First vs Database-First](#code-first-vs-database-first)
4. [Entity Definition](#entity-definition)
5. [Data Annotations](#data-annotations)
6. [Fluent API Configuration](#fluent-api-configuration)
7. [Relationships Configuration](#relationships-configuration)
8. [Migrations Deep Dive](#migrations-deep-dive)
9. [Data Seeding Strategies](#data-seeding-strategies)
10. [Conventions and Configuration](#conventions-and-configuration)
11. [Advanced Patterns](#advanced-patterns)
12. [Best Practices](#best-practices)
13. [Common Pitfalls and Anti-Patterns](#common-pitfalls-and-anti-patterns)
14. [Decision Guide: When to Use Code-First](#decision-guide-when-to-use-code-first)

---

## Introduction

**Entity Framework Core Code-First** is an approach where you define your domain model using **C# classes** (entities), and EF Core generates the database schema from these classes. This is the opposite of **Database-First** (scaffolding), where the database schema is the source of truth.

### What is Code-First?

Code-First means your **C# entities are the single source of truth** for your data model:

```
CODE-FIRST WORKFLOW:
====================

1. Define C# Entities          2. Generate Migration       3. Database Created
   ==================              ==================          ================

   public class Book               dotnet ef migrations        CREATE TABLE Books (
   {                              add InitialCreate              Id INT PRIMARY KEY,
       public int Id { get; set; }                                Title NVARCHAR(200),
       public string Title { get; set; }  ↓                       ISBN NVARCHAR(20)
       public string ISBN { get; set; }                         );
   }                               Migration files
                                   generated                   Database schema
   C# entities define                                         matches your entities
   the schema                      EF Core analyzes            exactly!
                                   your entities
```

### Why Code-First?

**Advantages:**
- ✅ **C# as source of truth** - Your domain model drives the database
- ✅ **Version control friendly** - Migrations are just C# files in git
- ✅ **Refactoring support** - Rename properties with confidence, generate migration
- ✅ **Type safety** - Database schema guaranteed to match your entities
- ✅ **Team collaboration** - Merge conflicts resolved in C# code, not SQL scripts
- ✅ **Full control** - Define exactly what you want using Fluent API
- ✅ **Cross-database** - Same entities work with SQL Server, PostgreSQL, SQLite
- ✅ **Rapid prototyping** - Change entities, regenerate database instantly

**Disadvantages:**
- ❌ **Limited for legacy databases** - Existing schemas may not map cleanly to entities
- ❌ **Complex migrations** - Hand-tuning needed for advanced SQL features
- ❌ **DBA workflows** - DBAs who prefer SQL scripts may resist
- ❌ **Learning curve** - Understanding migrations and Fluent API takes time
- ❌ **Generated SQL** - Less control over exact DDL statements
- ❌ **Migration conflicts** - Multiple developers creating migrations simultaneously

---

## Code-First Fundamentals

### The Code-First Development Cycle

Here's the typical workflow when developing with Code-First:

```
┌─────────────────────────────────────────────────────────────────┐
│                    CODE-FIRST DEVELOPMENT CYCLE                  │
└─────────────────────────────────────────────────────────────────┘

Step 1: DEFINE/MODIFY ENTITIES
────────────────────────────────
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;

    // Add new property
    public decimal Price { get; set; }  // ← NEW!
}

         ↓

Step 2: GENERATE MIGRATION
────────────────────────────
$ dotnet ef migrations add AddBookPrice
  → Analyzing entity changes...
  → Creating migration: 20240103_AddBookPrice.cs

  public partial class AddBookPrice : Migration
  {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
          migrationBuilder.AddColumn<decimal>(
              name: "Price",
              table: "Books",
              type: "decimal(18,2)",
              nullable: false,
              defaultValue: 0m);
      }
  }

         ↓

Step 3: REVIEW MIGRATION
─────────────────────────
  → Open generated migration file
  → Verify SQL is correct
  → Add custom SQL if needed (indexes, triggers, etc.)
  → Modify default values

         ↓

Step 4: APPLY TO DATABASE
──────────────────────────
$ dotnet ef database update
  → Applying migration: 20240103_AddBookPrice
  → Running: ALTER TABLE Books ADD Price decimal(18,2) NOT NULL DEFAULT 0
  → Migration complete!

         ↓

Step 5: VERIFY IN DATABASE
───────────────────────────
$ sqlcmd -Q "SELECT * FROM __EFMigrationsHistory"

  MigrationId                      ProductVersion
  ─────────────────────────────── ─────────────
  20240101_InitialCreate           8.0.0
  20240103_AddBookPrice            8.0.0  ← NEW!

         ↓

Step 6: COMMIT TO SOURCE CONTROL
─────────────────────────────────
$ git add Entities/Book.cs
$ git add Migrations/20240103_AddBookPrice.cs
$ git commit -m "Add Price property to Book entity"

  → Team members pull changes
  → They run: dotnet ef database update
  → Their databases updated automatically!
```

### Core Components of Code-First

Every Code-First project has these key components:

#### 1. Entity Classes

```csharp
// Entities/Book.cs
// Simple POCO (Plain Old CLR Object)
public class Book
{
    // EF Core conventions: "Id" or "BookId" → Primary Key
    public int Id { get; set; }

    // Required properties should have non-nullable types
    public string Title { get; set; } = string.Empty;

    // Optional properties use nullable types
    public string? Subtitle { get; set; }

    // Navigation properties for relationships
    public Category Category { get; set; } = null!;
    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}
```

#### 2. DbContext

```csharp
// LibraryDbContext.cs
// The "database session" - manages entities and queries
public class LibraryDbContext : DbContext
{
    // DbSets represent tables
    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<Author> Authors { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;

    // Constructor for dependency injection
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    // OnModelCreating: Configure schema using Fluent API
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
    }
}
```

#### 3. Entity Configuration Classes

```csharp
// Configuration/BookConfiguration.cs
// Separate configuration for clean organization
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        // Table name
        builder.ToTable("Books");

        // Primary key
        builder.HasKey(b => b.Id);

        // Properties
        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(200);

        // Indexes
        builder.HasIndex(b => b.ISBN)
            .IsUnique();

        // Relationships
        builder.HasOne(b => b.Category)
            .WithMany(c => c.Books)
            .HasForeignKey(b => b.CategoryId);
    }
}
```

#### 4. Migrations

```csharp
// Migrations/20240101120000_InitialCreate.cs
// Version-controlled database schema changes
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Forward migration: Create table
        migrationBuilder.CreateTable(
            name: "Books",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Title = table.Column<string>(maxLength: 200, nullable: false),
                ISBN = table.Column<string>(maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Books", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Rollback migration: Drop table
        migrationBuilder.DropTable(name: "Books");
    }
}
```

---

## Code-First vs Database-First

Understanding the differences helps you choose the right approach for your project.

### Workflow Comparison

```
DATABASE-FIRST (Scaffolding)                    CODE-FIRST
================================               ================================

1. DBA creates SQL migration scripts           1. Developer writes C# entities
   ↓                                              ↓
2. Run migrations against database             2. Run: dotnet ef migrations add
   ↓                                              ↓
3. Database schema exists                      3. Migration files generated
   ↓                                              ↓
4. Run: dotnet ef dbcontext scaffold           4. Run: dotnet ef database update
   ↓                                              ↓
5. EF Core generates entities from DB          5. Database created from entities
   ↓                                              ↓
6. Entities are READ-ONLY (regenerated)        6. Entities are SOURCE OF TRUTH

SOURCE OF TRUTH: Database                      SOURCE OF TRUTH: C# Entities
```

### Feature Comparison Table

| Feature | Code-First | Database-First | Winner |
|---------|-----------|----------------|--------|
| **Initial Setup** | Define entities in C# | Create DB schema first | Code-First (faster for new projects) |
| **Schema Changes** | Modify entity → Generate migration | Write SQL migration → Scaffold | Code-First (type-safe) |
| **Legacy Databases** | Difficult to model complex schemas | Scaffold existing database | Database-First |
| **Team Collaboration** | Merge conflicts in C# | Merge conflicts in SQL | Code-First (better tooling) |
| **Version Control** | C# migrations in git | SQL scripts in git | Tie (both work well) |
| **Refactoring Support** | Full IDE support (rename, etc.) | Manual SQL updates | Code-First |
| **DBA Review** | Review generated SQL | DBAs write SQL directly | Database-First |
| **Complex SQL** | Limited (stored procs, triggers) | Full SQL power | Database-First |
| **Type Safety** | Entity changes = compile errors | Schema changes = runtime errors | Code-First |
| **Learning Curve** | Learn EF conventions + Fluent API | Learn SQL + Scaffolding | Database-First (SQL is universal) |

### When to Use Code-First

**✅ Use Code-First when:**

1. **Greenfield projects** - Starting from scratch
   ```csharp
   // You define the domain model, database follows
   public class Customer
   {
       public int Id { get; set; }
       public string Name { get; set; } = string.Empty;
       public ICollection<Order> Orders { get; set; } = new List<Order>();
   }
   ```

2. **Domain-Driven Design** - Domain model is central
   ```csharp
   // Rich domain entities with behavior
   public class Order
   {
       public void AddItem(Product product, int quantity)
       {
           OrderItems.Add(new OrderItem { Product = product, Quantity = quantity });
           CalculateTotal();
       }
   }
   ```

3. **Rapid prototyping** - Schema changes frequently
   ```bash
   # Change entity → Regenerate migration → Update database
   $ dotnet ef migrations add AddCustomerEmail
   $ dotnet ef database update
   ```

4. **Team prefers C#** - Developers more comfortable with C# than SQL
   ```csharp
   // Configure schema in C# (IntelliSense, type safety)
   builder.Property(c => c.Email).HasMaxLength(200).IsRequired();
   ```

5. **Cross-database support** - Need to support multiple databases
   ```csharp
   // Same entities work with SQL Server, PostgreSQL, SQLite
   if (useSqlServer)
       options.UseSqlServer(connectionString);
   else
       options.UsePostgreSql(connectionString);
   ```

**❌ Avoid Code-First when:**

1. **Legacy database** - Complex existing schema that doesn't map to OOP
2. **DBA-controlled schema** - DBAs want full control over SQL
3. **Advanced SQL features** - Heavy use of stored procedures, triggers, SQL-specific features
4. **Regulatory requirements** - Schema changes must be reviewed as SQL scripts
5. **Performance-critical** - Need hand-tuned indexes, partitions, etc.

---

## Entity Definition

Entities are the building blocks of Code-First. Let's explore how to define them effectively.

### Basic Entity Structure

```csharp
// Entities/Category.cs
// Simplest entity: Id + properties + navigation properties
public class Category
{
    // PRIMARY KEY: "Id" property is convention for PK
    public int Id { get; set; }

    // REGULAR PROPERTIES: Mapped to columns
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    // NAVIGATION PROPERTY: One-to-Many relationship
    // Not a column - represents relationship
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
```

### EF Core Naming Conventions

EF Core uses conventions to reduce configuration. Understanding these saves you work:

```csharp
// CONVENTION: Property named "Id" or "CategoryId" → Primary Key
public class Category
{
    public int Id { get; set; }  // ← Primary Key (auto-increment)
}

// CONVENTION: DbSet<Category> Categories → Table named "Categories"
public DbSet<Category> Categories { get; set; }

// CONVENTION: Property type "string" → NVARCHAR(MAX) in SQL Server
public string Name { get; set; }  // → NVARCHAR(MAX)

// CONVENTION: Non-nullable type → NOT NULL column
public string Name { get; set; }  // → NOT NULL

// CONVENTION: Nullable type → NULL allowed
public string? Description { get; set; }  // → NULL allowed

// CONVENTION: Property named "CategoryId" + navigation property → Foreign Key
public class Book
{
    public int CategoryId { get; set; }  // ← Foreign Key
    public Category Category { get; set; }  // ← Navigation property
}

// CONVENTION: ICollection<T> → One-to-Many relationship
public ICollection<Book> Books { get; set; }  // Category has many Books
```

### Property Types and SQL Mapping

Here's how C# types map to SQL Server types:

```csharp
public class TypeMappingExample
{
    // INTEGER TYPES
    public int Id { get; set; }                    // → INT
    public long BigNumber { get; set; }            // → BIGINT
    public short SmallNumber { get; set; }         // → SMALLINT
    public byte TinyNumber { get; set; }           // → TINYINT

    // DECIMAL TYPES
    public decimal Price { get; set; }             // → DECIMAL(18,2)
    public double Measurement { get; set; }        // → FLOAT
    public float Precision { get; set; }           // → REAL

    // STRING TYPES
    public string Name { get; set; }               // → NVARCHAR(MAX)

    [MaxLength(100)]
    public string ShortName { get; set; }          // → NVARCHAR(100)

    // BOOLEAN
    public bool IsActive { get; set; }             // → BIT

    // DATE/TIME TYPES
    public DateTime CreatedAt { get; set; }        // → DATETIME2(7)
    public DateTimeOffset Timestamp { get; set; }  // → DATETIMEOFFSET(7)
    public DateOnly BirthDate { get; set; }        // → DATE
    public TimeOnly OpenTime { get; set; }         // → TIME

    // GUID
    public Guid UniqueId { get; set; }             // → UNIQUEIDENTIFIER

    // BINARY
    public byte[] FileData { get; set; }           // → VARBINARY(MAX)

    // ENUMS
    public BookStatus Status { get; set; }         // → INT (0, 1, 2, ...)

    // NULLABLE TYPES
    public int? OptionalNumber { get; set; }       // → INT NULL
    public DateTime? OptionalDate { get; set; }    // → DATETIME2(7) NULL
}

public enum BookStatus
{
    Available = 0,
    CheckedOut = 1,
    Lost = 2
}
```

### Required vs Optional Properties

```csharp
// .NET 6+ with nullable reference types enabled
#nullable enable

public class Book
{
    // REQUIRED (non-nullable) → NOT NULL in database
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;  // Must initialize!
    public string ISBN { get; set; } = string.Empty;

    // OPTIONAL (nullable) → NULL allowed in database
    public string? Subtitle { get; set; }  // Can be null
    public DateTime? PublishedDate { get; set; }  // Can be null

    // REQUIRED navigation property → NOT NULL foreign key
    public Category Category { get; set; } = null!;  // Will be loaded by EF
    public int CategoryId { get; set; }  // NOT NULL

    // OPTIONAL navigation property → NULL allowed foreign key
    public Publisher? Publisher { get; set; }  // Can be null
    public int? PublisherId { get; set; }  // NULL allowed
}

#nullable restore
```

### Entity Initialization Best Practices

```csharp
public class Author
{
    // OPTION 1: Initialize in property declaration (preferred)
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public ICollection<Book> Books { get; set; } = new List<Book>();

    // OPTION 2: Initialize in constructor
    public Author()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Books = new List<Book>();
    }

    // OPTION 3: Constructor with parameters (for required properties)
    public Author(string firstName, string lastName) : this()
    {
        FirstName = firstName;
        LastName = lastName;
    }
}

// USAGE:
var author = new Author
{
    FirstName = "Isaac",
    LastName = "Asimov"
};
// No NullReferenceException on Books - already initialized!
```

---

## Data Annotations

Data Annotations are attributes you place on entity properties to configure database schema. They're simple and readable for basic scenarios.

### Key Annotations

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Book
{
    // [Key] - Explicitly mark as primary key
    // Usually not needed if property is named "Id" or "BookId"
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  // Auto-increment
    public int Id { get; set; }

    // [Required] - NOT NULL column
    [Required]
    [MaxLength(200)]  // NVARCHAR(200) instead of NVARCHAR(MAX)
    public string Title { get; set; } = string.Empty;

    // [MaxLength] - Limit string length
    [MaxLength(20)]
    public string ISBN { get; set; } = string.Empty;

    // [Column] - Customize column name and type
    [Column("book_subtitle", TypeName = "nvarchar(200)")]
    public string? Subtitle { get; set; }

    // [ForeignKey] - Explicitly mark foreign key
    [ForeignKey(nameof(Category))]
    public int CategoryId { get; set; }

    // [DatabaseGenerated] - Control value generation
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime CreatedAt { get; set; }

    // Navigation properties don't need annotations
    public Category Category { get; set; } = null!;
}

public class Category
{
    [Key]
    public int Id { get; set; }

    // [StringLength] - Alternative to MaxLength, allows min/max
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Book> Books { get; set; } = new List<Book>();
}
```

### Common Data Annotations Reference

```csharp
public class DataAnnotationsExample
{
    // ===== PRIMARY KEY =====

    [Key]  // Mark as primary key
    public int Id { get; set; }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]  // Not auto-generated
    public int ManualId { get; set; }

    // ===== STRING VALIDATION =====

    [Required]  // NOT NULL
    [MaxLength(100)]  // Max length
    public string Name { get; set; } = string.Empty;

    [StringLength(200, MinimumLength = 5)]  // Min and max length
    public string Description { get; set; } = string.Empty;

    [EmailAddress]  // Validation (not database constraint)
    public string Email { get; set; } = string.Empty;

    [Phone]  // Validation
    public string Phone { get; set; } = string.Empty;

    [Url]  // Validation
    public string Website { get; set; } = string.Empty;

    [RegularExpression(@"^\d{3}-\d{2}-\d{4}$")]  // Pattern validation
    public string SSN { get; set; } = string.Empty;

    // ===== NUMERIC CONSTRAINTS =====

    [Range(0, 100)]  // Validation (not database constraint)
    public int Percentage { get; set; }

    [Column(TypeName = "decimal(18,2)")]  // Specify exact SQL type
    public decimal Price { get; set; }

    // ===== DATE/TIME =====

    [DataType(DataType.Date)]  // UI hint (not database constraint)
    public DateTime BirthDate { get; set; }

    [DataType(DataType.Time)]
    public DateTime AppointmentTime { get; set; }

    // ===== COLUMN CUSTOMIZATION =====

    [Column("custom_column_name")]  // Custom column name
    public string Property { get; set; } = string.Empty;

    [Column(TypeName = "varchar(50)")]  // Use VARCHAR instead of NVARCHAR
    public string AsciiOnly { get; set; } = string.Empty;

    [Column(Order = 1)]  // Column order in table (rarely used)
    public string FirstColumn { get; set; } = string.Empty;

    // ===== VALUE GENERATION =====

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  // Auto-increment
    public int AutoId { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]  // Computed column
    public string FullName { get; set; } = string.Empty;

    [DatabaseGenerated(DatabaseGeneratedOption.None)]  // No generation
    public int ManualValue { get; set; }

    // ===== TABLE CONFIGURATION =====

    [Table("CustomTableName")]  // On class level
    [Index(nameof(Email), IsUnique = true)]  // Index attribute (EF Core 5+)
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    // ===== NOT MAPPED =====

    [NotMapped]  // Not a database column
    public string TemporaryData { get; set; } = string.Empty;

    // ===== CONCURRENCY =====

    [Timestamp]  // Concurrency token (SQL Server ROWVERSION)
    public byte[]? RowVersion { get; set; }

    [ConcurrencyCheck]  // Use for optimistic concurrency
    public int Version { get; set; }
}
```

### Data Annotations Limitations

Data Annotations are great for simple scenarios but have limitations:

```csharp
// ❌ LIMITATION 1: Can't configure relationships fully
public class Book
{
    [ForeignKey(nameof(Category))]
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    // ❌ Can't specify DeleteBehavior with Data Annotations
    // ❌ Can't configure inverse navigation
    // ❌ Can't create composite foreign keys
}

// ❌ LIMITATION 2: Can't create composite primary keys
public class BookAuthor
{
    // ❌ Can't mark both as primary key with Data Annotations
    public int BookId { get; set; }
    public int AuthorId { get; set; }
}

// ❌ LIMITATION 3: Can't create filtered indexes
public class Book
{
    public string Title { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }

    // ❌ Want: Index on Title WHERE IsDeleted = 0
    // Can't do with Data Annotations!
}

// ❌ LIMITATION 4: Can't configure default values
public class Book
{
    public DateTime CreatedAt { get; set; }

    // ❌ Want: DEFAULT GETUTCDATE()
    // Can't specify with Data Annotations!
}

// ✅ SOLUTION: Use Fluent API for complex scenarios
// See next section!
```

---

## Fluent API Configuration

The Fluent API is EF Core's powerful programmatic configuration system. It allows you to configure everything that Data Annotations can do, plus much more.

### Why Fluent API?

```csharp
// DATA ANNOTATIONS: Limited, clutters entity classes
public class Book
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Index(IsUnique = false)]
    public string Title { get; set; } = string.Empty;

    // Entity class mixed with database concerns!
}

// FLUENT API: Full power, clean separation
public class Book
{
    // Clean POCO - just domain properties
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        // All database configuration in one place
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedOnAdd();
        builder.Property(b => b.Title).IsRequired().HasMaxLength(200);
        builder.HasIndex(b => b.Title);
    }
}
```

### IEntityTypeConfiguration Pattern

The recommended pattern is to create a separate configuration class for each entity:

```csharp
// Configuration/BookConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        // This method is called automatically by EF Core
        // when you use ApplyConfigurationsFromAssembly()

        ConfigureTable(builder);
        ConfigureProperties(builder);
        ConfigureIndexes(builder);
        ConfigureRelationships(builder);
    }

    private void ConfigureTable(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");
    }

    private void ConfigureProperties(EntityTypeBuilder<Book> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.ISBN)
            .IsRequired()
            .HasMaxLength(20);
    }

    private void ConfigureIndexes(EntityTypeBuilder<Book> builder)
    {
        builder.HasIndex(b => b.ISBN)
            .IsUnique()
            .HasDatabaseName("UQ_Books_ISBN");
    }

    private void ConfigureRelationships(EntityTypeBuilder<Book> builder)
    {
        builder.HasOne(b => b.Category)
            .WithMany(c => c.Books)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### Applying Configurations in DbContext

```csharp
public class LibraryDbContext : DbContext
{
    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<Author> Authors { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // OPTION 1: Apply all configurations automatically (RECOMMENDED)
        // Finds all classes implementing IEntityTypeConfiguration<T>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);

        // OPTION 2: Apply specific configuration
        modelBuilder.ApplyConfiguration(new BookConfiguration());
        modelBuilder.ApplyConfiguration(new AuthorConfiguration());

        // OPTION 3: Inline configuration (not recommended for large projects)
        modelBuilder.Entity<Book>(builder =>
        {
            builder.ToTable("Books");
            builder.HasKey(b => b.Id);
            // ... more configuration
        });
    }
}
```

### Fluent API Common Patterns

#### Table Configuration

```csharp
public void Configure(EntityTypeBuilder<Book> builder)
{
    // Table name
    builder.ToTable("Books");

    // Table with schema
    builder.ToTable("Books", "library");

    // Exclude from migrations (existing table)
    builder.ToTable("Books", t => t.ExcludeFromMigrations());
}
```

#### Primary Key Configuration

```csharp
public void Configure(EntityTypeBuilder<Book> builder)
{
    // Single column primary key
    builder.HasKey(b => b.Id);

    // Composite primary key
    builder.HasKey(ba => new { ba.BookId, ba.AuthorId });

    // Custom primary key name
    builder.HasKey(b => b.Id).HasName("PK_Books_Id");
}
```

#### Property Configuration

```csharp
public void Configure(EntityTypeBuilder<Book> builder)
{
    // String properties
    builder.Property(b => b.Title)
        .IsRequired()                    // NOT NULL
        .HasMaxLength(200)               // NVARCHAR(200)
        .HasColumnName("book_title")     // Custom column name
        .HasColumnType("nvarchar(200)"); // Explicit SQL type

    // Use VARCHAR instead of NVARCHAR (ASCII only, saves space)
    builder.Property(b => b.ISBN)
        .HasColumnType("varchar(20)");

    // Decimal precision
    builder.Property(b => b.Price)
        .HasColumnType("decimal(18,2)")  // 18 digits, 2 decimal places
        .HasPrecision(18, 2);            // Alternative syntax

    // Default values
    builder.Property(b => b.CreatedAt)
        .HasDefaultValueSql("GETUTCDATE()")    // SQL expression
        .ValueGeneratedOnAdd();                // Generated when row inserted

    builder.Property(b => b.IsActive)
        .HasDefaultValue(true);                // Literal value

    // Auto-update timestamp
    builder.Property(b => b.UpdatedAt)
        .HasDefaultValueSql("GETUTCDATE()")
        .ValueGeneratedOnAddOrUpdate();        // Updated on insert AND update

    // Computed columns
    builder.Property(b => b.FullTitle)
        .HasComputedColumnSql("[Title] + ' - ' + [Subtitle]", stored: true);

    // Value conversion (store enum as string)
    builder.Property(b => b.Status)
        .HasConversion<string>();

    // Ignore property (not mapped to database)
    builder.Ignore(b => b.TemporaryData);
}
```

#### Index Configuration

```csharp
public void Configure(EntityTypeBuilder<Book> builder)
{
    // Simple index
    builder.HasIndex(b => b.Title);

    // Unique index
    builder.HasIndex(b => b.ISBN)
        .IsUnique()
        .HasDatabaseName("UQ_Books_ISBN");

    // Composite index
    builder.HasIndex(b => new { b.CategoryId, b.PublishedDate })
        .HasDatabaseName("IX_Books_Category_Published");

    // Filtered index (SQL Server specific)
    builder.HasIndex(b => b.Title)
        .HasFilter("[IsDeleted] = 0")
        .HasDatabaseName("IX_Books_Title_Active");

    // Index with include columns (SQL Server 2016+)
    builder.HasIndex(b => b.ISBN)
        .IncludeProperties(b => new { b.Title, b.PublishedDate });
}
```

#### Query Filters (Global Filters)

```csharp
public void Configure(EntityTypeBuilder<Book> builder)
{
    // Global query filter - automatically applied to ALL queries
    builder.HasQueryFilter(b => !b.IsDeleted);

    // Now all queries automatically filter soft-deleted records:
    // _context.Books.ToList()
    // → SELECT * FROM Books WHERE IsDeleted = 0

    // To bypass the filter:
    // _context.Books.IgnoreQueryFilters().ToList()
    // → SELECT * FROM Books
}
```

### Combining Data Annotations and Fluent API

You can use both together. Fluent API always wins if there's a conflict:

```csharp
// Entity with Data Annotations
public class Book
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]  // ← Data Annotation says 100
    public string Title { get; set; } = string.Empty;
}

// Configuration with Fluent API
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.Property(b => b.Title)
            .HasMaxLength(200);  // ← Fluent API says 200

        // Result: MaxLength = 200 (Fluent API wins!)
    }
}
```

**Best Practice:**
- Always use **Fluent API**.

```csharp
// RECOMMENDED HYBRID APPROACH:
public class Book
{
    public int Id { get; set; }

    [Required]  // Documents that this is required
    [MaxLength(200)]  // Documents max length
    public string Title { get; set; } = string.Empty;

    [EmailAddress]  // Validation attribute
    public string ContactEmail { get; set; } = string.Empty;
}

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        // Database-specific config in Fluent API
        builder.HasIndex(b => b.ISBN).IsUnique();
        builder.HasQueryFilter(b => !b.IsDeleted);
        builder.Property(b => b.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    }
}
```

---

## Relationships Configuration

Configuring relationships is one of the most powerful features of Fluent API. Let's explore all relationship types.

### Relationship Types Overview

```
RELATIONSHIP TYPES IN EF CORE:
==============================

1. ONE-TO-MANY
   Category (1) ──────< (Many) Books
   One category has many books
   Each book belongs to one category

2. MANY-TO-MANY
   Books (Many) <──────> (Many) Authors
   One book can have many authors
   One author can write many books

3. ONE-TO-ONE
   User (1) ──────── (1) UserProfile
   Each user has exactly one profile
   Each profile belongs to exactly one user

4. SELF-REFERENCING
   Employee (1) ──────< (Many) Subordinates
   One employee (manager) has many subordinates
   Each subordinate has one manager
```

### One-to-Many Relationships

The most common relationship type.

```csharp
// ENTITIES:
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation property: "One Category has Many Books"
    public ICollection<Book> Books { get; set; } = new List<Book>();
}

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    // Foreign key
    public int CategoryId { get; set; }

    // Navigation property: "Each Book belongs to One Category"
    public Category Category { get; set; } = null!;
}

// CONFIGURATION:
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.HasOne(b => b.Category)      // Each Book has one Category
            .WithMany(c => c.Books)          // Each Category has many Books
            .HasForeignKey(b => b.CategoryId)  // Foreign key property
            .OnDelete(DeleteBehavior.Restrict)  // Don't allow deleting Category if Books exist
            .IsRequired();                    // CategoryId is NOT NULL
    }
}
```

#### Delete Behaviors

```csharp
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        // OPTION 1: Restrict (recommended for most cases)
        // Prevents deleting Category if Books exist
        builder.HasOne(b => b.Category)
            .WithMany(c => c.Books)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Try to delete category with books:
        // → SqlException: DELETE statement conflicted with REFERENCE constraint

        // OPTION 2: Cascade
        // Deleting Category automatically deletes all its Books
        builder.HasOne(b => b.Category)
            .WithMany(c => c.Books)
            .OnDelete(DeleteBehavior.Cascade);

        // Delete category → All books deleted too! (dangerous!)

        // OPTION 3: SetNull
        // Deleting Category sets CategoryId to NULL
        builder.HasOne(b => b.Category)
            .WithMany(c => c.Books)
            .OnDelete(DeleteBehavior.SetNull);

        // Delete category → All books have CategoryId = NULL
        // Requires CategoryId to be nullable: public int? CategoryId { get; set; }

        // OPTION 4: NoAction
        // No automatic action, database handles it
        builder.HasOne(b => b.Category)
            .WithMany(c => c.Books)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
```

### Many-to-Many Relationships

EF Core 5+ supports many-to-many without explicit junction table, but explicit junction tables give you more control.

#### Automatic Junction Table (EF Core 5+)

```csharp
// ENTITIES:
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    // Navigation property: Books can have many Authors
    public ICollection<Author> Authors { get; set; } = new List<Author>();
}

public class Author
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation property: Authors can write many Books
    public ICollection<Book> Books { get; set; } = new List<Book>();
}

// CONFIGURATION:
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        // EF Core automatically creates junction table "BookAuthor"
        builder.HasMany(b => b.Authors)
            .WithMany(a => a.Books)
            .UsingEntity(j => j.ToTable("BookAuthors"));  // Optional: custom table name
    }
}

// EF Core generates junction table:
// CREATE TABLE BookAuthors (
//     BooksId INT NOT NULL,
//     AuthorsId INT NOT NULL,
//     PRIMARY KEY (BooksId, AuthorsId),
//     FOREIGN KEY (BooksId) REFERENCES Books(Id),
//     FOREIGN KEY (AuthorsId) REFERENCES Authors(Id)
// )
```

#### Explicit Junction Table (Recommended)

When you need extra properties on the relationship (Role, DisplayOrder, etc.):

```csharp
// ENTITIES:
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    // Don't navigate directly to Authors - go through junction table
    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}

public class Author
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Don't navigate directly to Books - go through junction table
    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}

public class BookAuthor
{
    // Composite primary key (both columns)
    public int BookId { get; set; }
    public int AuthorId { get; set; }

    // Extra properties on the relationship
    public string Role { get; set; } = "Author";  // Author, Editor, Illustrator, etc.
    public int? DisplayOrder { get; set; }        // Order to display authors
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Book Book { get; set; } = null!;
    public Author Author { get; set; } = null!;
}

// CONFIGURATION:
public class BookAuthorConfiguration : IEntityTypeConfiguration<BookAuthor>
{
    public void Configure(EntityTypeBuilder<BookAuthor> builder)
    {
        builder.ToTable("BookAuthors");

        // Composite primary key
        builder.HasKey(ba => new { ba.BookId, ba.AuthorId });

        // Relationship to Book
        builder.HasOne(ba => ba.Book)
            .WithMany(b => b.BookAuthors)
            .HasForeignKey(ba => ba.BookId)
            .OnDelete(DeleteBehavior.Cascade);  // Delete BookAuthor when Book deleted

        // Relationship to Author
        builder.HasOne(ba => ba.Author)
            .WithMany(a => a.BookAuthors)
            .HasForeignKey(ba => ba.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);  // Delete BookAuthor when Author deleted

        // Indexes for efficient queries
        builder.HasIndex(ba => ba.BookId);
        builder.HasIndex(ba => ba.AuthorId);
    }
}

// USAGE:
// Get all authors for a book
var book = _context.Books
    .Include(b => b.BookAuthors)
        .ThenInclude(ba => ba.Author)
    .First(b => b.Id == 1);

foreach (var ba in book.BookAuthors.OrderBy(ba => ba.DisplayOrder))
{
    Console.WriteLine($"{ba.Role}: {ba.Author.Name}");
}
// Output:
// Author: Isaac Asimov
// Editor: John W. Campbell
```

### One-to-One Relationships

Less common but useful for splitting entities or optional detailed data.

```csharp
// ENTITIES:
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Navigation property: One User has One UserProfile
    public UserProfile? Profile { get; set; }
}

public class UserProfile
{
    public int Id { get; set; }  // Primary key

    // Foreign key to User (same as primary key - "shared primary key")
    public int UserId { get; set; }

    public string Bio { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation property: One UserProfile belongs to One User
    public User User { get; set; } = null!;
}

// CONFIGURATION:
public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.HasKey(p => p.Id);

        // One-to-One relationship
        builder.HasOne(p => p.User)
            .WithOne(u => u.Profile)
            .HasForeignKey<UserProfile>(p => p.UserId)  // Note: Generic parameter!
            .OnDelete(DeleteBehavior.Cascade);  // Delete Profile when User deleted

        // Alternative: Shared primary key (UserId is both PK and FK)
        // builder.HasKey(p => p.UserId);
        // builder.HasOne(p => p.User)
        //     .WithOne(u => u.Profile)
        //     .HasForeignKey<UserProfile>(p => p.UserId);
    }
}

// USAGE:
var user = _context.Users
    .Include(u => u.Profile)
    .First(u => u.Id == 1);

if (user.Profile != null)
{
    Console.WriteLine(user.Profile.Bio);
}
```

### Self-Referencing Relationships

An entity that relates to itself (e.g., employees and managers, categories with subcategories).

```csharp
// ENTITY:
public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Foreign key to manager
    public int? ManagerId { get; set; }

    // Navigation property: Employee's manager (another Employee)
    public Employee? Manager { get; set; }

    // Navigation property: Employee's subordinates
    public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();
}

// CONFIGURATION:
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);

        // Self-referencing one-to-many relationship
        builder.HasOne(e => e.Manager)
            .WithMany(e => e.Subordinates)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);  // Can't delete employee if they have subordinates
    }
}

// USAGE:
// Get employee with their subordinates
var manager = _context.Employees
    .Include(e => e.Subordinates)
    .First(e => e.Id == 1);

Console.WriteLine($"Manager: {manager.Name}");
foreach (var subordinate in manager.Subordinates)
{
    Console.WriteLine($"  - {subordinate.Name}");
}

// Get employee with their manager
var employee = _context.Employees
    .Include(e => e.Manager)
    .First(e => e.Id == 5);

Console.WriteLine($"Employee: {employee.Name}");
if (employee.Manager != null)
{
    Console.WriteLine($"  Reports to: {employee.Manager.Name}");
}
```

---

## Migrations Deep Dive

Migrations are EF Core's way of managing database schema changes over time. Understanding migrations is crucial for Code-First development.

### What Are Migrations?

```
MIGRATION CONCEPT:
==================

Migration = Version-controlled database schema change

Each migration is a C# class that describes:
1. How to apply the change (Up method)
2. How to roll back the change (Down method)

Your database has a history:
┌──────────────────────────────────────────────────────────┐
│ __EFMigrationsHistory Table                              │
├───────────────────────────────┬──────────────────────────┤
│ MigrationId                   │ ProductVersion           │
├───────────────────────────────┼──────────────────────────┤
│ 20240101120000_InitialCreate  │ 8.0.0                    │
│ 20240103150000_AddBookPrice   │ 8.0.0                    │
│ 20240105090000_AddAuthorBio   │ 8.0.0                    │
└───────────────────────────────┴──────────────────────────┘
```

### Migration Workflow Commands

```bash
# ===== CREATE MIGRATION =====

# Generate a new migration
dotnet ef migrations add InitialCreate
dotnet ef migrations add AddBookPrice
dotnet ef migrations add AddAuthorBio

# Specify project if not in current directory
dotnet ef migrations add InitialCreate \
    --project src/Infrastructure/Infrastructure.csproj \
    --startup-project src/WebApi/WebApi.csproj

# Specify output directory
dotnet ef migrations add InitialCreate \
    --output-dir Data/Migrations

# ===== APPLY MIGRATIONS =====

# Apply all pending migrations
dotnet ef database update

# Apply to specific migration
dotnet ef database update AddBookPrice

# Apply to initial (empty database)
dotnet ef database update 0

# ===== REMOVE MIGRATIONS =====

# Remove last migration (ONLY if not applied to database yet!)
dotnet ef migrations remove

# Remove last migration even if applied (dangerous!)
dotnet ef migrations remove --force

# ===== GENERATE SQL SCRIPT =====

# Generate SQL for all migrations
dotnet ef migrations script

# Generate SQL from specific migration to latest
dotnet ef migrations script InitialCreate

# Generate SQL between two migrations
dotnet ef migrations script AddBookPrice AddAuthorBio

# Generate idempotent script (safe to run multiple times)
dotnet ef migrations script --idempotent

# Output to file
dotnet ef migrations script --output migration.sql

# ===== DATABASE MANAGEMENT =====

# Drop database (WARNING: Deletes all data!)
dotnet ef database drop

# Drop with confirmation
dotnet ef database drop --force

# ===== INFORMATION =====

# List all migrations
dotnet ef migrations list

# Show DbContext information
dotnet ef dbcontext info

# List all DbContext types in project
dotnet ef dbcontext list

# ===== DESIGN-TIME SERVICES =====

# These commands require Microsoft.EntityFrameworkCore.Design package
# Already added to your project in this demo
```

### Anatomy of a Migration

When you run `dotnet ef migrations add AddBookPrice`, EF Core generates three files:

```csharp
// ===== FILE 1: Migrations/20240103150000_AddBookPrice.cs =====
// The migration itself

public partial class AddBookPrice : Migration
{
    // UP: Apply the migration (add Price column)
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "Price",
            table: "Books",
            type: "decimal(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.CreateIndex(
            name: "IX_Books_Price",
            table: "Books",
            column: "Price");
    }

    // DOWN: Rollback the migration (remove Price column)
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Books_Price",
            table: "Books");

        migrationBuilder.DropColumn(
            name: "Price",
            table: "Books");
    }
}

// ===== FILE 2: Migrations/20240103150000_AddBookPrice.Designer.cs =====
// Metadata about the migration (used by EF Core internally)

[DbContext(typeof(LibraryDbContext))]
[Migration("20240103150000_AddBookPrice")]
partial class AddBookPrice
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        // Full model snapshot at this point in time
        // Used to calculate next migration
    }
}

// ===== FILE 3: Migrations/LibraryDbContextModelSnapshot.cs =====
// Current model state (updated with each migration)

[DbContext(typeof(LibraryDbContext))]
partial class LibraryDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        // Current state of your entire model
        // EF Core compares this to your actual entities to detect changes

        modelBuilder.Entity("Book", b =>
        {
            b.Property<int>("Id");
            b.Property<string>("Title").HasMaxLength(200);
            b.Property<decimal>("Price").HasColumnType("decimal(18,2)");
            // ... all properties
        });
    }
}
```

### How Migrations Work Internally

```
MIGRATION GENERATION PROCESS:
==============================

1. You modify your entities:
   ┌──────────────────────────┐
   │ public class Book        │
   │ {                        │
   │     public int Id        │
   │     public string Title  │
   │     public decimal Price │ ← NEW!
   │ }                        │
   └──────────────────────────┘

         ↓

2. You run: dotnet ef migrations add AddPrice

3. EF Core compares:

   CURRENT MODEL                    MODEL SNAPSHOT
   (from your entities)             (last migration)
   ═══════════════════             ═══════════════
   Book.Id                          Book.Id
   Book.Title                       Book.Title
   Book.Price  ← NEW!               (not here)

         ↓

4. EF Core detects difference:
   "Book entity now has Price property that didn't exist before"

         ↓

5. EF Core generates migration:

   Up():   AddColumn("Price", "Books")
   Down(): DropColumn("Price", "Books")

         ↓

6. EF Core updates model snapshot:
   Now includes Book.Price

         ↓

7. You run: dotnet ef database update

8. EF Core checks __EFMigrationsHistory:
   - InitialCreate ✓ (already applied)
   - AddPrice ✗ (not applied yet)

         ↓

9. EF Core executes AddPrice.Up():
   ALTER TABLE Books ADD Price decimal(18,2) NOT NULL DEFAULT 0

         ↓

10. EF Core records in __EFMigrationsHistory:
    INSERT INTO __EFMigrationsHistory VALUES ('20240103150000_AddPrice', '8.0.0')

         ↓

11. Database updated! ✓
```

### Customizing Migrations

You can (and should) review and modify generated migrations:

```csharp
// GENERATED MIGRATION:
public partial class AddBookPrice : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ❌ PROBLEM: Default value is 0, but we want NULL for existing books
        migrationBuilder.AddColumn<decimal>(
            name: "Price",
            table: "Books",
            type: "decimal(18,2)",
            nullable: false,
            defaultValue: 0m);
    }
}

// CUSTOMIZED MIGRATION:
public partial class AddBookPrice : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ✅ STEP 1: Add column as nullable first
        migrationBuilder.AddColumn<decimal>(
            name: "Price",
            table: "Books",
            type: "decimal(18,2)",
            nullable: true);  // Allow NULL initially

        // ✅ STEP 2: Set default price for existing books
        migrationBuilder.Sql(@"
            UPDATE Books
            SET Price = 9.99
            WHERE Price IS NULL
        ");

        // ✅ STEP 3: Make column NOT NULL now that all values are set
        migrationBuilder.AlterColumn<decimal>(
            name: "Price",
            table: "Books",
            type: "decimal(18,2)",
            nullable: false);

        // ✅ STEP 4: Add default constraint for future inserts
        migrationBuilder.AddDefaultConstraint(
            name: "DF_Books_Price",
            table: "Books",
            column: "Price",
            value: 9.99m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("Price", "Books");
    }
}
```

### Common Migration Operations

```csharp
public partial class ExampleMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ===== CREATE TABLE =====
        migrationBuilder.CreateTable(
            name: "Books",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Title = table.Column<string>(maxLength: 200, nullable: false),
                Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Books", x => x.Id);
            });

        // ===== ADD COLUMN =====
        migrationBuilder.AddColumn<string>(
            name: "Subtitle",
            table: "Books",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: true);

        // ===== ALTER COLUMN =====
        migrationBuilder.AlterColumn<string>(
            name: "Title",
            table: "Books",
            type: "nvarchar(500)",  // Changed from 200 to 500
            maxLength: 500,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(200)",
            oldMaxLength: 200);

        // ===== DROP COLUMN =====
        migrationBuilder.DropColumn(
            name: "OldColumn",
            table: "Books");

        // ===== RENAME COLUMN =====
        migrationBuilder.RenameColumn(
            name: "OldName",
            table: "Books",
            newName: "NewName");

        // ===== CREATE INDEX =====
        migrationBuilder.CreateIndex(
            name: "IX_Books_ISBN",
            table: "Books",
            column: "ISBN",
            unique: true);

        // ===== DROP INDEX =====
        migrationBuilder.DropIndex(
            name: "IX_Books_ISBN",
            table: "Books");

        // ===== ADD FOREIGN KEY =====
        migrationBuilder.AddForeignKey(
            name: "FK_Books_Categories",
            table: "Books",
            column: "CategoryId",
            principalTable: "Categories",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        // ===== DROP FOREIGN KEY =====
        migrationBuilder.DropForeignKey(
            name: "FK_Books_Categories",
            table: "Books");

        // ===== EXECUTE RAW SQL =====
        migrationBuilder.Sql(@"
            CREATE TRIGGER Books_UpdatedAt
            ON Books
            AFTER UPDATE
            AS
            BEGIN
                UPDATE Books
                SET UpdatedAt = GETUTCDATE()
                WHERE Id IN (SELECT Id FROM inserted)
            END
        ");

        // ===== INSERT DATA =====
        migrationBuilder.InsertData(
            table: "Categories",
            columns: new[] { "Id", "Name", "Description" },
            values: new object[] { 1, "Fiction", "Literary fiction and novels" });

        // ===== UPDATE DATA =====
        migrationBuilder.UpdateData(
            table: "Categories",
            keyColumn: "Id",
            keyValue: 1,
            column: "Description",
            value: "Updated description");

        // ===== DELETE DATA =====
        migrationBuilder.DeleteData(
            table: "Categories",
            keyColumn: "Id",
            keyValue: 1);
    }
}
```

### Team Collaboration with Migrations

```
SCENARIO: Multiple developers creating migrations simultaneously
================================================================

Developer A (feature/add-pricing):
1. Creates entity property: public decimal Price { get; set; }
2. Generates migration: 20240103_AddBookPrice
3. Commits to git

Developer B (feature/add-reviews):
1. Creates entity: public class Review { ... }
2. Generates migration: 20240103_AddReviewTable
3. Commits to git

PROBLEM: Both migrations have same timestamp prefix!
20240103_AddBookPrice     ← Developer A
20240103_AddReviewTable   ← Developer B

SOLUTION 1: Manual timestamp adjustment
─────────────────────────────────────────
Developer B renames their migration file:
20240103_AddReviewTable  →  20240104_AddReviewTable

SOLUTION 2: Merge main first
─────────────────────────────
Developer B:
$ git pull origin main  # Get Developer A's migration
$ dotnet ef migrations add AddReviewTable  # EF Core auto-increments timestamp

SOLUTION 3: Resolve conflict after merge
──────────────────────────────────────────
$ git merge feature/add-pricing
# Conflict in LibraryDbContextModelSnapshot.cs
# Resolve by accepting both changes
$ dotnet ef migrations add MergeConflictResolution
# This creates a new migration that combines both changes

BEST PRACTICE:
──────────────
1. Pull main before creating migration
2. Create migration
3. Test migration: dotnet ef database update
4. Push to remote immediately
5. Other developers pull and update their databases
```

### Rollback Strategies

```bash
# ===== ROLLBACK LAST MIGRATION =====

# Remove last migration (ONLY if not applied to database)
$ dotnet ef migrations remove

# If already applied to database:
$ dotnet ef database update PreviousMigration  # Rollback
$ dotnet ef migrations remove  # Then remove migration file

# ===== ROLLBACK TO SPECIFIC MIGRATION =====

# List migrations
$ dotnet ef migrations list
  20240101_InitialCreate
  20240103_AddPrice
  20240105_AddReviews  ← Current

# Rollback to AddPrice (undoes AddReviews)
$ dotnet ef database update AddPrice

# Rollback to empty database
$ dotnet ef database update 0

# ===== ROLLBACK IN PRODUCTION =====

# OPTION 1: Generate rollback script
$ dotnet ef migrations script AddReviews AddPrice --output rollback.sql

# Review rollback.sql, then execute on production:
$ sqlcmd -S prod-server -d LibraryDb -i rollback.sql

# OPTION 2: Create "undo" migration
$ dotnet ef database update AddPrice  # Rollback in dev
$ dotnet ef migrations remove  # Remove bad migration
$ dotnet ef migrations add FixReviews  # Create new migration with correct changes
$ dotnet ef database update  # Apply in dev
# Test thoroughly, then deploy to production

# WARNING: NEVER drop and recreate production database!
# Always use migrations to evolve schema
```

---

## Data Seeding Strategies

Seeding is populating the database with initial data. EF Core provides multiple strategies.

### Strategy 1: HasData() in OnModelCreating

Best for: Lookup/reference data that rarely changes.

```csharp
public class LibraryDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed categories
        modelBuilder.Entity<Category>().HasData(
            new Category
            {
                Id = 1,
                Name = "Fiction",
                Description = "Literary fiction and novels",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Category
            {
                Id = 2,
                Name = "Non-Fiction",
                Description = "Biographies, history, science",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed authors
        modelBuilder.Entity<Author>().HasData(
            new Author
            {
                Id = 1,
                FirstName = "Isaac",
                LastName = "Asimov",
                Email = "isaac.asimov@example.com",
                CreatedAt = DateTime.UtcNow
            }
        );

        // Seed books
        modelBuilder.Entity<Book>().HasData(
            new Book
            {
                Id = 1,
                ISBN = "978-0553293357",
                Title = "Foundation",
                CategoryId = 1,  // Must use FK value, not navigation property!
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}

// When you generate migration, seed data is included:
$ dotnet ef migrations add InitialSeed

// Generated migration includes:
migrationBuilder.InsertData(
    table: "Categories",
    columns: new[] { "Id", "Name", "Description", "CreatedAt" },
    values: new object[] { 1, "Fiction", "Literary fiction and novels", ... });
```

**HasData() Characteristics:**
- ✅ Seed data included in migrations
- ✅ Version controlled (in C# code)
- ✅ Applied automatically with `dotnet ef database update`
- ❌ Can't use navigation properties (must use foreign key values)
- ❌ Must provide primary key values manually
- ❌ Updates to seed data require new migration
- ❌ Not suitable for large datasets

### Strategy 2: Custom Seeder Class

Best for: Complex data, conditional seeding, large datasets.

```csharp
// Seed/LibraryDataSeeder.cs
public class LibraryDataSeeder
{
    private readonly LibraryDbContext _context;

    public LibraryDataSeeder(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        // Only seed if database is empty
        if (await _context.Categories.AnyAsync())
        {
            return;  // Already seeded
        }

        await SeedCategoriesAsync();
        await SeedAuthorsAsync();
        await SeedBooksAsync();
        await SeedBookAuthorsAsync();
    }

    private async Task SeedCategoriesAsync()
    {
        var categories = new[]
        {
            new Category { Name = "Fiction", Description = "Literary fiction and novels" },
            new Category { Name = "Non-Fiction", Description = "Biographies, history, science" },
            new Category { Name = "Science Fiction", Description = "Sci-fi novels" },
            new Category { Name = "Mystery", Description = "Detective stories and thrillers" },
            new Category { Name = "Biography", Description = "Life stories" }
        };

        _context.Categories.AddRange(categories);
        await _context.SaveChangesAsync();
    }

    private async Task SeedAuthorsAsync()
    {
        var authors = new[]
        {
            new Author
            {
                FirstName = "Isaac",
                LastName = "Asimov",
                Email = "isaac.asimov@example.com",
                Bio = "American writer and professor of biochemistry..."
            },
            new Author
            {
                FirstName = "Agatha",
                LastName = "Christie",
                Email = "agatha.christie@example.com",
                Bio = "English writer known for detective novels..."
            }
        };

        _context.Authors.AddRange(authors);
        await _context.SaveChangesAsync();
    }

    private async Task SeedBooksAsync()
    {
        // ✅ Can use navigation properties!
        var scienceFiction = await _context.Categories
            .FirstAsync(c => c.Name == "Science Fiction");

        var books = new[]
        {
            new Book
            {
                ISBN = "978-0553293357",
                Title = "Foundation",
                Subtitle = "The Foundation Trilogy, Book 1",
                Category = scienceFiction  // ← Navigation property works!
            }
        };

        _context.Books.AddRange(books);
        await _context.SaveChangesAsync();
    }

    private async Task SeedBookAuthorsAsync()
    {
        var foundation = await _context.Books.FirstAsync(b => b.ISBN == "978-0553293357");
        var asimov = await _context.Authors.FirstAsync(a => a.LastName == "Asimov");

        var bookAuthor = new BookAuthor
        {
            Book = foundation,
            Author = asimov,
            Role = "Author",
            DisplayOrder = 1
        };

        _context.BookAuthors.Add(bookAuthor);
        await _context.SaveChangesAsync();
    }
}

// Usage in Program.cs or Startup:
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    var seeder = new LibraryDataSeeder(context);
    await seeder.SeedAsync();
}
```

**Custom Seeder Characteristics:**
- ✅ Full control over seeding logic
- ✅ Can use navigation properties
- ✅ Can seed conditionally (check if data exists)
- ✅ Can read from files (JSON, CSV, etc.)
- ✅ Suitable for large datasets
- ❌ Not included in migrations
- ❌ Must run manually or in application startup
- ❌ Not version controlled (unless reading from files in repo)

### Strategy 3: Migration with Custom SQL

Best for: One-time data import, production data migration.

```csharp
public partial class SeedInitialData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Import from CSV file
        var csv = File.ReadAllText("SeedData/books.csv");
        var lines = csv.Split('\n').Skip(1);  // Skip header

        foreach (var line in lines)
        {
            var values = line.Split(',');
            migrationBuilder.Sql($@"
                INSERT INTO Books (ISBN, Title, CategoryId, CreatedAt)
                VALUES ('{values[0]}', '{values[1]}', {values[2]}, GETUTCDATE())
            ");
        }

        // Or embed SQL directly
        migrationBuilder.Sql(@"
            INSERT INTO Categories (Name, Description, CreatedAt)
            VALUES
                ('Fiction', 'Literary fiction and novels', GETUTCDATE()),
                ('Non-Fiction', 'Biographies, history, science', GETUTCDATE()),
                ('Science Fiction', 'Sci-fi novels', GETUTCDATE())
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM Books");
        migrationBuilder.Sql("DELETE FROM Categories");
    }
}
```

### Strategy 4: Reading from JSON Files

Best for: Large datasets, test data, configuration data.

```csharp
// Seed/LibraryJsonSeeder.cs
public class LibraryJsonSeeder
{
    private readonly LibraryDbContext _context;

    public LibraryJsonSeeder(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task SeedFromJsonAsync()
    {
        if (await _context.Books.AnyAsync())
        {
            return;  // Already seeded
        }

        // Read JSON files
        var categoriesJson = await File.ReadAllTextAsync("SeedData/categories.json");
        var authorsJson = await File.ReadAllTextAsync("SeedData/authors.json");
        var booksJson = await File.ReadAllTextAsync("SeedData/books.json");

        // Deserialize
        var categories = JsonSerializer.Deserialize<List<Category>>(categoriesJson);
        var authors = JsonSerializer.Deserialize<List<Author>>(authorsJson);
        var books = JsonSerializer.Deserialize<List<Book>>(booksJson);

        // Add to context
        _context.Categories.AddRange(categories!);
        _context.Authors.AddRange(authors!);
        _context.Books.AddRange(books!);

        await _context.SaveChangesAsync();
    }
}

// SeedData/categories.json
[
  {
    "id": 1,
    "name": "Fiction",
    "description": "Literary fiction and novels",
    "createdAt": "2024-01-01T00:00:00Z"
  },
  {
    "id": 2,
    "name": "Non-Fiction",
    "description": "Biographies, history, science",
    "createdAt": "2024-01-01T00:00:00Z"
  }
]
```

### Choosing a Seeding Strategy

```
DECISION TREE:
══════════════

Is it lookup/reference data that rarely changes?
│
├─ YES → Use HasData() in OnModelCreating
│         ✅ Simple
│         ✅ Version controlled
│         ✅ Applied with migrations
│
└─ NO → Is it a large dataset (>1000 rows)?
        │
        ├─ YES → Use JSON files + Custom Seeder
        │         ✅ Easy to edit
        │         ✅ Can version control JSON
        │         ✅ Performant
        │
        └─ NO → Is it environment-specific?
                │
                ├─ YES → Use Custom Seeder with conditions
                │         ✅ Different data per environment
                │         ✅ Can check environment variables
                │
                └─ NO → Is it test data?
                        │
                        ├─ YES → Use Custom Seeder in tests
                        │         ✅ Isolated per test
                        │         ✅ Clean up after test
                        │
                        └─ NO → Default to Custom Seeder
                                  ✅ Most flexible
                                  ✅ Can combine strategies
```

---

## Conventions and Configuration

EF Core uses **conventions** to reduce configuration. Understanding conventions helps you write less code.

### EF Core Conventions

```csharp
// EF Core infers configuration from your code structure

public class Book
{
    // CONVENTION: Property named "Id" or "BookId" → Primary Key
    public int Id { get; set; }  // ← Primary key, auto-increment

    // CONVENTION: Non-nullable reference type → NOT NULL
    public string Title { get; set; } = string.Empty;  // → NOT NULL

    // CONVENTION: Nullable reference type → NULL allowed
    public string? Subtitle { get; set; }  // → NULL allowed

    // CONVENTION: Property named "{ClassName}Id" + nav property → Foreign Key
    public int CategoryId { get; set; }  // ← Foreign key
    public Category Category { get; set; } = null!;  // ← Navigation property

    // CONVENTION: ICollection<T> → One-to-Many relationship
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

// CONVENTION: DbSet<T> property name → Table name
public class LibraryDbContext : DbContext
{
    public DbSet<Book> Books { get; set; } = null!;  // → Table: "Books"
    public DbSet<Author> Authors { get; set; } = null!;  // → Table: "Authors"
}
```

### Convention Priority

```
CONFIGURATION PRIORITY (lowest to highest):
═══════════════════════════════════════════

1. EF Core Conventions
   ↓ (overridden by)
2. Data Annotations
   ↓ (overridden by)
3. Fluent API
   ↓ (overridden by)
4. Custom Conventions (advanced)

Example:
────────

// Entity:
public class Book
{
    public int Id { get; set; }  // Convention: Primary key

    [MaxLength(100)]  // Data Annotation: Max length 100
    public string Title { get; set; } = string.Empty;
}

// Configuration:
builder.Property(b => b.Title).HasMaxLength(200);  // Fluent API: Max length 200

// Result: Title is NVARCHAR(200)
// Fluent API wins!
```

### Configuring Conventions

You can customize EF Core's conventions (advanced):

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    // All decimal properties → decimal(18,2)
    configurationBuilder.Properties<decimal>()
        .HavePrecision(18, 2);

    // All string properties → NVARCHAR(500) instead of NVARCHAR(MAX)
    configurationBuilder.Properties<string>()
        .HaveMaxLength(500);

    // All DateTime properties → Use UTC
    configurationBuilder.Properties<DateTime>()
        .HaveConversion<DateTimeToUtcConverter>();

    // Custom naming convention: snake_case
    configurationBuilder.Properties<string>()
        .HaveColumnName(p => ToSnakeCase(p.Name));
}

private string ToSnakeCase(string input)
{
    return Regex.Replace(input, "([a-z])([A-Z])", "$1_$2").ToLower();
}

// Now all string columns are snake_case:
// public string FirstName { get; set; }  → column: "first_name"
```

---

## Advanced Patterns

### Pattern 1: Soft Delete with Global Query Filters

```csharp
// Entity:
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
    }
}

// Configuration:
builder.HasQueryFilter(b => !b.IsDeleted);

// Usage:
// All queries automatically exclude soft-deleted books
var activeBooks = _context.Books.ToList();  // WHERE IsDeleted = 0

// Include soft-deleted books
var allBooks = _context.Books.IgnoreQueryFilters().ToList();

// Soft delete a book
var book = _context.Books.First(b => b.Id == 1);
book.SoftDelete();
_context.SaveChanges();

// Book disappears from queries (but still in database)
var book = _context.Books.FirstOrDefault(b => b.Id == 1);  // null!
```

### Pattern 2: Audit Fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)

```csharp
// Base entity:
public abstract class AuditableEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

// Entities inherit:
public class Book : AuditableEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

// Override SaveChanges to automatically set audit fields:
public class LibraryDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public LibraryDbContext(
        DbContextOptions<LibraryDbContext> options,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<AuditableEntity>();
        var currentUser = _currentUserService.GetCurrentUser();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = currentUser;
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = currentUser;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = currentUser;
            }
        }
    }
}

// Usage:
var book = new Book { Title = "Foundation" };
_context.Books.Add(book);
_context.SaveChanges();

// CreatedAt, CreatedBy, UpdatedAt, UpdatedBy set automatically!
```

### Pattern 3: Value Objects

```csharp
// Value Object:
public class Money
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Zero(string currency) => new Money(0, currency);
}

// Entity:
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    // Owned type (not a separate table)
    public Money Price { get; set; } = Money.Zero("USD");
}

// Configuration:
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.OwnsOne(b => b.Price, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnName("Price_Amount")
                .HasColumnType("decimal(18,2)");

            price.Property(p => p.Currency)
                .HasColumnName("Price_Currency")
                .HasMaxLength(3);
        });
    }
}

// Result: Book table has Price_Amount and Price_Currency columns
// No separate Money table created
```

### Pattern 4: Concurrency Tokens

```csharp
// Entity:
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    // Concurrency token (SQL Server ROWVERSION)
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}

// Or with Fluent API:
builder.Property(b => b.RowVersion)
    .IsRowVersion();

// Usage:
try
{
    var book = _context.Books.First(b => b.Id == 1);
    book.Title = "Updated Title";
    _context.SaveChanges();
}
catch (DbUpdateConcurrencyException ex)
{
    // Someone else modified this book!
    // Handle conflict:
    // Option 1: Reload and try again
    // Option 2: Show error to user
    // Option 3: Merge changes
}
```

---

## Best Practices

### 1. Separate Configuration Classes

```csharp
// ✅ GOOD: One configuration class per entity
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        // All Book configuration here
    }
}

// ❌ BAD: All configuration in OnModelCreating
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Book>(builder =>
    {
        // 500 lines of configuration...
    });

    modelBuilder.Entity<Author>(builder =>
    {
        // Another 500 lines...
    });
    // DbContext becomes huge and unmaintainable
}
```

### 2. Use Navigation Properties

```csharp
// ✅ GOOD: Use navigation properties
var book = _context.Books
    .Include(b => b.Category)
    .First(b => b.Id == 1);

Console.WriteLine(book.Category.Name);

// ❌ BAD: Manual joins with foreign keys
var book = _context.Books.First(b => b.Id == 1);
var category = _context.Categories.First(c => c.Id == book.CategoryId);
Console.WriteLine(category.Name);
```

### 3. Initialize Collections

```csharp
// ✅ GOOD: Initialize collections to prevent NullReferenceException
public class Author
{
    public ICollection<Book> Books { get; set; } = new List<Book>();
}

// Now safe:
var author = new Author();
author.Books.Add(new Book());  // No NullReferenceException

// ❌ BAD: Uninitialized collections
public class Author
{
    public ICollection<Book> Books { get; set; }
}

var author = new Author();
author.Books.Add(new Book());  // NullReferenceException!
```

### 4. Use Async Methods

```csharp
// ✅ GOOD: Async methods for I/O operations
public async Task<List<Book>> GetBooksAsync()
{
    return await _context.Books.ToListAsync();
}

// ❌ BAD: Blocking calls
public List<Book> GetBooks()
{
    return _context.Books.ToList();  // Blocks thread!
}
```

### 5. Dispose DbContext Properly

```csharp
// ✅ GOOD: Use using statement
using (var context = new LibraryDbContext())
{
    var books = context.Books.ToList();
}  // Disposed automatically

// ✅ BETTER: Use dependency injection
// DbContext is scoped - disposed at end of request
public class BookService
{
    private readonly LibraryDbContext _context;

    public BookService(LibraryDbContext context)
    {
        _context = context;  // Injected
    }
}

// ❌ BAD: Don't dispose manually
var context = new LibraryDbContext();
var books = context.Books.ToList();
// Forgot to dispose - memory leak!
```

### 6. Explicitly Configure Relationships

```csharp
// ✅ GOOD: Explicit configuration
builder.HasOne(b => b.Category)
    .WithMany(c => c.Books)
    .HasForeignKey(b => b.CategoryId)
    .OnDelete(DeleteBehavior.Restrict)
    .IsRequired();

// ❌ BAD: Rely on conventions for important relationships
// What if conventions change in future EF versions?
```

### 7. Use Meaningful Migration Names

```bash
# ✅ GOOD: Descriptive names
$ dotnet ef migrations add AddBookPriceColumn
$ dotnet ef migrations add CreateReviewsTable
$ dotnet ef migrations add AddUniqueIndexOnISBN

# ❌ BAD: Generic names
$ dotnet ef migrations add Update1
$ dotnet ef migrations add Changes
$ dotnet ef migrations add Fix
```

---

## Common Pitfalls and Anti-Patterns

### Anti-Pattern 1: N+1 Query Problem

```csharp
// ❌ BAD: N+1 queries
var authors = _context.Authors.ToList();  // 1 query
foreach (var author in authors)
{
    // N queries (one per author)!
    var bookCount = author.Books.Count;
    Console.WriteLine($"{author.Name}: {bookCount} books");
}
// Total: 1 + N queries

// ✅ GOOD: Single query with Include
var authors = _context.Authors
    .Include(a => a.Books)
    .ToList();  // 1 query with JOIN

foreach (var author in authors)
{
    var bookCount = author.Books.Count;  // No database query!
    Console.WriteLine($"{author.Name}: {bookCount} books");
}
// Total: 1 query
```

### Anti-Pattern 2: Retrieving Too Much Data

```csharp
// ❌ BAD: Loading all columns when you only need a few
var bookTitles = _context.Books
    .ToList()  // Loads all columns for all books!
    .Select(b => b.Title)
    .ToList();

// ✅ GOOD: Project to what you need
var bookTitles = _context.Books
    .Select(b => b.Title)  // Only SELECT Title column
    .ToList();

// ✅ BETTER: Use DTOs for complex projections
var bookDtos = _context.Books
    .Select(b => new BookDto
    {
        Title = b.Title,
        AuthorName = b.BookAuthors.First().Author.FullName,
        CategoryName = b.Category.Name
    })
    .ToList();
```

### Anti-Pattern 3: Not Using AsNoTracking for Read-Only Queries

```csharp
// ❌ BAD: Change tracking overhead for read-only data
var books = _context.Books.ToList();  // Tracked!
// Memory overhead for change tracking even though we won't modify data

// ✅ GOOD: Disable tracking for read-only queries
var books = _context.Books.AsNoTracking().ToList();
// 30-40% performance improvement for large result sets
```

### Anti-Pattern 4: Multiple DbContext Instances

```csharp
// ❌ BAD: Create multiple contexts
using (var context1 = new LibraryDbContext())
{
    var book = context1.Books.First(b => b.Id == 1);

    using (var context2 = new LibraryDbContext())
    {
        var category = context2.Categories.First(c => c.Id == book.CategoryId);
        book.Category = category;  // ERROR: Category tracked by context2, not context1!
    }
}

// ✅ GOOD: Use single context per unit of work
using (var context = new LibraryDbContext())
{
    var book = context.Books
        .Include(b => b.Category)
        .First(b => b.Id == 1);
    // Everything tracked by same context
}
```

### Anti-Pattern 5: Ignoring Migration Conflicts

```bash
# ❌ BAD: Ignore merge conflicts in migrations
$ git merge feature-branch
# Conflict in LibraryDbContextModelSnapshot.cs
$ git checkout --theirs LibraryDbContextModelSnapshot.cs  # Just take theirs
$ git commit
# Database schema now inconsistent with code!

# ✅ GOOD: Resolve properly
$ git merge feature-branch
# Conflict in LibraryDbContextModelSnapshot.cs
# Manually resolve to include both changes
$ dotnet ef migrations add MergeMigration
$ dotnet ef database update
# Schema consistent with code
```

### Anti-Pattern 6: Hardcoding Connection Strings

```csharp
// ❌ BAD: Hardcoded connection string
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseSqlServer(
        "Server=localhost;Database=LibraryDb;User=sa;Password=MyPassword123"
    );
}

// ✅ GOOD: Configuration from settings
public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }
}

// In Program.cs:
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LibraryDb"))
);

// In appsettings.json:
{
  "ConnectionStrings": {
    "LibraryDb": "Server=localhost;Database=LibraryDb;..."
  }
}
```

---

## Decision Guide: When to Use Code-First

### Use Code-First When:

1. **Starting a new project** - No existing database
2. **Domain-Driven Design** - Domain model is central, database is implementation detail
3. **Team is more comfortable with C#** - Developers prefer C# over SQL
4. **Need rapid iteration** - Frequent schema changes during development
5. **Cross-database portability** - Same entities on SQL Server, PostgreSQL, SQLite
6. **Strong type safety required** - Schema changes must cause compile errors
7. **CI/CD pipelines** - Automated database updates as part of deployment

### Use Database-First When:

1. **Existing database** - Legacy database with complex schema
2. **DBA-controlled schema** - DBAs require full control over SQL
3. **Performance critical** - Need hand-tuned indexes, partitions, stored procedures
4. **Regulatory compliance** - Schema changes must be reviewed as SQL scripts
5. **Complex SQL features** - Heavy use of triggers, functions, complex views
6. **Multiple applications** - Database shared by .NET, Java, Python apps

### Hybrid Approach:

You can combine both!

```
Development: Code-First
─────────────────────────
- Use Code-First during development
- Generate migrations
- Rapid iteration

Production: Database-First
───────────────────────────
- Generate SQL script from migration: dotnet ef migrations script
- DBA reviews and approves SQL
- DBA runs SQL script on production
- Best of both worlds!
```

---

## Conclusion

Entity Framework Code-First is a powerful approach for modern .NET applications. Key takeaways:

**Core Concepts:**
- Entities are the source of truth
- Migrations manage schema evolution
- Fluent API provides full configuration control
- Navigation properties simplify relationships

**Best Practices:**
- Use separate configuration classes (IEntityTypeConfiguration<T>)
- Initialize collections in entities
- Use async methods for I/O
- Choose appropriate seeding strategy
- Review and customize generated migrations
- Use meaningful migration names

**Common Mistakes to Avoid:**
- N+1 query problem
- Retrieving too much data
- Not using AsNoTracking for read-only queries
- Hardcoding connection strings
- Ignoring migration merge conflicts

**When to Use Code-First:**
- Greenfield projects
- Domain-driven design
- Team prefers C# over SQL
- Rapid prototyping needs

**Further Reading:**
- EF Core documentation: https://docs.microsoft.com/ef/core
- GitHub issues and discussions: https://github.com/dotnet/efcore
- EF Core triage meetings: https://github.com/dotnet/efcore/wiki/Triage-Meetings

Happy coding with EF Core Code-First! 🚀
