using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DbDemo.Infrastructure.Repositories;

using DbDemo.Domain.Entities;
using DbDemo.Application.DTOs;
using DbDemo.Application.Repositories;

/// <summary>
/// ADO.NET implementation of IMemberRepository
/// Demonstrates proper parameterized queries, resource disposal, and async patterns
/// </summary>
public class MemberRepository : IMemberRepository
{
    public MemberRepository()
    {
    }

    public async Task<Member> CreateAsync(Member member, SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO Members (
                MembershipNumber, FirstName, LastName, Email, PhoneNumber, DateOfBirth,
                Address, MemberSince, MembershipExpiresAt, IsActive, MaxBooksAllowed,
                OutstandingFees, CreatedAt, UpdatedAt
            )
            OUTPUT INSERTED.Id
            VALUES (
                @MembershipNumber, @FirstName, @LastName, @Email, @PhoneNumber, @DateOfBirth,
                @Address, @MemberSince, @MembershipExpiresAt, @IsActive, @MaxBooksAllowed,
                @OutstandingFees, @CreatedAt, @UpdatedAt
            );";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);
        AddMemberParameters(command, member);

        var newId = (int)await command.ExecuteScalarAsync(cancellationToken);

        return await GetByIdAsync(newId, transaction, cancellationToken)
               ?? throw new InvalidOperationException("Failed to retrieve newly created member");
    }

    public async Task<Member?> GetByIdAsync(int id, SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, MembershipNumber, FirstName, LastName, Email, PhoneNumber, DateOfBirth,
                Address, MemberSince, MembershipExpiresAt, IsActive, MaxBooksAllowed,
                OutstandingFees, CreatedAt, UpdatedAt
            FROM Members
            WHERE Id = @Id;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return MapReaderToMember(reader);
        }

