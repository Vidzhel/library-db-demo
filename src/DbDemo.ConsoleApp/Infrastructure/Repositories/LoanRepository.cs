using DbDemo.ConsoleApp.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DbDemo.ConsoleApp.Infrastructure.Repositories;

/// <summary>
/// ADO.NET implementation of ILoanRepository
/// Demonstrates proper parameterized queries, resource disposal, and async patterns
/// </summary>
public class LoanRepository : ILoanRepository
{
    private readonly string _connectionString;

    public LoanRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<Loan> CreateAsync(Loan loan, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO Loans (
                MemberId, BookId, BorrowedAt, DueDate, ReturnedAt, Status,
                LateFee, IsFeePaid, RenewalCount, MaxRenewalsAllowed, Notes,
                CreatedAt, UpdatedAt
            )
            OUTPUT INSERTED.Id
            VALUES (
                @MemberId, @BookId, @BorrowedAt, @DueDate, @ReturnedAt, @Status,
                @LateFee, @IsFeePaid, @RenewalCount, @MaxRenewalsAllowed, @Notes,
                @CreatedAt, @UpdatedAt
            );";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        AddLoanParameters(command, loan);

        var newId = (int)await command.ExecuteScalarAsync(cancellationToken);

        return await GetByIdAsync(newId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve newly created loan");
    }

    public async Task<Loan?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, MemberId, BookId, BorrowedAt, DueDate, ReturnedAt, Status,
                LateFee, IsFeePaid, RenewalCount, MaxRenewalsAllowed, Notes,
                CreatedAt, UpdatedAt
            FROM Loans
            WHERE Id = @Id;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return MapReaderToLoan(reader);
        }

