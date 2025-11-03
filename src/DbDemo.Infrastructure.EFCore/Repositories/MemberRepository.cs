using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using DbDemo.Application.DTOs;
using DbDemo.Application.Repositories;
using DbDemo.Domain.Entities;
using DbDemo.Infrastructure.EFCore.Mappers;
using EFMember = DbDemo.Infrastructure.EFCore.EFModels.Member;

namespace DbDemo.Infrastructure.EFCore.Repositories;

/// <summary>
/// Entity Framework Core implementation of IMemberRepository.
///
/// This repository demonstrates STORED PROCEDURES and TABLE-VALUED FUNCTIONS:
/// ===========================================================================
///
/// ADVANCED SQL INTEROP:
/// - FromSqlRaw() to execute stored procedures
/// - Table-valued functions via FromSqlRaw()
/// - Parameter binding with SqlParameter
/// - Mapping query results to entities or DTOs
///
/// WHY USE STORED PROCEDURES IN EF:
/// - Legacy databases with existing sprocs
/// - Complex business logic in database
/// - Performance-critical operations
/// - Bulk operations not expressible in LINQ
///
/// TRADE-OFFS:
/// ✓ Can leverage existing database logic
/// ✓ Sometimes faster than generated SQL
/// ✗ Less portable across databases
/// ✗ Harder to test (requires database)
/// ✗ Business logic split between app and DB
///
/// See docs/29-ef-core-orm.md for detailed discussion.
/// </summary>
public class MemberRepository : IMemberRepository
{
    private readonly LibraryDbContext _context;

    public MemberRepository(LibraryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Basic CRUD (Similar to AuthorRepository)

    public async Task<Member> CreateAsync(Member member, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var efMember = new EFMember();
        efMember.UpdateFromDomain(member, isNewEntity: true);

        _context.Members.Add(efMember);
        await _context.SaveChangesAsync(cancellationToken);

        return efMember.ToDomain();
    }

    public async Task<Member?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var efMember = await _context.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        return efMember?.ToDomain();
    }

    public async Task<Member?> GetByMembershipNumberAsync(string membershipNumber, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(membershipNumber);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var efMember = await _context.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MembershipNumber == membershipNumber, cancellationToken);

