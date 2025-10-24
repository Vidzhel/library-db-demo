using Microsoft.Extensions.Configuration;

namespace DbDemo;

/// <summary>
/// Library Management System - ADO.NET Demo Application
/// This application demonstrates working with databases using ADO.NET
/// </summary>
internal class Program
{
    private static IConfiguration? _configuration;

    static void Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("Library Management System - ADO.NET Demo");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        // Load configuration from multiple sources
        LoadConfiguration();

        // Display configuration information
        DisplayConfigurationInfo();

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
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
}