        return null;
    }

    public async Task<List<Loan>> GetPagedAsync(
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) throw new ArgumentException("Page number must be >= 1", nameof(pageNumber));
        if (pageSize < 1) throw new ArgumentException("Page size must be >= 1", nameof(pageSize));

        const string sql = @"
            SELECT
                Id, MemberId, BookId, BorrowedAt, DueDate, ReturnedAt, Status,
                LateFee, IsFeePaid, RenewalCount, MaxRenewalsAllowed, Notes,
                CreatedAt, UpdatedAt
            FROM Loans
            ORDER BY BorrowedAt DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Offset", SqlDbType.Int).Value = (pageNumber - 1) * pageSize;
        command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var loans = new List<Loan>();
        while (await reader.ReadAsync(cancellationToken))
        {
            loans.Add(MapReaderToLoan(reader));
        }

        return loans;
    }

    public async Task<List<Loan>> GetActiveLoansByMemberIdAsync(int memberId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, MemberId, BookId, BorrowedAt, DueDate, ReturnedAt, Status,
                LateFee, IsFeePaid, RenewalCount, MaxRenewalsAllowed, Notes,
                CreatedAt, UpdatedAt
            FROM Loans
            WHERE MemberId = @MemberId AND ReturnedAt IS NULL
            ORDER BY BorrowedAt DESC;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@MemberId", SqlDbType.Int).Value = memberId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var loans = new List<Loan>();
        while (await reader.ReadAsync(cancellationToken))
        {
            loans.Add(MapReaderToLoan(reader));
        }

        return loans;
    }

    public async Task<List<Loan>> GetOverdueLoansAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, MemberId, BookId, BorrowedAt, DueDate, ReturnedAt, Status,
                LateFee, IsFeePaid, RenewalCount, MaxRenewalsAllowed, Notes,
                CreatedAt, UpdatedAt
            FROM Loans
            WHERE ReturnedAt IS NULL AND DueDate < @Now
            ORDER BY DueDate ASC;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Now", SqlDbType.DateTime2).Value = DateTime.UtcNow;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var loans = new List<Loan>();
        while (await reader.ReadAsync(cancellationToken))
        {
            loans.Add(MapReaderToLoan(reader));
        }

        return loans;
    }

    public async Task<List<Loan>> GetLoanHistoryByMemberIdAsync(int memberId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, MemberId, BookId, BorrowedAt, DueDate, ReturnedAt, Status,
                LateFee, IsFeePaid, RenewalCount, MaxRenewalsAllowed, Notes,
                CreatedAt, UpdatedAt
            FROM Loans
            WHERE MemberId = @MemberId
            ORDER BY BorrowedAt DESC;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@MemberId", SqlDbType.Int).Value = memberId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var loans = new List<Loan>();
        while (await reader.ReadAsync(cancellationToken))
        {
            loans.Add(MapReaderToLoan(reader));
        }

        return loans;
    }

    public async Task<List<Loan>> GetLoanHistoryByBookIdAsync(int bookId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, MemberId, BookId, BorrowedAt, DueDate, ReturnedAt, Status,
                LateFee, IsFeePaid, RenewalCount, MaxRenewalsAllowed, Notes,
                CreatedAt, UpdatedAt
            FROM Loans
            WHERE BookId = @BookId
            ORDER BY BorrowedAt DESC;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@BookId", SqlDbType.Int).Value = bookId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var loans = new List<Loan>();
        while (await reader.ReadAsync(cancellationToken))
        {
            loans.Add(MapReaderToLoan(reader));
        }

        return loans;
    }

    public async Task<int> GetCountAsync(LoanStatus? status = null, CancellationToken cancellationToken = default)
    {
        string sql = "SELECT COUNT(*) FROM Loans";

        if (status.HasValue)
        {
            sql += " WHERE Status = @Status;";
        }
        else
        {
            sql += ";";
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);

        if (status.HasValue)
        {
            command.Parameters.Add("@Status", SqlDbType.Int).Value = (int)status.Value;
        }

        var count = (int)await command.ExecuteScalarAsync(cancellationToken);
        return count;
    }

    public async Task<bool> UpdateAsync(Loan loan, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE Loans
            SET
                MemberId = @MemberId,
                BookId = @BookId,
                BorrowedAt = @BorrowedAt,
                DueDate = @DueDate,
                ReturnedAt = @ReturnedAt,
                Status = @Status,
                LateFee = @LateFee,
                IsFeePaid = @IsFeePaid,
                RenewalCount = @RenewalCount,
                MaxRenewalsAllowed = @MaxRenewalsAllowed,
                Notes = @Notes,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        AddLoanParameters(command, loan);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = loan.Id;

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        // Hard delete
        const string sql = "DELETE FROM Loans WHERE Id = @Id;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    /// <summary>
    /// Helper method to add all loan parameters to a command
    /// Centralizes parameter creation to avoid duplication
    /// </summary>
    private static void AddLoanParameters(SqlCommand command, Loan loan)
    {
        command.Parameters.Add("@MemberId", SqlDbType.Int).Value = loan.MemberId;
        command.Parameters.Add("@BookId", SqlDbType.Int).Value = loan.BookId;
        command.Parameters.Add("@BorrowedAt", SqlDbType.DateTime2).Value = loan.BorrowedAt;
        command.Parameters.Add("@DueDate", SqlDbType.DateTime2).Value = loan.DueDate;
        command.Parameters.Add("@ReturnedAt", SqlDbType.DateTime2).Value = (object?)loan.ReturnedAt ?? DBNull.Value;
        command.Parameters.Add("@Status", SqlDbType.Int).Value = (int)loan.Status;
        command.Parameters.Add("@LateFee", SqlDbType.Decimal).Value = (object?)loan.LateFee ?? DBNull.Value;
        command.Parameters.Add("@IsFeePaid", SqlDbType.Bit).Value = loan.IsFeePaid;
        command.Parameters.Add("@RenewalCount", SqlDbType.Int).Value = loan.RenewalCount;
        command.Parameters.Add("@MaxRenewalsAllowed", SqlDbType.Int).Value = loan.MaxRenewalsAllowed;
        command.Parameters.Add("@Notes", SqlDbType.NVarChar, 1000).Value = (object?)loan.Notes ?? DBNull.Value;
        command.Parameters.Add("@CreatedAt", SqlDbType.DateTime2).Value = loan.CreatedAt;
        command.Parameters.Add("@UpdatedAt", SqlDbType.DateTime2).Value = loan.UpdatedAt;
    }

    /// <summary>
    /// Maps a SqlDataReader row to a Loan entity
    /// Uses internal factory method to bypass validation for database-sourced data
    /// </summary>
    private static Loan MapReaderToLoan(SqlDataReader reader)
    {
        var id = reader.GetInt32(0);
        var memberId = reader.GetInt32(1);
        var bookId = reader.GetInt32(2);
        var borrowedAt = reader.GetDateTime(3);
        var dueDate = reader.GetDateTime(4);
        var returnedAt = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5);
        var status = (LoanStatus)reader.GetInt32(6);
        var lateFee = reader.IsDBNull(7) ? (decimal?)null : reader.GetDecimal(7);
        var isFeePaid = reader.GetBoolean(8);
        var renewalCount = reader.GetInt32(9);
        var maxRenewalsAllowed = reader.GetInt32(10);
        var notes = reader.IsDBNull(11) ? null : reader.GetString(11);
        var createdAt = reader.GetDateTime(12);
        var updatedAt = reader.GetDateTime(13);

        return Loan.FromDatabase(
            id,
            memberId,
            bookId,
            borrowedAt,
            dueDate,
            returnedAt,
            status,
            lateFee,
            isFeePaid,
            renewalCount,
            maxRenewalsAllowed,
            notes,
            createdAt,
            updatedAt
        );
    }
}
