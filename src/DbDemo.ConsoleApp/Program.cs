using DbDemo.Demos;
using DbDemo.Infrastructure.Migrations;
using DbDemo.Application.Repositories;
using DbDemo.Domain.Entities;
using DbDemo.Application.DTOs;
using DbDemo.ConsoleApp.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace DbDemo.ConsoleApp.ConsoleApp;

/// <summary>
/// Library Management System - Multi-Provider Demo Application
/// This application demonstrates working with databases using three different approaches:
/// 1. ADO.NET (raw SQL)
/// 2. SqlKata (query builder)
/// 3. Entity Framework Core (ORM)
/// </summary>
internal class Program
{
    private static IConfiguration? _configuration;
    private static string? _connectionString;
    private static RepositoryFactory? _repositoryFactory;
    private static RepositoryProvider _selectedProvider = RepositoryProvider.AdoNet;

    // Repository instances (created by factory)
    private static IBookRepository? _bookRepository;
    private static IAuthorRepository? _authorRepository;
    private static IMemberRepository? _memberRepository;
    private static ILoanRepository? _loanRepository;
    private static ICategoryRepository? _categoryRepository;
    private static IBookAuditRepository? _bookAuditRepository;
    private static ISystemStatisticsRepository? _systemStatisticsRepository;

    static async Task Main(string[] args)
    {
        Console.WriteLine("=======================================================");
        Console.WriteLine("Library Management System - Multi-Provider Demo");
        Console.WriteLine("Demonstrating: ADO.NET | SqlKata | Entity Framework");
        Console.WriteLine("=======================================================");
        Console.WriteLine();

        // Parse command-line arguments for provider selection
        ParseProviderArgument(args);

        // Load configuration from multiple sources
        LoadConfiguration();

        // Display configuration information
        DisplayConfigurationInfo();

        // Run database migrations
        await RunMigrationsAsync();

        Console.WriteLine();

        // Allow user to select provider if not specified via command line
        if (!args.Any(a => a.StartsWith("--provider=")) && !args.Contains("--run-demos"))
        {
            SelectProvider();
        }

        // Initialize repositories with selected provider
        InitializeRepositories();

        // Handle command-line arguments
        if (args.Contains("--run-demos") || args.Contains("--demos"))
        {
            // Run automated demos directly
            await RunAllDemosAsync();
        }
        else if (args.Length == 0 || !args.Contains("--no-wait"))
        {
            // Run interactive menu
            await RunInteractiveMenuAsync();
        }
    }