        return null;
    }

    public async Task<Member?> GetByMembershipNumberAsync(string membershipNumber, SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, MembershipNumber, FirstName, LastName, Email, PhoneNumber, DateOfBirth,
                Address, MemberSince, MembershipExpiresAt, IsActive, MaxBooksAllowed,
                OutstandingFees, CreatedAt, UpdatedAt
            FROM Members
            WHERE MembershipNumber = @MembershipNumber;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@MembershipNumber", SqlDbType.NVarChar, 20).Value = membershipNumber;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return MapReaderToMember(reader);
        }

        return null;
    }

    public async Task<Member?> GetByEmailAsync(string email, SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, MembershipNumber, FirstName, LastName, Email, PhoneNumber, DateOfBirth,
                Address, MemberSince, MembershipExpiresAt, IsActive, MaxBooksAllowed,
                OutstandingFees, CreatedAt, UpdatedAt
            FROM Members
            WHERE Email = @Email;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = email.ToLowerInvariant();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return MapReaderToMember(reader);
        }

        return null;
    }

    public async Task<List<Member>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) throw new ArgumentException("Page number must be >= 1", nameof(pageNumber));
        if (pageSize < 1) throw new ArgumentException("Page size must be >= 1", nameof(pageSize));

        string sql = @"
            SELECT
                Id, MembershipNumber, FirstName, LastName, Email, PhoneNumber, DateOfBirth,
                Address, MemberSince, MembershipExpiresAt, IsActive, MaxBooksAllowed,
                OutstandingFees, CreatedAt, UpdatedAt
            FROM Members"
                     + (activeOnly ? " WHERE IsActive = 1" : "")
                     + @"
            ORDER BY LastName, FirstName
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Offset", SqlDbType.Int).Value = (pageNumber - 1) * pageSize;
        command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var members = new List<Member>();
        while (await reader.ReadAsync(cancellationToken))
        {
            members.Add(MapReaderToMember(reader));
        }

        return members;
    }

    public async Task<List<Member>> SearchByNameAsync(string searchTerm, SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException("Search term cannot be empty", nameof(searchTerm));

        const string sql = @"
            SELECT
                Id, MembershipNumber, FirstName, LastName, Email, PhoneNumber, DateOfBirth,
                Address, MemberSince, MembershipExpiresAt, IsActive, MaxBooksAllowed,
                OutstandingFees, CreatedAt, UpdatedAt
            FROM Members
            WHERE FirstName LIKE @SearchPattern OR LastName LIKE @SearchPattern
            ORDER BY LastName, FirstName;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@SearchPattern", SqlDbType.NVarChar, 102).Value = $"%{searchTerm}%";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var members = new List<Member>();
        while (await reader.ReadAsync(cancellationToken))
        {
            members.Add(MapReaderToMember(reader));
        }

        return members;
    }

    public async Task<int> GetCountAsync(bool activeOnly, SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        string sql = "SELECT COUNT(*) FROM Members"
                     + (activeOnly ? " WHERE IsActive = 1;" : ";");

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);

        var count = (int)await command.ExecuteScalarAsync(cancellationToken);
        return count;
    }

    public async Task<bool> UpdateAsync(Member member, SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE Members
            SET
                MembershipNumber = @MembershipNumber,
                FirstName = @FirstName,
                LastName = @LastName,
                Email = @Email,
                PhoneNumber = @PhoneNumber,
                DateOfBirth = @DateOfBirth,
                Address = @Address,
                MemberSince = @MemberSince,
                MembershipExpiresAt = @MembershipExpiresAt,
                IsActive = @IsActive,
                MaxBooksAllowed = @MaxBooksAllowed,
                OutstandingFees = @OutstandingFees,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);
        AddMemberParameters(command, member);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = member.Id;

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id, SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        // Hard delete - will fail if member has loans due to FK constraints
        const string sql = "DELETE FROM Members WHERE Id = @Id;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        try
        {
            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            return rowsAffected > 0;
        }
        catch (SqlException)
        {
            // FK constraint violation - member has loans
            return false;
        }
    }

    public async Task<MemberStatistics?> GetStatisticsAsync(int memberId, SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        // Query the table-valued function fn_GetMemberStatistics
        // TVFs are queried just like tables using SELECT
        const string sql = @"
            SELECT
                MemberId,
                TotalBooksLoaned,
                ActiveLoans,
                OverdueLoans,
                ReturnedLateCount,
                TotalLateFees,
                UnpaidLateFees,
                AvgLoanDurationDays,
                LastBorrowDate,
                TotalRenewals,
                LostOrDamagedCount
            FROM dbo.fn_GetMemberStatistics(@MemberId);";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@MemberId", SqlDbType.Int).Value = memberId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            // If the member doesn't exist or has no loan history, TotalBooksLoaned will be 0
            // We still return statistics showing all zeros rather than null
            return MapReaderToMemberStatistics(reader);
        }

        // This should not happen as the TVF always returns one row, even if member doesn't exist
        return null;
    }

    /// <summary>
    /// Helper method to add all member parameters to a command
    /// Centralizes parameter creation to avoid duplication
    /// </summary>
    private static void AddMemberParameters(SqlCommand command, Member member)
    {
        command.Parameters.Add("@MembershipNumber", SqlDbType.NVarChar, 20).Value = member.MembershipNumber;
        command.Parameters.Add("@FirstName", SqlDbType.NVarChar, 50).Value = member.FirstName;
        command.Parameters.Add("@LastName", SqlDbType.NVarChar, 50).Value = member.LastName;
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = member.Email;
        command.Parameters.Add("@PhoneNumber", SqlDbType.NVarChar, 20).Value =
            (object?)member.PhoneNumber ?? DBNull.Value;
        command.Parameters.Add("@DateOfBirth", SqlDbType.DateTime2).Value = member.DateOfBirth;
        command.Parameters.Add("@Address", SqlDbType.NVarChar, 500).Value = (object?)member.Address ?? DBNull.Value;
        command.Parameters.Add("@MemberSince", SqlDbType.DateTime2).Value = member.MemberSince;
        command.Parameters.Add("@MembershipExpiresAt", SqlDbType.DateTime2).Value = member.MembershipExpiresAt;
        command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = member.IsActive;
        command.Parameters.Add("@MaxBooksAllowed", SqlDbType.Int).Value = member.MaxBooksAllowed;
        command.Parameters.Add("@OutstandingFees", SqlDbType.Decimal).Value = member.OutstandingFees;
        command.Parameters.Add("@CreatedAt", SqlDbType.DateTime2).Value = member.CreatedAt;
        command.Parameters.Add("@UpdatedAt", SqlDbType.DateTime2).Value = member.UpdatedAt;
    }

    /// <summary>
    /// Maps a SqlDataReader row to a Member entity
    /// Uses internal factory method to bypass validation for database-sourced data
    /// </summary>
    private static Member MapReaderToMember(SqlDataReader reader)
    {
        var id = reader.GetInt32(0);
        var membershipNumber = reader.GetString(1);
        var firstName = reader.GetString(2);
        var lastName = reader.GetString(3);
        var email = reader.GetString(4);
        var phoneNumber = reader.IsDBNull(5) ? null : reader.GetString(5);
        var dateOfBirth = reader.GetDateTime(6);
        var address = reader.IsDBNull(7) ? null : reader.GetString(7);
        var memberSince = reader.GetDateTime(8);
        var membershipExpiresAt = reader.GetDateTime(9);
        var isActive = reader.GetBoolean(10);
        var maxBooksAllowed = reader.GetInt32(11);
        var outstandingFees = reader.GetDecimal(12);
        var createdAt = reader.GetDateTime(13);
        var updatedAt = reader.GetDateTime(14);

        return Member.FromDatabase(
            id,
            membershipNumber,
            firstName,
            lastName,
            email,
            phoneNumber,
            dateOfBirth,
            address,
            memberSince,
            membershipExpiresAt,
            isActive,
            maxBooksAllowed,
            outstandingFees,
            createdAt,
            updatedAt
        );
    }

    /// <summary>
    /// Maps a SqlDataReader row to a MemberStatistics DTO
    /// Reads results from fn_GetMemberStatistics table-valued function
    /// </summary>
    private static MemberStatistics MapReaderToMemberStatistics(SqlDataReader reader)
    {
        var memberId = reader.GetInt32(0);
        var totalBooksLoaned = reader.GetInt32(1);
        var activeLoans = reader.GetInt32(2);
        var overdueLoans = reader.GetInt32(3);
        var returnedLateCount = reader.GetInt32(4);
        var totalLateFees = reader.GetDecimal(5);
        var unpaidLateFees = reader.GetDecimal(6);
        var avgLoanDurationDays = reader.IsDBNull(7) ? null : (int?)reader.GetInt32(7);
        var lastBorrowDate = reader.IsDBNull(8) ? null : (DateTime?)reader.GetDateTime(8);
        var totalRenewals = reader.GetInt32(9);
        var lostOrDamagedCount = reader.GetInt32(10);

        return MemberStatistics.FromDatabase(
            memberId,
            totalBooksLoaned,
            activeLoans,
            overdueLoans,
            returnedLateCount,
            totalLateFees,
            unpaidLateFees,
            avgLoanDurationDays,
            lastBorrowDate,
            totalRenewals,
            lostOrDamagedCount
        );
    }
}