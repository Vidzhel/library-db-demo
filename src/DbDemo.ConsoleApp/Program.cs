using DbDemo.ConsoleApp.Infrastructure.Migrations;
using Microsoft.Extensions.Configuration;

namespace DbDemo.ConsoleApp.ConsoleApp;

/// <summary>
/// Library Management System - ADO.NET Demo Application
/// This application demonstrates working with databases using ADO.NET
/// </summary>
internal class Program
{
    private static IConfiguration? _configuration;

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

        // Only wait for keypress if running interactively
        if (args.Length == 0 || !args.Contains("--no-wait"))
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
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

        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
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
}
