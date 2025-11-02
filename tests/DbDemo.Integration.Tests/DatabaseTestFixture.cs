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

    /// <summary>
    /// Executes a scalar query and returns the result
    /// </summary>
    public async Task<T> ExecuteScalarAsync<T>(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        var result = await command.ExecuteScalarAsync();
        return (T)result!;
    }

    /// <summary>
    /// Executes a query and returns multiple rows
    /// </summary>
    public async Task<List<T>> ExecuteQueryAsync<T>(string sql, params (string Name, object Value)[] parameters)
    {
        var results = new List<T>();

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            // For tuples
            if (typeof(T).IsValueType && typeof(T).IsGenericType && typeof(T).Name.StartsWith("ValueTuple"))
            {
                var fieldCount = reader.FieldCount;
                var values = new object[fieldCount];
                for (int i = 0; i < fieldCount; i++)
                {
                    values[i] = reader.GetValue(i);
                }
                results.Add((T)Activator.CreateInstance(typeof(T), values)!);
            }
            // For simple types
            else
            {
                results.Add((T)reader.GetValue(0));
            }
        }

        return results;
    }

    /// <summary>
    /// Executes a non-query command (INSERT, UPDATE, DELETE)
    /// </summary>
    public async Task<int> ExecuteNonQueryAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        // Cleanup if needed
        GC.SuppressFinalize(this);
    }
}
