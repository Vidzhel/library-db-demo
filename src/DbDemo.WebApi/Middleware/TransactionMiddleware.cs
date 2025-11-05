using DbDemo.WebApi.Services;
using Microsoft.Data.SqlClient;

namespace DbDemo.WebApi.Middleware;

/// <summary>
/// Middleware that manages database transactions for each HTTP request.
/// Opens a connection, begins a transaction, and commits or rolls back based on the response status.
/// </summary>
public class TransactionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TransactionMiddleware> _logger;

    public TransactionMiddleware(RequestDelegate next, ILogger<TransactionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITransactionContext transactionContext, SqlConnection connection)
    {
        // Open connection and begin transaction
        await connection.OpenAsync(context.RequestAborted);
        using var transaction = connection.BeginTransaction();

        try
        {
            // Initialize the transaction context for this request
            transactionContext.Initialize(connection, transaction);

            // Execute the rest of the pipeline
            await _next(context);

            // Commit if the response indicates success (2xx status codes)
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                await transaction.CommitAsync(context.RequestAborted);
                _logger.LogDebug("Transaction committed successfully for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }
            else
            {
                await transaction.RollbackAsync(context.RequestAborted);
                _logger.LogDebug("Transaction rolled back due to status code {StatusCode} for {Method} {Path}",
                    context.Response.StatusCode, context.Request.Method, context.Request.Path);
            }
        }
        catch (Exception ex)
        {
            // Rollback transaction on any exception
            await transaction.RollbackAsync(context.RequestAborted);
            _logger.LogWarning(ex, "Transaction rolled back due to exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            // Re-throw to let ErrorHandlingMiddleware handle it
            throw;
        }
        finally
        {
            // Always close the connection
            await connection.CloseAsync();
        }
    }
}

/// <summary>
/// Extension methods for registering TransactionMiddleware.
/// </summary>
public static class TransactionMiddlewareExtensions
{
    /// <summary>
    /// Adds transaction management middleware to the application pipeline.
    /// This middleware should be registered after error handling middleware
    /// and before controller endpoints.
    /// </summary>
    public static IApplicationBuilder UseTransactionManagement(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TransactionMiddleware>();
    }
}
