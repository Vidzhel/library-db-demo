using Microsoft.Data.SqlClient;

namespace DbDemo.WebApi.Services;

/// <summary>
/// Provides access to the current database transaction and connection for the request.
/// This service is scoped per HTTP request and managed by TransactionMiddleware.
/// </summary>
public interface ITransactionContext
{
    /// <summary>
    /// Gets the current SQL transaction for this request.
    /// </summary>
    SqlTransaction Transaction { get; }

    /// <summary>
    /// Gets the current SQL connection for this request.
    /// </summary>
    SqlConnection Connection { get; }

    /// <summary>
    /// Sets the connection and transaction for this request.
    /// Should only be called by TransactionMiddleware.
    /// </summary>
    void Initialize(SqlConnection connection, SqlTransaction transaction);
}

/// <summary>
/// Implementation of ITransactionContext that holds the connection and transaction
/// for the current HTTP request.
/// </summary>
public class TransactionContext : ITransactionContext
{
    private SqlConnection? _connection;
    private SqlTransaction? _transaction;

    public SqlTransaction Transaction => _transaction
        ?? throw new InvalidOperationException("Transaction has not been initialized. Ensure TransactionMiddleware is registered.");

    public SqlConnection Connection => _connection
        ?? throw new InvalidOperationException("Connection has not been initialized. Ensure TransactionMiddleware is registered.");

    public void Initialize(SqlConnection connection, SqlTransaction transaction)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }
}
