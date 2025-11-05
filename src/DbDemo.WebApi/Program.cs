using Microsoft.Data.SqlClient;
using Microsoft.OpenApi.Models;
using DbDemo.Application.Repositories;
using DbDemo.Infrastructure.Repositories;
using DbDemo.Infrastructure.Migrations;
using DbDemo.WebApi.Middleware;
using DbDemo.WebApi.Services;
using DotNetEnv;

// Load environment variables from .env file
// Try multiple paths to support running from different directories
var currentDir = Directory.GetCurrentDirectory();
var envPaths = new[] { ".env", "../.env", "../../.env", "../../../.env" };
var envLoaded = false;
string? loadedPath = null;

foreach (var path in envPaths)
{
    var fullPath = Path.GetFullPath(path);
    if (File.Exists(fullPath))
    {
        Env.Load(fullPath);
        envLoaded = true;
        loadedPath = fullPath;
        break;
    }
}

if (!envLoaded)
{
    Console.WriteLine($"⚠️  Warning: .env file not found in current directory: {currentDir}");
    Console.WriteLine("   Using system environment variables if available.");
}

Console.WriteLine("====================================");
Console.WriteLine("Library Management Web API");
Console.WriteLine("====================================");
Console.WriteLine();

// Run database migrations before starting the API
await RunMigrationsAsync();

Console.WriteLine();

var builder = WebApplication.CreateBuilder(args);

// Configure connection string from environment variables
var connectionString = $"Server={Environment.GetEnvironmentVariable("DB_HOST")},{Environment.GetEnvironmentVariable("DB_PORT")};" +
                      $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                      $"User Id={Environment.GetEnvironmentVariable("APP_USER")};" +
                      $"Password={Environment.GetEnvironmentVariable("APP_PASSWORD")};" +
                      "TrustServerCertificate=True;";

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Configure SqlConnection as a transient service (new connection per request)
builder.Services.AddTransient<SqlConnection>(_ => new SqlConnection(connectionString));

// Register transaction context for managing database transactions per request
builder.Services.AddScoped<ITransactionContext, TransactionContext>();

// Register repositories
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// Add Swagger/OpenAPI documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Library Management API",
        Version = "v1",
        Description = "ASP.NET Core Web API for managing library books and categories using ADO.NET",
        Contact = new OpenApiContact
        {
            Name = "Library Management System",
            Email = "admin@library.com"
        }
    });

    // Include XML comments for better documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Configure CORS to allow frontend integration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Library Management API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root URL
    });
}

// Add global error handling middleware
app.UseErrorHandling();

// Add transaction management middleware (after error handling, before controllers)
app.UseTransactionManagement();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

Console.WriteLine("====================================");
Console.WriteLine("Starting Web API Server");
Console.WriteLine("====================================");
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"Database: {Environment.GetEnvironmentVariable("DB_NAME")}");
Console.WriteLine($"Server: {Environment.GetEnvironmentVariable("DB_HOST")}:{Environment.GetEnvironmentVariable("DB_PORT")}");
Console.WriteLine("Swagger UI: http://localhost:5000");
Console.WriteLine("====================================");

app.Run();

/// <summary>
/// Runs database migrations on startup to ensure schema is up to date
/// </summary>
static async Task RunMigrationsAsync()
{
    try
    {
        Console.WriteLine("🔍 Checking database migrations...");

        // Get admin connection string (SA user) for running migrations
        var adminConnectionString = $"Server={Environment.GetEnvironmentVariable("DB_HOST")},{Environment.GetEnvironmentVariable("DB_PORT")};" +
                                   $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                                   $"User Id={Environment.GetEnvironmentVariable("SA_USER")};" +
                                   $"Password={Environment.GetEnvironmentVariable("SA_PASSWORD")};" +
                                   "TrustServerCertificate=True;";

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SA_USER")) ||
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SA_PASSWORD")))
        {
            Console.WriteLine("❌ Admin credentials not found in environment variables");
            Console.WriteLine("   Required: SA_USER and SA_PASSWORD in .env file");
            Console.WriteLine("   Skipping migrations - ensure database is already set up");
            return;
        }

        // Find migrations path - try multiple locations to support running from different directories
        var migrationsPaths = new[] { "migrations", "../migrations", "../../migrations", "../../../migrations" };
        var migrationsPath = migrationsPaths.FirstOrDefault(p => Directory.Exists(p));

        if (migrationsPath == null)
        {
            Console.WriteLine("❌ Migrations directory not found");
            Console.WriteLine("   Skipping migrations - ensure database is already set up");
            return;
        }

        var databaseName = Environment.GetEnvironmentVariable("DB_NAME") ?? "LibraryDb";

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
        Console.WriteLine("⚠️  Continuing to start API - database may not be properly configured");
        Console.WriteLine("    You may need to run migrations manually using DbDemo.Setup");
        Console.WriteLine();
    }
}

// Make Program class accessible for integration tests
public partial class Program { }
