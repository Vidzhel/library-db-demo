using DbDemo.Infrastructure.Migrations;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
Console.WriteLine("║  Database Setup Tool - DbDemo                            ║");
Console.WriteLine("║  Runs Migrations + Scaffolding                           ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
Console.WriteLine();

try
{
    // Load configuration
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .AddUserSecrets<Program>(optional: true)
        .Build();

    var adminConnectionString = configuration.GetConnectionString("SqlServerAdmin");
    var appConnectionString = configuration.GetConnectionString("LibraryDb");

    if (string.IsNullOrWhiteSpace(adminConnectionString))
        throw new InvalidOperationException("SqlServerAdmin connection string not found");

    if (string.IsNullOrWhiteSpace(appConnectionString))
        throw new InvalidOperationException("LibraryDb connection string not found");

    // Step 1: Run Migrations
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.WriteLine("Step 1: Running Database Migrations");
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.WriteLine();

    var currentDirectory = Directory.GetCurrentDirectory();
    var migrationsPath = Path.Combine(currentDirectory, "migrations");

    if (!Directory.Exists(migrationsPath))
    {
        throw new DirectoryNotFoundException($"Migrations directory not found: {migrationsPath}");
    }

    var runner = new MigrationRunner(adminConnectionString, migrationsPath);
    var executedCount = await runner.RunMigrationsAsync();

    Console.WriteLine();
    if (executedCount > 0)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ {executedCount} migration(s) executed successfully");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("✓ No new migrations to run (database is up to date)");
        Console.ResetColor();
    }

    Console.WriteLine();

    // Step 2: Run Scaffolding
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.WriteLine("Step 2: Running Database Scaffolding");
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.WriteLine();

    // Find the project root
    var projectRoot = FindProjectRoot(currentDirectory);
    var scaffoldingProject = Path.Combine(projectRoot, "src", "DbDemo.Scaffolding", "DbDemo.Scaffolding.csproj");

    if (!File.Exists(scaffoldingProject))
    {
        throw new FileNotFoundException($"Scaffolding project not found: {scaffoldingProject}");
    }

    // Run scaffolding tool
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{scaffoldingProject}\" --no-build",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };

    process.Start();

    // Stream output in real-time
    while (!process.StandardOutput.EndOfStream)
    {
        var line = await process.StandardOutput.ReadLineAsync();
        if (line != null)
        {
            Console.WriteLine(line);
        }
    }

    await process.WaitForExitAsync();

    if (process.ExitCode != 0)
    {
        var error = await process.StandardError.ReadToEndAsync();
        throw new InvalidOperationException($"Scaffolding failed with exit code {process.ExitCode}:\n{error}");
    }

    // Summary
    Console.WriteLine();
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✓ Setup Completed Successfully!");
    Console.ResetColor();
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.WriteLine();
    Console.WriteLine("Next steps:");
    Console.WriteLine("  1. Build the solution to verify generated schema code compiles");
    Console.WriteLine("  2. Run the ConsoleApp to choose between ADO.NET or SqlKata repositories");
    Console.WriteLine();

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.WriteLine($"✗ Setup Failed: {ex.Message}");
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("Stack trace:");
    Console.WriteLine(ex.StackTrace);
    return 1;
}

static string FindProjectRoot(string currentDirectory)
{
    var directory = new DirectoryInfo(currentDirectory);
    while (directory != null)
    {
        if (directory.GetFiles("*.sln").Any() ||
            directory.GetDirectories("migrations").Any())
        {
            return directory.FullName;
        }
        directory = directory.Parent;
    }

    throw new InvalidOperationException("Could not find project root (no .sln file found)");
}
