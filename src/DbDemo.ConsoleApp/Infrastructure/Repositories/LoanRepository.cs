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
    public LoanRepository()
    {
    }

    public async Task<Loan> CreateAsync(Loan loan, SqlTransaction transaction, CancellationToken cancellationToken = default)
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

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);

        AddLoanParameters(command, loan);

        var newId = (int)await command.ExecuteScalarAsync(cancellationToken);

        // Fetch the created loan using the same transaction
        return await GetByIdAsync(newId, transaction, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve newly created loan");
    }

    public async Task<Loan?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, MemberId, BookId, BorrowedAt, DueDate, ReturnedAt, Status,
                LateFee, IsFeePaid, RenewalCount, MaxRenewalsAllowed, Notes,
                CreatedAt, UpdatedAt
            FROM Loans
            WHERE Id = @Id;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return MapReaderToLoan(reader);
        }

        return null;
    }

    public async Task<List<Loan>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        SqlTransaction transaction,
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

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
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

    public async Task<List<Loan>> GetActiveLoansByMemberIdAsync(int memberId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, MemberId, BookId, BorrowedAt, DueDate, ReturnedAt, Status,
                LateFee, IsFeePaid, RenewalCount, MaxRenewalsAllowed, Notes,
                CreatedAt, UpdatedAt
            FROM Loans
            WHERE MemberId = @MemberId AND ReturnedAt IS NULL
            ORDER BY BorrowedAt DESC;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@MemberId", SqlDbType.Int).Value = memberId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var loans = new List<Loan>();
        while (await reader.ReadAsync(cancellationToken))
        {
            loans.Add(MapReaderToLoan(reader));
        }

        return loans;
    }

    public async Task<List<Loan>> GetOverdueLoansAsync(SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, MemberId, BookId, BorrowedAt, DueDate, ReturnedAt, Status,
                LateFee, IsFeePaid, RenewalCount, MaxRenewalsAllowed, Notes,
                CreatedAt, UpdatedAt
            FROM Loans
            WHERE ReturnedAt IS NULL AND DueDate < @Now
            ORDER BY DueDate ASC;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Now", SqlDbType.DateTime2).Value = DateTime.UtcNow;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var loans = new List<Loan>();
        while (await reader.ReadAsync(cancellationToken))
        {
            loans.Add(MapReaderToLoan(reader));
        }

        return loans;
    }

    public async Task<List<Loan>> GetLoanHistoryByMemberIdAsync(int memberId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, MemberId, BookId, BorrowedAt, DueDate, ReturnedAt, Status,
                LateFee, IsFeePaid, RenewalCount, MaxRenewalsAllowed, Notes,
                CreatedAt, UpdatedAt
            FROM Loans
            WHERE MemberId = @MemberId
            ORDER BY BorrowedAt DESC;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@MemberId", SqlDbType.Int).Value = memberId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var loans = new List<Loan>();
        while (await reader.ReadAsync(cancellationToken))
        {
            loans.Add(MapReaderToLoan(reader));
        }

        return loans;
    }

    public async Task<List<Loan>> GetLoanHistoryByBookIdAsync(int bookId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, MemberId, BookId, BorrowedAt, DueDate, ReturnedAt, Status,
                LateFee, IsFeePaid, RenewalCount, MaxRenewalsAllowed, Notes,
                CreatedAt, UpdatedAt
            FROM Loans
            WHERE BookId = @BookId
            ORDER BY BorrowedAt DESC;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@BookId", SqlDbType.Int).Value = bookId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var loans = new List<Loan>();
        while (await reader.ReadAsync(cancellationToken))
        {
            loans.Add(MapReaderToLoan(reader));
        }

        return loans;
    }

    public async Task<int> GetCountAsync(LoanStatus? status, SqlTransaction transaction, CancellationToken cancellationToken = default)
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

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);

        if (status.HasValue)
        {
            command.Parameters.Add("@Status", SqlDbType.Int).Value = (int)status.Value;
        }

        var count = (int)await command.ExecuteScalarAsync(cancellationToken);
        return count;
    }

    public async Task<bool> UpdateAsync(Loan loan, SqlTransaction transaction, CancellationToken cancellationToken = default)
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

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);

        AddLoanParameters(command, loan);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = loan.Id;

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        // Hard delete
        const string sql = "DELETE FROM Loans WHERE Id = @Id;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<(List<OverdueLoanReport> Loans, int TotalCount)> GetOverdueLoansReportAsync(
        DateTime? asOfDate,
        int minDaysOverdue,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var connection = transaction.Connection;

        await using var command = new SqlCommand("dbo.sp_GetOverdueLoans", connection, transaction);
        command.CommandType = System.Data.CommandType.StoredProcedure;

        // Input parameters
        command.Parameters.Add("@AsOfDate", SqlDbType.DateTime2).Value = (object?)asOfDate ?? DBNull.Value;
        command.Parameters.Add("@MinDaysOverdue", SqlDbType.Int).Value = minDaysOverdue;

        // Output parameter
        var totalCountParam = new SqlParameter("@TotalCount", SqlDbType.Int)
        {
            Direction = System.Data.ParameterDirection.Output
        };
        command.Parameters.Add(totalCountParam);

        var loans = new List<OverdueLoanReport>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            loans.Add(MapReaderToOverdueLoanReport(reader));
        }

        // Close reader before accessing output parameter
        await reader.CloseAsync();

        // Retrieve output parameter value
        var totalCount = (int)totalCountParam.Value;

        return (loans, totalCount);
    }

    public async Task<decimal> CalculateLateFeeAsync(
        int loanId,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT dbo.fn_CalculateLateFee(@LoanId)";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@LoanId", SqlDbType.Int).Value = loanId;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        // ExecuteScalar returns object, convert to decimal
        return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0.00m;
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

    /// <summary>
    /// Maps a SqlDataReader row to an OverdueLoanReport DTO
    /// Used for the sp_GetOverdueLoans stored procedure results
    /// </summary>
    private static OverdueLoanReport MapReaderToOverdueLoanReport(SqlDataReader reader)
    {
        return OverdueLoanReport.FromDatabase(
            loanId: reader.GetInt32(0),
            memberId: reader.GetInt32(1),
            memberName: reader.GetString(2),
            memberEmail: reader.GetString(3),
            memberPhone: reader.IsDBNull(4) ? null : reader.GetString(4),
            bookId: reader.GetInt32(5),
            isbn: reader.GetString(6),
            bookTitle: reader.GetString(7),
            publisher: reader.IsDBNull(8) ? null : reader.GetString(8),
            borrowedAt: reader.GetDateTime(9),
            dueDate: reader.GetDateTime(10),
            daysOverdue: reader.GetInt32(11),
            calculatedLateFee: reader.GetDecimal(12),
            status: (LoanStatus)reader.GetInt32(13),
            notes: reader.IsDBNull(14) ? null : reader.GetString(14)
        );
    }
}
