using DbDemo.ConsoleApp.Demos;
using DbDemo.ConsoleApp.Infrastructure.Migrations;
using DbDemo.ConsoleApp.Infrastructure.Repositories;
using DbDemo.ConsoleApp.Models;
using Microsoft.Extensions.Configuration;

namespace DbDemo.ConsoleApp.ConsoleApp;

/// <summary>
/// Library Management System - ADO.NET Demo Application
/// This application demonstrates working with databases using ADO.NET
/// </summary>
internal class Program
{
    private static IConfiguration? _configuration;
    private static IBookRepository? _bookRepository;
    private static IAuthorRepository? _authorRepository;
    private static IMemberRepository? _memberRepository;
    private static ILoanRepository? _loanRepository;
    private static ICategoryRepository? _categoryRepository;

    static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("Library Management System - ADO.NET Demo");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        // Load configuration from multiple sources
        LoadConfiguration();

        // Display configuration information
        DisplayConfigurationInfo();

        // Run database migrations
        await RunMigrationsAsync();

        Console.WriteLine();

        // Initialize repository
        InitializeRepository();

        // Run interactive menu (unless --no-wait specified)
        if (args.Length == 0 || !args.Contains("--no-wait"))
        {
            await RunInteractiveMenuAsync();
        }
    }

    /// <summary>
    /// Loads configuration from multiple sources in priority order:
    /// 1. appsettings.json (base configuration)
    /// 2. appsettings.{Environment}.json (environment-specific)
    /// 3. User Secrets (development passwords - never committed)
    /// 4. Environment Variables (production passwords)
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

        _configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<Program>(optional: true)  // Development secrets (not committed to git)
            .AddEnvironmentVariables()                 // Production secrets
            .Build();

        Console.WriteLine($"✅ Configuration loaded (Environment: {environment})");
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
            return;
        }

        try
        {
            // Get admin connection string (SA user) - required for running migrations
            var adminConnectionString = _configuration.GetConnectionString("SqlServerAdmin");

            if (string.IsNullOrEmpty(adminConnectionString))
            {
                Console.WriteLine("⚠️  No admin connection string configured - skipping migrations");
                Console.WriteLine("   Set ConnectionStrings:SqlServerAdmin in appsettings.json or user secrets");
                return;
            }

            // Get migrations path from configuration
            var migrationsPath = _configuration["Database:MigrationsPath"] ?? "../../../../migrations";

            // Create and run migration runner
            var runner = new MigrationRunner(adminConnectionString, migrationsPath);
            var executedCount = await runner.RunMigrationsAsync();

            if (executedCount > 0)
            {
                Console.WriteLine($"✅ Database schema is now up to date!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Migration error: {ex.Message}");
            Console.WriteLine("⚠️  Application will continue, but database may be in an inconsistent state.");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Initializes the repositories with the application connection string
    /// </summary>
    private static void InitializeRepository()
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

            _bookRepository = new BookRepository(appConnectionString);
            _authorRepository = new AuthorRepository(appConnectionString);
            _memberRepository = new MemberRepository(appConnectionString);
            _loanRepository = new LoanRepository(appConnectionString);
            _categoryRepository = new CategoryRepository(appConnectionString);
            Console.WriteLine("✅ All repositories initialized");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Repository initialization error: {ex.Message}");
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
            var pageNumber = 1;
            var pageSize = 10;

            var books = await _bookRepository!.GetPagedAsync(pageNumber, pageSize);
            var totalCount = await _bookRepository.GetCountAsync();

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
            var created = await _bookRepository!.CreateAsync(book);

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

            var book = await _bookRepository!.GetByIdAsync(id);

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

            var book = await _bookRepository!.GetByIdAsync(id);

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

            var updated = await _bookRepository.UpdateAsync(book);

            if (updated)
            {
                Console.WriteLine();
                Console.WriteLine("✅ Book updated successfully!");
                Console.WriteLine();

                // Fetch and display updated book
                var refreshedBook = await _bookRepository.GetByIdAsync(id);
                if (refreshedBook != null)
                {
                    DisplayBookDetails(refreshedBook);
                }
            }
            else
            {
                Console.WriteLine("❌ Failed to update book.");
            }
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

            var book = await _bookRepository!.GetByIdAsync(id);

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

            var deleted = await _bookRepository.DeleteAsync(id);

            if (deleted)
            {
                Console.WriteLine("✅ Book deleted successfully!");
            }
            else
            {
                Console.WriteLine("❌ Failed to delete book.");
            }
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

            var books = await _bookRepository!.SearchByTitleAsync(searchTerm);

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
                    withDelays: true
                );

                switch (choice)
                {
                    case 1:
                        await demoRunner.RunScenario1_BasicBookManagementAsync();
                        break;
                    case 2:
                        await demoRunner.RunScenario2_AuthorManagementAsync();
                        break;
                    case 3:
                        await demoRunner.RunScenario3_MemberManagementAsync();
                        break;
                    case 4:
                        await demoRunner.RunScenario4_CompleteLoanWorkflowAsync();
                        break;
                    case 5:
                        await demoRunner.RunScenario5_OverdueLoanScenarioAsync();
                        break;
                    case 6:
                        await demoRunner.RunScenario6_LoanRenewalAsync();
                        break;
                    case 7:
                        await demoRunner.RunAllScenariosAsync();
                        break;
                    case 8:
                        await RunConnectionPoolingDemoAsync();
                        break;
                    case 9:
                        await RunBulkOperationsDemoAsync();
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
    /// Displays the demo menu
    /// </summary>
    private static void DisplayDemoMenu()
    {
        Console.Clear();
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║         AUTOMATED DEMO SCENARIOS       ║");
        Console.WriteLine("╚════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("1. Scenario 1: Basic Book Management");
        Console.WriteLine("2. Scenario 2: Author Management");
        Console.WriteLine("3. Scenario 3: Member Management");
        Console.WriteLine("4. Scenario 4: Complete Loan Workflow (Happy Path)");
        Console.WriteLine("5. Scenario 5: Overdue Loan Scenario");
        Console.WriteLine("6. Scenario 6: Loan Renewal");
        Console.WriteLine("7. Run ALL Scenarios");
        Console.WriteLine("8. Connection Pooling Performance Demo");
        Console.WriteLine("9. Bulk Operations Performance Demo");
        Console.WriteLine("0. Back to Main Menu");
        Console.WriteLine();
        Console.Write("Enter your choice: ");
    }

    /// <summary>
    /// Runs the connection pooling performance demonstration
    /// </summary>
    private static async Task RunConnectionPoolingDemoAsync()
    {
        if (_configuration == null || _bookRepository == null)
        {
            Console.WriteLine("❌ Configuration or repository not initialized. Cannot run demo.");
            return;
        }

        try
        {
            var appConnectionString = _configuration.GetConnectionString("LibraryDb");

            if (string.IsNullOrEmpty(appConnectionString))
            {
                Console.WriteLine("❌ Application connection string not configured!");
                return;
            }

            var poolingDemo = new ConnectionPoolingDemo(appConnectionString, _bookRepository);
            await poolingDemo.RunDemonstrationAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Connection pooling demo error: {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Runs the bulk operations performance demonstration
    /// </summary>
    private static async Task RunBulkOperationsDemoAsync()
    {
        if (_configuration == null)
        {
            Console.WriteLine("❌ Configuration not initialized. Cannot run demo.");
            return;
        }

        try
        {
            var appConnectionString = _configuration.GetConnectionString("LibraryDb");

            if (string.IsNullOrEmpty(appConnectionString))
            {
                Console.WriteLine("❌ Application connection string not configured!");
                return;
            }

            var bulkDemo = new BulkOperationsDemo(appConnectionString);
            await bulkDemo.RunDemonstrationAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Bulk operations demo error: {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
        }
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
