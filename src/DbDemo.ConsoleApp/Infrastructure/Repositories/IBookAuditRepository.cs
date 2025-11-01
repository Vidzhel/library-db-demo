using DbDemo.ConsoleApp.Models;
using Microsoft.Data.SqlClient;

namespace DbDemo.ConsoleApp.Infrastructure.Repositories;

/// <summary>
/// Repository for querying audit trail records for Book changes.
/// Note: Audit records are created automatically by the TR_Books_Audit trigger.
/// This repository only provides read access to the audit trail.
/// </summary>
public interface IBookAuditRepository
{
    /// <summary>
    /// Gets all audit records for a specific book, ordered by most recent first.
    /// </summary>
    /// <param name="bookId">The ID of the book to get audit history for.</param>
    /// <param name="transaction">The transaction to execute within.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit records for the specified book.</returns>
    Task<List<BookAudit>> GetAuditHistoryAsync(
        int bookId,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all audit records, optionally filtered by action type.
    /// </summary>
    /// <param name="action">Optional action filter ('INSERT', 'UPDATE', 'DELETE'). If null, returns all actions.</param>
    /// <param name="limit">Maximum number of records to return.</param>
    /// <param name="transaction">The transaction to execute within.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit records.</returns>
    Task<List<BookAudit>> GetAllAuditRecordsAsync(
        string? action = null,
        int limit = 100,
        SqlTransaction transaction = null!,
        CancellationToken cancellationToken = default);
}