    /// <summary>
    /// Parses command-line arguments to determine repository provider.
    /// Usage: --provider=adonet|sqlkata|efcore
    /// </summary>
    private static void ParseProviderArgument(string[] args)
    {
        var providerArg = args.FirstOrDefault(a => a.StartsWith("--provider="));
        if (providerArg != null)
        {
            var providerName = providerArg.Split('=')[1].ToLowerInvariant();
            _selectedProvider = providerName switch
            {
                "adonet" or "ado" => RepositoryProvider.AdoNet,
                "sqlkata" or "kata" => RepositoryProvider.SqlKata,
                "efcore" or "ef" => RepositoryProvider.EFCore,
                _ => RepositoryProvider.AdoNet
            };

            Console.WriteLine($"📌 Provider selected via command line: {_selectedProvider}");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Allows user to interactively select the repository provider.
    /// </summary>
    private static void SelectProvider()
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║            SELECT REPOSITORY PROVIDER                          ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("1. ADO.NET (Raw SQL)");
        Console.WriteLine("   ✓ Maximum control and performance");
        Console.WriteLine("   ✓ Direct SQL queries with SqlCommand/SqlDataReader");
        Console.WriteLine("   ✓ All repositories fully implemented");
        Console.WriteLine();
        Console.WriteLine("2. SqlKata Query Builder");
        Console.WriteLine("   ✓ Fluent API for building queries");
        Console.WriteLine("   ✓ Type-safe query construction");
        Console.WriteLine("   ⚠️  BookRepository implemented (demo)");
        Console.WriteLine();
        Console.WriteLine("3. Entity Framework Core (ORM)");
        Console.WriteLine("   ✓ LINQ queries with expression trees");
        Console.WriteLine("   ✓ Automatic change tracking");
        Console.WriteLine("   ⚠️  Book/Author/Member repositories implemented (demo)");
        Console.WriteLine();
        Console.Write("Enter your choice (1-3) [default: 1]: ");

        var input = Console.ReadLine();
        _selectedProvider = input?.Trim() switch
        {
            "2" => RepositoryProvider.SqlKata,
            "3" => RepositoryProvider.EFCore,
            _ => RepositoryProvider.AdoNet
        };

        Console.WriteLine();
    }

    /// <summary>
    /// Loads configuration from multiple sources in priority order:
    /// 1. .env file (loads into environment variables - SINGLE SOURCE OF TRUTH for secrets)
    /// 2. appsettings.json (base configuration with placeholders)
    /// 3. appsettings.{Environment}.json (environment-specific settings)
    /// 4. Environment Variables (for production/CI environments)
    /// </summary>
    private static void LoadConfiguration()
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

        // Determine the base path for configuration files
        // When running with 'dotnet run', working directory is the project directory
        // When running the compiled executable, working directory is the bin directory
        var basePath = Directory.GetCurrentDirectory();

        // If appsettings.json doesn't exist in current directory, look in the repository root
        if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
        {
            // Try going up to find the repository root (where appsettings.json should be)
            var repoRoot = basePath;
            for (int i = 0; i < 5; i++) // Try up to 5 levels up
            {
                var parentDir = Directory.GetParent(repoRoot);
                if (parentDir == null) break;

                repoRoot = parentDir.FullName;
                if (File.Exists(Path.Combine(repoRoot, "appsettings.json")))
                {
                    basePath = repoRoot;
                    break;
                }
            }
        }

        // Load .env file from repository root (loads variables into environment)
        var envFile = Path.Combine(basePath, ".env");
        if (File.Exists(envFile))
        {
            DotNetEnv.Env.Load(envFile);
            Console.WriteLine($"✅ Loaded environment variables from .env file");
        }

        _configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()  // Environment variables (includes those loaded from .env)
            .Build();

        // Expand environment variables in connection strings
        ExpandConnectionStrings();

        Console.WriteLine($"✅ Configuration loaded (Environment: {environment})");
    }

    /// <summary>
    /// Expands environment variables in configuration values (${VAR} syntax)
    /// </summary>
    private static void ExpandConnectionStrings()
    {
        if (_configuration == null) return;

        // Expand ConnectionStrings section
        var connectionStrings = _configuration.GetSection("ConnectionStrings");
        foreach (var conn in connectionStrings.GetChildren())
        {
            ExpandConfigValue(conn);
        }

        // Expand Database section
        var database = _configuration.GetSection("Database");
        foreach (var dbConfig in database.GetChildren())
        {
            ExpandConfigValue(dbConfig);
        }
    }

    /// <summary>
    /// Expands environment variables in a single configuration value
    /// </summary>
    private static void ExpandConfigValue(IConfigurationSection configSection)
    {
        var value = configSection.Value;
        if (string.IsNullOrEmpty(value)) return;

        // Replace ${VAR} with environment variable values
        var expanded = System.Text.RegularExpressions.Regex.Replace(
            value,
            @"\$\{([^}]+)\}",
            match =>
            {
                var varName = match.Groups[1].Value;
                return Environment.GetEnvironmentVariable(varName) ?? match.Value;
            });

        // Update the configuration value
        if (expanded != value)
        {
            _configuration![configSection.Path] = expanded;
        }
    }

