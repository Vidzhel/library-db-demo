using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Test fixture that provides a database connection for integration tests
/// Shared across all tests in a test class
/// </summary>
public class DatabaseTestFixture : IDisposable
{
    public string ConnectionString { get; }

    public DatabaseTestFixture()
    {
        // Load configuration from appsettings and user secrets
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<DatabaseTestFixture>()
            .Build();

        // Use the app connection string (not admin) for tests
        ConnectionString = configuration.GetConnectionString("LibraryDb")
            ?? throw new InvalidOperationException("LibraryDb connection string not found");
    }

    /// <summary>
    /// Cleans up test data from a specific table
    /// </summary>
    public async Task CleanupTableAsync(string tableName)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand($"DELETE FROM {tableName}", connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Gets count of rows in a table
    /// </summary>
    public async Task<int> GetRowCountAsync(string tableName)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand($"SELECT COUNT(*) FROM {tableName}", connection);
        return (int)await command.ExecuteScalarAsync();
    }

    public void Dispose()
    {
        // Cleanup if needed
        GC.SuppressFinalize(this);
    }
}
