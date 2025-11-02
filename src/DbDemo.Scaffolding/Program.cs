using DbDemo.Scaffolding;
using Microsoft.Extensions.Configuration;

Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
Console.WriteLine("║  Database Scaffolding Tool - DbDemo                      ║");
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

    var connectionString = configuration.GetConnectionString("LibraryDb");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("LibraryDb connection string not found in configuration");
    }

    Console.WriteLine("Reading database schema...");
    var schemaReader = new SchemaReader(connectionString);
    var tables = await schemaReader.ReadSchemaAsync();

    Console.WriteLine($"Found {tables.Count} tables with {tables.Sum(t => t.Columns.Count)} total columns");
    Console.WriteLine();

    // Determine output directory (relative to project root)
    var currentDirectory = Directory.GetCurrentDirectory();
    var projectRoot = FindProjectRoot(currentDirectory);
    var outputDirectory = Path.Combine(projectRoot, "src", "DbDemo.Infrastructure.SqlKata", "Generated");

    Console.WriteLine($"Generating code to: {outputDirectory}");
    Console.WriteLine();

    var codeGenerator = new CodeGenerator();
    await codeGenerator.WriteGeneratedFilesAsync(outputDirectory, tables);

    Console.WriteLine();
    Console.WriteLine("Schema Summary:");
    foreach (var table in tables.OrderBy(t => t.TableName))
    {
        Console.WriteLine($"  • {table.TableName} ({table.Columns.Count} columns)");
    }

    Console.WriteLine();
    Console.WriteLine("✓ Scaffolding completed successfully!");
    Console.WriteLine();
    Console.WriteLine("Generated constants can be used like:");
    Console.WriteLine("  - Tables.Books");
    Console.WriteLine("  - Columns.Books.ISBN");
    Console.WriteLine("  - Columns.Books.Title");

    return 0;
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"✗ Error: {ex.Message}");
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine(ex.StackTrace);
    return 1;
}

static string FindProjectRoot(string currentDirectory)
{
    var directory = new DirectoryInfo(currentDirectory);
    while (directory != null)
    {
        // Look for solution file or migrations folder
        if (directory.GetFiles("*.sln").Any() ||
            directory.GetDirectories("migrations").Any())
        {
            return directory.FullName;
        }
        directory = directory.Parent;
    }

    throw new InvalidOperationException("Could not find project root (no .sln file found)");
}
