using Microsoft.EntityFrameworkCore;
using DbDemo.Infrastructure.EFCore.CodeFirst.Entities;
using DbDemo.Infrastructure.EFCore.CodeFirst.Configuration;

namespace DbDemo.Infrastructure.EFCore.CodeFirst;

/// <summary>
/// DbContext for Code-First approach.
///
/// CODE-FIRST PATTERN: DbContext as schema definition
/// ===================================================
///
/// In Code-First, the DbContext and entity classes define the database schema.
/// EF Core generates migrations from this definition.
///
/// WORKFLOW:
/// 1. Define entities (Book, Author, Category, BookAuthor)
/// 2. Configure relationships via Fluent API
/// 3. Run: dotnet ef migrations add InitialCreate
/// 4. Review generated migration code
/// 5. Run: dotnet ef database update
/// 6. Database schema created from C# code!
///
/// COMPARISON TO DATABASE-FIRST:
/// - Database-First: SQL scripts → Database → Scaffold → EF entities
/// - Code-First: EF entities → Migrations → Database  ← THIS APPROACH
///
/// KEY FEATURES:
/// - Uses separate configuration classes (IEntityTypeConfiguration)
/// - ApplyConfigurationsFromAssembly() applies all configurations automatically
/// - Clean OnModelCreating (just applies configurations and seeding)
/// - External transaction support (same as Database-First implementation)
/// </summary>
public class LibraryCodeFirstDbContext : DbContext
{
    /// <summary>
    /// Constructor for dependency injection with options.
    /// </summary>
    /// <param name="options">DbContext options (connection string, etc.)</param>
    public LibraryCodeFirstDbContext(DbContextOptions<LibraryCodeFirstDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Constructor for testing or manual instantiation.
    /// </summary>
    public LibraryCodeFirstDbContext()
    {
    }

    #region DbSets (Tables)

    /// <summary>
    /// Books table.
    /// </summary>
    public DbSet<Book> Books { get; set; } = null!;

    /// <summary>
    /// Authors table.
    /// </summary>
    public DbSet<Author> Authors { get; set; } = null!;

    /// <summary>
    /// Categories table (lookup).
    /// </summary>
    public DbSet<Category> Categories { get; set; } = null!;

    /// <summary>
    /// BookAuthors junction table (many-to-many).
    /// </summary>
    public DbSet<BookAuthor> BookAuthors { get; set; } = null!;

    #endregion

    /// <summary>
    /// Configures the model using Fluent API.
    ///
    /// CODE-FIRST PATTERN: Centralized configuration
    /// ==============================================
    ///
    /// We use IEntityTypeConfiguration<T> for each entity:
    /// - BookConfiguration
    /// - AuthorConfiguration
    /// - CategoryConfiguration
    /// - BookAuthorConfiguration
    ///
    /// ApplyConfigurationsFromAssembly() automatically finds and applies all
    /// configuration classes in the current assembly.
    ///
    /// BENEFITS:
    /// ✓ Keeps OnModelCreating clean
    /// ✓ Each entity's config in its own file
    /// ✓ Easier to maintain and test
    /// ✓ Better organization for large projects
    /// </summary>
    /// <param name="modelBuilder">Model builder for Fluent API configuration</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from Configuration/ directory
        // This finds all classes implementing IEntityTypeConfiguration<T> and applies them
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryCodeFirstDbContext).Assembly);

        // Data seeding (will be moved to separate seeder class later)
        SeedData(modelBuilder);
    }

    /// <summary>
    /// Seeds initial data.
    ///
    /// CODE-FIRST PATTERN: HasData() for simple seeding
    /// ================================================
    ///
    /// HasData() adds seed data to migrations.
    /// - Data is inserted when migration runs
    /// - Data is tracked by EF (updates in subsequent migrations)
    /// - Good for lookup/reference data
    ///
    /// LIMITATIONS:
    /// - Can't use navigation properties (must use FK values)
    /// - Must provide primary key values
    /// - Complex scenarios better handled with custom seeder
    ///
    /// For complex seeding, see: Seed/LibraryDataSeeder.cs
    /// </summary>
    /// <param name="modelBuilder">Model builder</param>
    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Categories (lookup data)
        modelBuilder.Entity<Category>().HasData(
            new Category
            {
                Id = 1,
                Name = "Fiction",
                Description = "Literary fiction, novels, and short stories",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Category
            {
                Id = 2,
                Name = "Non-Fiction",
                Description = "Biographies, history, science, and more",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Category
            {
                Id = 3,
                Name = "Science Fiction",
                Description = "Sci-fi novels and stories",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Category
            {
                Id = 4,
                Name = "Mystery",
                Description = "Detective stories, thrillers, and mysteries",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Category
            {
                Id = 5,
                Name = "Biography",
                Description = "Life stories of notable people",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed Authors (sample data)
        modelBuilder.Entity<Author>().HasData(
            new Author
            {
                Id = 1,
                FirstName = "Isaac",
                LastName = "Asimov",
                Email = "isaac.asimov@example.com",
                Bio = "American writer and professor of biochemistry, best known for his science fiction works.",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Author
            {
                Id = 2,
                FirstName = "Agatha",
                LastName = "Christie",
                Email = "agatha.christie@example.com",
                Bio = "English writer known for her detective novels featuring Hercule Poirot and Miss Marple.",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Author
            {
                Id = 3,
                FirstName = "J.K.",
                LastName = "Rowling",
                Email = "jk.rowling@example.com",
                Bio = "British author, best known for the Harry Potter fantasy series.",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed Books (sample data)
        modelBuilder.Entity<Book>().HasData(
            new Book
            {
                Id = 1,
                ISBN = "978-0553293357",
                Title = "Foundation",
                Subtitle = "The Foundation Trilogy, Book 1",
                CategoryId = 3, // Science Fiction
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            },
            new Book
            {
                Id = 2,
                ISBN = "978-0062073488",
                Title = "Murder on the Orient Express",
                Subtitle = null,
                CategoryId = 4, // Mystery
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            },
            new Book
            {
                Id = 3,
                ISBN = "978-0439708180",
                Title = "Harry Potter and the Sorcerer's Stone",
                Subtitle = "Harry Potter Series, Book 1",
                CategoryId = 1, // Fiction
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            }
        );

        // Seed BookAuthors (junction table)
        // Note: Can't seed FullName for Author because it's computed - EF will handle it
        modelBuilder.Entity<BookAuthor>().HasData(
            new BookAuthor
            {
                BookId = 1, // Foundation
                AuthorId = 1, // Isaac Asimov
                Role = "Author",
                DisplayOrder = 1,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new BookAuthor
            {
                BookId = 2, // Murder on the Orient Express
                AuthorId = 2, // Agatha Christie
                Role = "Author",
                DisplayOrder = 1,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new BookAuthor
            {
                BookId = 3, // Harry Potter
                AuthorId = 3, // J.K. Rowling
                Role = "Author",
                DisplayOrder = 1,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }

    /// <summary>
    /// Configures database provider (optional - usually configured via DI).
    /// </summary>
    /// <param name="optionsBuilder">Options builder</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Only configure if not already configured (for testing scenarios)
        if (!optionsBuilder.IsConfigured)
        {
            // Default connection string for development
            // In production, configure via DI with proper connection string
            optionsBuilder.UseSqlServer(
                "Server=localhost,1453;Database=LibraryDbCodeFirst;User Id=library_app_user;Password=LibraryApp@2024!;TrustServerCertificate=True;",
                options => options.EnableRetryOnFailure()
            );
        }
    }
}