    /// <summary>
    /// Displays configuration information (safely, without exposing passwords!)
    /// </summary>
    private static void DisplayConfigurationInfo()
    {
        if (_configuration == null)
        {
            Console.WriteLine("❌ Configuration not loaded!");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("📋 Configuration Summary:");
        Console.WriteLine("─────────────────────────────────────────");

        // Get connection strings
        var adminConnectionString = _configuration.GetConnectionString("SqlServerAdmin");
        var appConnectionString = _configuration.GetConnectionString("LibraryDb");

        // Display connection string info (masked passwords!)
        Console.WriteLine();
        Console.WriteLine("🔌 Connection Strings:");
        Console.WriteLine($"   Admin (SA): {MaskPassword(adminConnectionString)}");
        Console.WriteLine($"   App User:   {MaskPassword(appConnectionString)}");

        // Display database settings
        Console.WriteLine();
        Console.WriteLine("⚙️  Database Settings:");
        Console.WriteLine($"   Migrations Path: {_configuration["Database:MigrationsPath"]}");
        Console.WriteLine($"   Command Timeout: {_configuration["Database:CommandTimeout"]}s");
        Console.WriteLine($"   Retry Enabled:   {_configuration["Database:EnableRetryOnFailure"]}");

        // Check if passwords are configured
        Console.WriteLine();
        Console.WriteLine("🔒 Security Check:");
        var adminHasPassword = adminConnectionString?.Contains("Password=", StringComparison.OrdinalIgnoreCase) ?? false;
        var appHasPassword = appConnectionString?.Contains("Password=", StringComparison.OrdinalIgnoreCase) ?? false;

        if (!adminHasPassword)
        {
            Console.WriteLine("   ⚠️  WARNING: Admin connection string has no password!");
            Console.WriteLine("      Run: dotnet user-secrets set \"ConnectionStrings:SqlServerAdmin\" \"Server=localhost,1453;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;\"");
        }
        else
        {
            Console.WriteLine("   ✅ Admin password configured");
        }

        if (!appHasPassword)
        {
            Console.WriteLine("   ⚠️  WARNING: App connection string has no password!");
            Console.WriteLine("      Run: dotnet user-secrets set \"ConnectionStrings:LibraryDb\" \"Server=localhost,1453;Database=LibraryDb;User Id=library_app_user;Password=YOUR_PASSWORD;TrustServerCertificate=True;\"");
        }
        else
        {
            Console.WriteLine("   ✅ App user password configured");
        }

        Console.WriteLine("─────────────────────────────────────────");
    }

    /// <summary>
    /// Masks the password in a connection string for safe display
    /// </summary>
    /// <param name="connectionString">The connection string to mask</param>
    /// <returns>Connection string with password replaced by asterisks</returns>
    private static string MaskPassword(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return "[NOT CONFIGURED]";
        }

        // Simple password masking - replace password value with asterisks
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var masked = parts.Select(part =>
        {
            if (part.Trim().StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
            {
                return "Password=********";
            }
            return part.Trim();
        });

        return string.Join("; ", masked);
    }

    /// <summary>
    /// Gets the configuration instance (for use in other parts of the application)
    /// </summary>
    public static IConfiguration Configuration => _configuration
        ?? throw new InvalidOperationException("Configuration not loaded. Call LoadConfiguration() first.");

    /// <summary>
    /// Runs database migrations using the MigrationRunner
    /// </summary>
    private static async Task RunMigrationsAsync()
    {
        if (_configuration == null)
        {
            Console.WriteLine("❌ Configuration not loaded - cannot run migrations!");
            Console.WriteLine("Application will exit.");
            Environment.Exit(1);
        }

        try
        {
            // Get admin connection string (SA user) - required for running migrations
            var adminConnectionString = _configuration.GetConnectionString("SqlServerAdmin");

            if (string.IsNullOrEmpty(adminConnectionString))
            {
                Console.WriteLine("❌ No admin connection string configured - cannot run migrations");
                Console.WriteLine("   Set ConnectionStrings:SqlServerAdmin in appsettings.json or user secrets");
                Console.WriteLine("Application will exit.");
                Environment.Exit(1);
            }

            // Get migrations path from configuration
            var migrationsPath = _configuration["Database:MigrationsPath"] ?? "../../../../migrations";

            // Get database name from configuration (defaults to LibraryDb)
            var databaseName = _configuration["Database:Name"] ?? "LibraryDb";

            // Create and run migration runner
            var runner = new MigrationRunner(adminConnectionString, migrationsPath, databaseName);
            var executedCount = await runner.RunMigrationsAsync();

            if (executedCount > 0)
            {
                Console.WriteLine($"✅ Database schema is now up to date!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("❌ Migration failed!");
            Console.WriteLine($"   Error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Application will exit.");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Initializes the repositories using the selected provider.
    /// </summary>
    private static void InitializeRepositories()
    {
        if (_configuration == null)
        {
            Console.WriteLine("❌ Configuration not loaded - cannot initialize repositories!");
            return;
        }

        try
        {
            var appConnectionString = _configuration.GetConnectionString("LibraryDb");

            if (string.IsNullOrEmpty(appConnectionString))
            {
                Console.WriteLine("⚠️  No application connection string configured!");
                Console.WriteLine("   Set ConnectionStrings:LibraryDb in appsettings.json or user secrets");
                return;
            }

            _connectionString = appConnectionString;

            // Create factory with selected provider
            _repositoryFactory = new RepositoryFactory(_selectedProvider, _connectionString);

            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  Repository Provider: {_repositoryFactory.ProviderName,-42} ║");
            Console.WriteLine("╠════════════════════════════════════════════════════════════════╣");
            Console.WriteLine($"  {_repositoryFactory.ProviderDescription}");
            Console.WriteLine($"  Status: {_repositoryFactory.GetImplementationStatus()}");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Create repository instances using factory
            try
            {
                _bookRepository = _repositoryFactory.CreateBookRepository();
                Console.WriteLine("  ✅ BookRepository initialized");
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine($"  ⚠️  BookRepository: {ex.Message}");
            }

            try
            {
                _authorRepository = _repositoryFactory.CreateAuthorRepository();
                Console.WriteLine("  ✅ AuthorRepository initialized");
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine($"  ⚠️  AuthorRepository: {ex.Message}");
            }

            try
            {
                _memberRepository = _repositoryFactory.CreateMemberRepository();
                Console.WriteLine("  ✅ MemberRepository initialized");
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine($"  ⚠️  MemberRepository: {ex.Message}");
            }

            try
            {
                _loanRepository = _repositoryFactory.CreateLoanRepository();
                Console.WriteLine("  ✅ LoanRepository initialized");
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine($"  ⚠️  LoanRepository: {ex.Message}");
            }

            try
            {
                _categoryRepository = _repositoryFactory.CreateCategoryRepository();
                Console.WriteLine("  ✅ CategoryRepository initialized");
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine($"  ⚠️  CategoryRepository: {ex.Message}");
            }

            try
            {
                _bookAuditRepository = _repositoryFactory.CreateBookAuditRepository();
                Console.WriteLine("  ✅ BookAuditRepository initialized");
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine($"  ⚠️  BookAuditRepository: {ex.Message}");
            }

            try
            {
                _systemStatisticsRepository = _repositoryFactory.CreateSystemStatisticsRepository();
                Console.WriteLine("  ✅ SystemStatisticsRepository initialized");
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine($"  ⚠️  SystemStatisticsRepository: {ex.Message}");
            }

            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Repository initialization error: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes a repository operation within a transaction
    /// </summary>
    private static async Task<T> WithTransactionAsync<T>(Func<Microsoft.Data.SqlClient.SqlTransaction, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (_connectionString == null)
            throw new InvalidOperationException("Connection string not initialized");

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (Microsoft.Data.SqlClient.SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(transaction);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Executes a repository operation within a transaction (no return value)
    /// </summary>
    private static async Task WithTransactionAsync(Func<Microsoft.Data.SqlClient.SqlTransaction, Task> operation, CancellationToken cancellationToken = default)
    {
        if (_connectionString == null)
            throw new InvalidOperationException("Connection string not initialized");

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (Microsoft.Data.SqlClient.SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await operation(transaction);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    #region Interactive Menu

    /// <summary>
    /// Runs the interactive menu loop
    /// </summary>
    private static async Task RunInteractiveMenuAsync()
    {
        if (_bookRepository == null)
        {
            Console.WriteLine("❌ Repository not initialized - cannot run interactive menu");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        var running = true;

        while (running)
        {
            try
            {
                DisplayMainMenu();
                var choice = GetUserChoice();

                Console.WriteLine();

                switch (choice)
                {
                    case 1:
                        await ListBooksAsync();
                        break;
                    case 2:
                        await AddBookAsync();
                        break;
                    case 3:
                        await ViewBookDetailsAsync();
                        break;
                    case 4:
                        await UpdateBookAsync();
                        break;
                    case 5:
                        await DeleteBookAsync();
                        break;
                    case 6:
                        await SearchBooksAsync();
                        break;
                    case 7:
                        await RunDemoMenuAsync();
                        break;
                    case 0:
                        Console.WriteLine("Thank you for using the Library Management System!");
                        running = false;
                        break;
                    default:
                        Console.WriteLine("❌ Invalid choice. Please try again.");
                        break;
                }

                if (running && choice != 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ An error occurred: {ex.Message}");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Displays the main menu
    /// </summary>
    private static void DisplayMainMenu()
    {
        Console.Clear();
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║      LIBRARY MANAGEMENT SYSTEM         ║");
        Console.WriteLine("║           Book Management              ║");
        Console.WriteLine("╚════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("1. List All Books");
        Console.WriteLine("2. Add New Book");
        Console.WriteLine("3. View Book Details");
        Console.WriteLine("4. Update Book");
        Console.WriteLine("5. Delete Book");
        Console.WriteLine("6. Search Books by Title");
        Console.WriteLine("7. Run Automated Demos");
        Console.WriteLine("0. Exit");
        Console.WriteLine();
        Console.Write("Enter your choice: ");
    }

    /// <summary>
    /// Gets and validates user menu choice
    /// </summary>
    private static int GetUserChoice()
    {
        var input = Console.ReadLine();
        return int.TryParse(input, out var choice) ? choice : -1;
    }

    #endregion

    #region Book CRUD Operations

    /// <summary>
    /// Lists all books with pagination
    /// </summary>
    private static async Task ListBooksAsync()
    {
        Console.WriteLine("📚 Book List");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════");

        try
        {
            await WithTransactionAsync(async (tx) =>
            {
                var pageNumber = 1;
                var pageSize = 10;

                var books = await _bookRepository!.GetPagedAsync(pageNumber, pageSize, false, tx);
                var totalCount = await _bookRepository.GetCountAsync(false, tx);

                if (books.Count == 0)
                {
                    Console.WriteLine("No books found in the library.");
                    return;
                }

                Console.WriteLine($"Showing {books.Count} of {totalCount} books (Page {pageNumber})");
                Console.WriteLine();

                foreach (var book in books)
                {
                    DisplayBookSummary(book);
                }

                Console.WriteLine("═══════════════════════════════════════════════════════════════════════════");
                Console.WriteLine($"Total: {totalCount} books");
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error listing books: {ex.Message}");
        }
    }

    /// <summary>
    /// Adds a new book to the library
    /// </summary>
    private static async Task AddBookAsync()
    {
        Console.WriteLine("➕ Add New Book");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════");

        try
        {
            // Collect book information
            var isbn = PromptForString("ISBN (10 or 13 digits, may include dashes)");
            var title = PromptForString("Title");
            var categoryId = PromptForInt("Category ID");
            var totalCopies = PromptForInt("Total Copies");

            // Optional fields
            Console.WriteLine();
            Console.WriteLine("Optional information (press Enter to skip):");
            var subtitle = PromptForOptionalString("Subtitle");
            var description = PromptForOptionalString("Description");
            var publisher = PromptForOptionalString("Publisher");
            var pageCount = PromptForOptionalInt("Page Count");
            var language = PromptForOptionalString("Language");
            var shelfLocation = PromptForOptionalString("Shelf Location");

            DateTime? publishedDate = null;
            Console.Write("Published Date (yyyy-MM-dd, or Enter to skip): ");
            var dateInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(dateInput) && DateTime.TryParse(dateInput, out var parsedDate))
            {
                publishedDate = parsedDate;
            }

            // Create the book
            var book = new Book(isbn, title, categoryId, totalCopies);

            // Update optional details if provided
            if (!string.IsNullOrWhiteSpace(subtitle) || !string.IsNullOrWhiteSpace(description) || !string.IsNullOrWhiteSpace(publisher))
            {
                book.UpdateDetails(title, subtitle, description, publisher);
            }

            if (publishedDate.HasValue || pageCount.HasValue || !string.IsNullOrWhiteSpace(language))
            {
                book.UpdatePublishingInfo(publishedDate, pageCount, language);
            }

            if (!string.IsNullOrWhiteSpace(shelfLocation))
            {
                book.UpdateShelfLocation(shelfLocation);
            }

            // Save to database
            var created = await WithTransactionAsync(tx => _bookRepository!.CreateAsync(book, tx));

            Console.WriteLine();
            Console.WriteLine("✅ Book added successfully!");
            Console.WriteLine();
            DisplayBookDetails(created);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error adding book: {ex.Message}");
        }
    }

    /// <summary>
    /// Views detailed information about a specific book
    /// </summary>
    private static async Task ViewBookDetailsAsync()
    {
        Console.WriteLine("🔍 View Book Details");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════");

        try
        {
            var id = PromptForInt("Enter Book ID");

            var book = await WithTransactionAsync(tx => _bookRepository!.GetByIdAsync(id, tx));

            if (book == null)
            {
                Console.WriteLine($"❌ Book with ID {id} not found.");
                return;
            }

            Console.WriteLine();
            DisplayBookDetails(book);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error retrieving book: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing book's information
    /// </summary>
    private static async Task UpdateBookAsync()
    {
        Console.WriteLine("✏️  Update Book");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════");

        try
        {
            var id = PromptForInt("Enter Book ID to update");

            await WithTransactionAsync(async (tx) =>
            {
                var book = await _bookRepository!.GetByIdAsync(id, tx);

                if (book == null)
                {
                    Console.WriteLine($"❌ Book with ID {id} not found.");
                    return;
                }

                Console.WriteLine();
                Console.WriteLine("Current book information:");
                DisplayBookDetails(book);

                Console.WriteLine();
                Console.WriteLine("Enter new values (press Enter to keep current value):");

                var title = PromptForOptionalString($"Title [{book.Title}]");
                var subtitle = PromptForOptionalString($"Subtitle [{book.Subtitle ?? "none"}]");
                var description = PromptForOptionalString($"Description [{book.Description ?? "none"}]");
                var publisher = PromptForOptionalString($"Publisher [{book.Publisher ?? "none"}]");

                // Update the book
                book.UpdateDetails(
                    string.IsNullOrWhiteSpace(title) ? book.Title : title,
                    string.IsNullOrWhiteSpace(subtitle) ? book.Subtitle : subtitle,
                    string.IsNullOrWhiteSpace(description) ? book.Description : description,
                    string.IsNullOrWhiteSpace(publisher) ? book.Publisher : publisher
                );

                var updated = await _bookRepository.UpdateAsync(book, tx);

                if (updated)
                {
                    Console.WriteLine();
                    Console.WriteLine("✅ Book updated successfully!");
                    Console.WriteLine();

                    // Fetch and display updated book
                    var refreshedBook = await _bookRepository.GetByIdAsync(id, tx);
                    if (refreshedBook != null)
                    {
                        DisplayBookDetails(refreshedBook);
                    }
                }
                else
                {
                    Console.WriteLine("❌ Failed to update book.");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error updating book: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a book from the library (soft delete)
    /// </summary>
    private static async Task DeleteBookAsync()
    {
        Console.WriteLine("🗑️  Delete Book");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════");

        try
        {
            var id = PromptForInt("Enter Book ID to delete");

            await WithTransactionAsync(async (tx) =>
            {
                var book = await _bookRepository!.GetByIdAsync(id, tx);

                if (book == null)
                {
                    Console.WriteLine($"❌ Book with ID {id} not found.");
                    return;
                }

                Console.WriteLine();
                Console.WriteLine("Book to delete:");
                DisplayBookSummary(book);

                Console.WriteLine();
                Console.Write("Are you sure you want to delete this book? (y/n): ");
                var confirmation = Console.ReadLine()?.Trim().ToLower();

                if (confirmation != "y" && confirmation != "yes")
                {
                    Console.WriteLine("❌ Deletion cancelled.");
                    return;
                }

                var deleted = await _bookRepository.DeleteAsync(id, tx);

                if (deleted)
                {
                    Console.WriteLine("✅ Book deleted successfully!");
                }
                else
                {
                    Console.WriteLine("❌ Failed to delete book.");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error deleting book: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches for books by title
    /// </summary>
    private static async Task SearchBooksAsync()
    {
        Console.WriteLine("🔎 Search Books");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════");

        try
        {
            var searchTerm = PromptForString("Enter search term");

            await WithTransactionAsync(async (tx) =>
            {
                var books = await _bookRepository!.SearchByTitleAsync(searchTerm, tx);

                if (books.Count == 0)
                {
                    Console.WriteLine($"No books found matching '{searchTerm}'.");
                    return;
                }

                Console.WriteLine();
                Console.WriteLine($"Found {books.Count} book(s) matching '{searchTerm}':");
                Console.WriteLine();

                foreach (var book in books)
                {
                    DisplayBookSummary(book);
                }

                Console.WriteLine("═══════════════════════════════════════════════════════════════════════════");
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error searching books: {ex.Message}");
        }
    }

    #endregion

    #region Demo Menu

    /// <summary>
    /// Displays and handles the automated demo menu
    /// </summary>
    private static async Task RunDemoMenuAsync()
    {
        if (!AreAllRepositoriesInitialized())
        {
            Console.WriteLine("❌ Not all repositories are initialized. Cannot run demos.");
            return;
        }

        var running = true;

        while (running)
        {
            try
            {
                DisplayDemoMenu();
                var choice = GetUserChoice();

                Console.WriteLine();

                if (choice == 0)
                {
                    running = false;
                    continue;
                }

                var demoRunner = new DemoRunner(
                    _bookRepository!,
                    _authorRepository!,
                    _memberRepository!,
                    _loanRepository!,
                    _categoryRepository!,
                    _bookAuditRepository!,
                    _systemStatisticsRepository!,
                    _connectionString!,
                    withDelays: true
                );

                switch (choice)
                {
                    case 1:
                        await demoRunner.RunBasicBookManagementAsync();
                        break;
                    case 2:
                        await demoRunner.RunAuthorManagementAsync();
                        break;
                    case 3:
                        await demoRunner.RunMemberManagementAsync();
                        break;
                    case 4:
                        await demoRunner.RunCompleteLoanWorkflowAsync();
                        break;
                    case 5:
                        await demoRunner.RunOverdueLoanScenarioAsync();
                        break;
                    case 6:
                        await demoRunner.RunLoanRenewalAsync();
                        break;
                    case 7:
                        await demoRunner.RunConnectionPoolingAsync();
                        break;
                    case 8:
                        await demoRunner.RunBulkOperationsAsync();
                        break;
                    case 9:
                        await demoRunner.RunBookAuditTrailAsync();
                        break;
                    case 10:
                        await demoRunner.RunOverdueLoansReportAsync();
                        break;
                    case 11:
                        await demoRunner.RunStatisticsAnalyticsAsync();
                        break;
                    case 99:
                        await demoRunner.RunAllScenariosAsync();
                        break;
                    default:
                        Console.WriteLine("❌ Invalid choice. Please try again.");
                        break;
                }

                if (running && choice != 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Press any key to return to demo menu...");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Demo error: {ex.Message}");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Runs all automated demos without interactive prompts
    /// </summary>
    private static async Task RunAllDemosAsync()
    {
        if (!AreAllRepositoriesInitialized())
        {
            Console.WriteLine("❌ Not all repositories are initialized. Cannot run demos.");
            return;
        }

        try
        {
            var demoRunner = new DemoRunner(
                _bookRepository!,
                _authorRepository!,
                _memberRepository!,
                _loanRepository!,
                _categoryRepository!,
                _bookAuditRepository!,
                _systemStatisticsRepository!,
                _connectionString!,
                withDelays: false  // Disable delays for faster automated execution
            );

            Console.WriteLine("🚀 Running all automated demos...");
            Console.WriteLine();

            await demoRunner.RunAllScenariosAsync();

            Console.WriteLine();
            Console.WriteLine("✅ All automated demos completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Demo error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.ExitCode = 1;
        }
    }

    /// <summary>
    /// Displays the demo menu
    /// </summary>
    private static void DisplayDemoMenu()
    {
        Console.Clear();
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║         AUTOMATED DEMO SCENARIOS       ║");
        Console.WriteLine("╚════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("1. Basic Book Management");
        Console.WriteLine("2. Author Management");
        Console.WriteLine("3. Member Management");
        Console.WriteLine("4. Complete Loan Workflow (Happy Path)");
        Console.WriteLine("5. Overdue Loan Scenario");
        Console.WriteLine("6. Loan Renewal");
        Console.WriteLine("7. Connection Pooling Performance Demo");
        Console.WriteLine("8. Bulk Operations Performance Demo");
        Console.WriteLine("9. Book Audit Trail");
        Console.WriteLine("10. Overdue Loans Report");
        Console.WriteLine("11. Statistics & Analytics");
        Console.WriteLine("99. Run ALL Scenarios");
        Console.WriteLine("0. Back to Main Menu");
        Console.WriteLine();
        Console.Write("Enter your choice: ");
    }

    /// <summary>
    /// Checks if all repositories are initialized
    /// </summary>
    private static bool AreAllRepositoriesInitialized()
    {
        return _bookRepository != null &&
               _authorRepository != null &&
               _memberRepository != null &&
               _loanRepository != null &&
               _categoryRepository != null;
    }

    #endregion

    #region Helper Methods - Display

    /// <summary>
    /// Displays a summary of a book (one-line format)
    /// </summary>
    private static void DisplayBookSummary(Book book)
    {
        var status = book.IsAvailable ? "✓ Available" : "✗ Not Available";
        var availability = $"({book.AvailableCopies}/{book.TotalCopies})";

        Console.WriteLine($"[{book.Id,4}] {book.Title,-40} | ISBN: {book.ISBN,-15} | {status} {availability}");
    }

    /// <summary>
    /// Displays detailed information about a book
    /// </summary>
    private static void DisplayBookDetails(Book book)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"  ID:              {book.Id}");
        Console.WriteLine($"  Title:           {book.Title}");

        if (!string.IsNullOrWhiteSpace(book.Subtitle))
            Console.WriteLine($"  Subtitle:        {book.Subtitle}");

        Console.WriteLine($"  ISBN:            {book.ISBN}");
        Console.WriteLine($"  Category ID:     {book.CategoryId}");

        if (!string.IsNullOrWhiteSpace(book.Publisher))
            Console.WriteLine($"  Publisher:       {book.Publisher}");

        if (book.PublishedDate.HasValue)
            Console.WriteLine($"  Published:       {book.PublishedDate.Value:yyyy-MM-dd}");

        if (book.PageCount.HasValue)
            Console.WriteLine($"  Pages:           {book.PageCount}");

        if (!string.IsNullOrWhiteSpace(book.Language))
            Console.WriteLine($"  Language:        {book.Language}");

        if (!string.IsNullOrWhiteSpace(book.ShelfLocation))
            Console.WriteLine($"  Shelf Location:  {book.ShelfLocation}");

        Console.WriteLine("  ───────────────────────────────────────────────────────────────────────────");
        Console.WriteLine($"  Total Copies:    {book.TotalCopies}");
        Console.WriteLine($"  Available:       {book.AvailableCopies}");
        Console.WriteLine($"  On Loan:         {book.CopiesOnLoan}");
        Console.WriteLine($"  Status:          {(book.IsAvailable ? "✓ Available" : "✗ Not Available")}");
        Console.WriteLine("  ───────────────────────────────────────────────────────────────────────────");

        if (!string.IsNullOrWhiteSpace(book.Description))
        {
            Console.WriteLine($"  Description:");
            Console.WriteLine($"  {book.Description}");
            Console.WriteLine("  ───────────────────────────────────────────────────────────────────────────");
        }

        Console.WriteLine($"  Created:         {book.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"  Last Updated:    {book.UpdatedAt:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"  Deleted:         {(book.IsDeleted ? "Yes" : "No")}");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════════════════╝");
    }

    #endregion

    #region Helper Methods - Input

    /// <summary>
    /// Prompts for a required string input
    /// </summary>
    private static string PromptForString(string prompt)
    {
        while (true)
        {
            Console.Write($"{prompt}: ");
            var input = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(input))
            {
                return input.Trim();
            }

            Console.WriteLine("❌ This field is required. Please enter a value.");
        }
    }

    /// <summary>
    /// Prompts for an optional string input
    /// </summary>
    private static string? PromptForOptionalString(string prompt)
    {
        Console.Write($"{prompt}: ");
        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? null : input.Trim();
    }

    /// <summary>
    /// Prompts for a required integer input
    /// </summary>
    private static int PromptForInt(string prompt)
    {
        while (true)
        {
            Console.Write($"{prompt}: ");
            var input = Console.ReadLine();

            if (int.TryParse(input, out var value))
            {
                return value;
            }

            Console.WriteLine("❌ Please enter a valid number.");
        }
    }

    /// <summary>
    /// Prompts for an optional integer input
    /// </summary>
    private static int? PromptForOptionalInt(string prompt)
    {
        Console.Write($"{prompt}: ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        return int.TryParse(input, out var value) ? value : null;
    }

    #endregion
}
