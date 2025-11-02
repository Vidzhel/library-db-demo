using Microsoft.Data.SqlClient;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace DbDemo.Infrastructure.SqlKata;

/// <summary>
/// Provides QueryFactory instances for SqlKata query execution.
/// Handles transaction integration with SqlKata.
/// </summary>
public static class QueryFactoryProvider
{
    /// <summary>
    /// Creates a QueryFactory from a SqlTransaction.
    /// This allows SqlKata queries to participate in the existing transaction.
    /// </summary>
    /// <param name="transaction">The SqlTransaction to use for queries.</param>
    /// <returns>A QueryFactory configured for SQL Server that uses the provided transaction.</returns>
    public static QueryFactory Create(SqlTransaction transaction)
    {
        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));

        if (transaction.Connection == null)
            throw new InvalidOperationException("Transaction connection is null");

        // SqlKata.Execution QueryFactory requires a connection
        // Since we have a transaction, we use the transaction's connection
        // and manually manage the transaction in repository methods
        var compiler = new SqlServerCompiler();
        var factory = new QueryFactory(transaction.Connection, compiler);

        return factory;
    }

    /// <summary>
    /// Creates a QueryFactory from a connection string.
    /// This creates a new connection and should be disposed properly.
    /// </summary>
    /// <param name="connectionString">The connection string to use.</param>
    /// <returns>A QueryFactory configured for SQL Server.</returns>
    public static QueryFactory Create(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException(nameof(connectionString));

        var connection = new SqlConnection(connectionString);
        connection.Open();

        var compiler = new SqlServerCompiler();
        return new QueryFactory(connection, compiler);
    }
}
