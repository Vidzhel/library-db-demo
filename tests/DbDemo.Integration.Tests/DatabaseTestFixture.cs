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

        // Delete all rows from the table
        await using var deleteCommand = new SqlCommand($"DELETE FROM {tableName}", connection);
        await deleteCommand.ExecuteNonQueryAsync();
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

    /// <summary>
    /// Executes a repository operation within a transaction and returns a result
    /// </summary>
    public async Task<T> WithTransactionAsync<T>(Func<SqlTransaction, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        // Note: await using ensures automatic rollback on exception (Dispose rolls back uncommitted transactions)
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var result = await operation(transaction);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Executes a repository operation within a transaction (no return value)
    /// </summary>
    public async Task WithTransactionAsync(Func<SqlTransaction, Task> operation, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        // Note: await using ensures automatic rollback on exception (Dispose rolls back uncommitted transactions)
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await operation(transaction);
        await transaction.CommitAsync(cancellationToken);
    }

    public void Dispose()
    {
        // Cleanup if needed
        GC.SuppressFinalize(this);
    }
}
