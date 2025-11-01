using DbDemo.ConsoleApp.Models;
using Microsoft.Data.SqlClient;

namespace DbDemo.ConsoleApp.Infrastructure.Repositories;

/// <summary>
/// Repository for querying audit trail records for Book changes.
/// Audit records are created automatically by the TR_Books_Audit database trigger.
/// </summary>
public class BookAuditRepository : IBookAuditRepository
{
    public BookAuditRepository()
    {
    }

    public async Task<List<BookAudit>> GetAuditHistoryAsync(
        int bookId,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                AuditId,
                BookId,
                Action,
                OldISBN,
                NewISBN,
                OldTitle,
                NewTitle,
                OldAvailableCopies,
                NewAvailableCopies,
                OldTotalCopies,
                NewTotalCopies,
                ChangedAt,
                ChangedBy
            FROM dbo.BooksAudit
            WHERE BookId = @BookId
            ORDER BY ChangedAt DESC, AuditId DESC";

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        command.Parameters.AddWithValue("@BookId", bookId);

        var auditRecords = new List<BookAudit>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            auditRecords.Add(MapReaderToBookAudit(reader));
        }

        return auditRecords;
    }

    public async Task<List<BookAudit>> GetAllAuditRecordsAsync(
        string? action = null,
        int limit = 100,
        SqlTransaction transaction = null!,
        CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT TOP (@Limit)
                AuditId,
                BookId,
                Action,
                OldISBN,
                NewISBN,
                OldTitle,
                NewTitle,
                OldAvailableCopies,
                NewAvailableCopies,
                OldTotalCopies,
                NewTotalCopies,
                ChangedAt,
                ChangedBy
            FROM dbo.BooksAudit";

        if (!string.IsNullOrWhiteSpace(action))
        {
            sql += " WHERE Action = @Action";
        }

        sql += " ORDER BY ChangedAt DESC, AuditId DESC";

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        command.Parameters.AddWithValue("@Limit", limit);

        if (!string.IsNullOrWhiteSpace(action))
        {
            command.Parameters.AddWithValue("@Action", action);
        }

        var auditRecords = new List<BookAudit>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            auditRecords.Add(MapReaderToBookAudit(reader));
        }

        return auditRecords;
    }

    private static BookAudit MapReaderToBookAudit(SqlDataReader reader)
    {
        return BookAudit.FromDatabase(
            auditId: reader.GetInt32(0),
            bookId: reader.GetInt32(1),
            action: reader.GetString(2),
            oldISBN: reader.IsDBNull(3) ? null : reader.GetString(3),
            newISBN: reader.IsDBNull(4) ? null : reader.GetString(4),
            oldTitle: reader.IsDBNull(5) ? null : reader.GetString(5),
            newTitle: reader.IsDBNull(6) ? null : reader.GetString(6),
            oldAvailableCopies: reader.IsDBNull(7) ? null : reader.GetInt32(7),
            newAvailableCopies: reader.IsDBNull(8) ? null : reader.GetInt32(8),
            oldTotalCopies: reader.IsDBNull(9) ? null : reader.GetInt32(9),
            newTotalCopies: reader.IsDBNull(10) ? null : reader.GetInt32(10),
            changedAt: reader.GetDateTime(11),
            changedBy: reader.GetString(12)
        );
    }
}