        return efMember?.ToDomain();
    }

    public async Task<Member?> GetByEmailAsync(string email, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var efMember = await _context.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Email == email, cancellationToken);

        return efMember?.ToDomain();
    }

    public async Task<List<Member>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (pageNumber < 1) throw new ArgumentException("Page number must be >= 1", nameof(pageNumber));
        if (pageSize < 1) throw new ArgumentException("Page size must be >= 1", nameof(pageSize));

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        IQueryable<EFMember> query = _context.Members.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(m => m.IsActive);
        }

        var efMembers = await query
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return efMembers.ToDomain();
    }

    public async Task<List<Member>> SearchByNameAsync(
        string searchTerm,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchTerm);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var pattern = $"%{searchTerm}%";

        var efMembers = await _context.Members
            .AsNoTracking()
            .Where(m =>
                EF.Functions.Like(m.FirstName, pattern) ||
                EF.Functions.Like(m.LastName, pattern))
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToListAsync(cancellationToken);

        return efMembers.ToDomain();
    }

    public async Task<int> GetCountAsync(bool activeOnly, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        IQueryable<EFMember> query = _context.Members;

        if (activeOnly)
        {
            query = query.Where(m => m.IsActive);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(Member member, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var existingEfMember = await _context.Members.FindAsync(new object[] { member.Id }, cancellationToken);

        if (existingEfMember == null)
        {
            return false;
        }

        existingEfMember.UpdateFromDomain(member, isNewEntity: false);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var efMember = await _context.Members.FindAsync(new object[] { id }, cancellationToken);

        if (efMember == null)
        {
            return false;
        }

        _context.Members.Remove(efMember);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    #endregion

    #region Table-Valued Function Integration

    /// <summary>
    /// Gets member statistics using a table-valued function.
    ///
    /// EF CORE PATTERN: FromSqlRaw with table-valued function
    /// ========================================================
    ///
    /// SQL SERVER TABLE-VALUED FUNCTION:
    /// CREATE FUNCTION dbo.fn_GetMemberStatistics(@MemberId INT)
    /// RETURNS TABLE
    /// AS RETURN (
    ///     SELECT MemberId, TotalLoans, ActiveLoans, OverdueLoans, ...
    ///     FROM ... complex aggregation logic ...
    /// )
    ///
    /// EF CORE USAGE:
    /// - FromSqlRaw("SELECT * FROM fn_GetMemberStatistics({0})", memberId)
    /// - Maps result to MemberStatistics DTO
    /// - Cannot use LINQ on function results (limitation)
    ///
    /// WHY USE TABLE-VALUED FUNCTIONS:
    /// ✓ Complex aggregation logic encapsulated in database
    /// ✓ Can be indexed and optimized by query planner
    /// ✓ Reusable across multiple queries
    /// ✗ Business logic in database (harder to test)
    /// ✗ Less portable across database platforms
    ///
    /// ALTERNATIVE: Use LINQ with GroupBy, aggregations (more portable but may be slower)
    /// </summary>
    public async Task<MemberStatistics?> GetStatisticsAsync(
        int memberId,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        // EF Core cannot directly call table-valued functions in LINQ
        // Solution: Use FromSqlRaw to execute the function
        //
        // Note: This requires a DbSet<MemberStatistics> or using Database.SqlQueryRaw<T>()
        // For now, we'll use raw ADO.NET within the transaction

        // LIMITATION: EF Core 8.0 has limited support for TVFs
        // Best approach: Use ADO.NET within the transaction for complex TVF calls
        using var command = new SqlCommand("SELECT * FROM dbo.fn_GetMemberStatistics(@MemberId)", transaction.Connection, transaction);
        command.Parameters.Add("@MemberId", SqlDbType.Int).Value = memberId;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return MemberStatistics.FromDatabase(
                memberId: reader.GetInt32(reader.GetOrdinal("MemberId")),
                totalBooksLoaned: reader.GetInt32(reader.GetOrdinal("TotalBooksLoaned")),
                activeLoans: reader.GetInt32(reader.GetOrdinal("ActiveLoans")),
                overdueLoans: reader.GetInt32(reader.GetOrdinal("OverdueLoans")),
                returnedLateCount: reader.GetInt32(reader.GetOrdinal("ReturnedLateCount")),
                totalLateFees: reader.GetDecimal(reader.GetOrdinal("TotalLateFees")),
                unpaidLateFees: reader.GetDecimal(reader.GetOrdinal("UnpaidLateFees")),
                avgLoanDurationDays: reader.IsDBNull(reader.GetOrdinal("AvgLoanDurationDays")) ?
                    null : reader.GetInt32(reader.GetOrdinal("AvgLoanDurationDays")),
                lastBorrowDate: reader.IsDBNull(reader.GetOrdinal("LastBorrowDate")) ?
                    null : reader.GetDateTime(reader.GetOrdinal("LastBorrowDate")),
                totalRenewals: reader.GetInt32(reader.GetOrdinal("TotalRenewals")),
                lostOrDamagedCount: reader.GetInt32(reader.GetOrdinal("LostOrDamagedCount"))
            );
        }

        return null;
    }

    #endregion

    #region Additional Member-Specific Queries

    /// <summary>
    /// Gets members with expired memberships.
    ///
    /// LINQ PATTERN: DateTime comparison
    /// - EF translates to SQL: WHERE MembershipExpiresAt < GETDATE()
    /// </summary>
    public async Task<List<Member>> GetExpiredMembershipsAsync(
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var now = DateTime.UtcNow;

        var efMembers = await _context.Members
            .AsNoTracking()
            .Where(m => m.MembershipExpiresAt < now)
            .OrderBy(m => m.MembershipExpiresAt)
            .ToListAsync(cancellationToken);

        return efMembers.ToDomain();
    }

    /// <summary>
    /// Gets members with outstanding fees.
    ///
    /// LINQ PATTERN: Numeric comparison
    /// - WHERE OutstandingFees > 0
    /// </summary>
    public async Task<List<Member>> GetMembersWithFeesAsync(
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var efMembers = await _context.Members
            .AsNoTracking()
            .Where(m => m.OutstandingFees > 0)
            .OrderByDescending(m => m.OutstandingFees)
            .ToListAsync(cancellationToken);

        return efMembers.ToDomain();
    }

    #endregion
}
