using DbDemo.Application.Repositories;
using DbDemo.Domain.Entities;
using DbDemo.Infrastructure.SqlKata.Generated;
using Microsoft.Data.SqlClient;
using SqlKata.Execution;

namespace DbDemo.Infrastructure.SqlKata.Repositories;

/// <summary>
/// SqlKata implementation of IMemberRepository
/// Simplified example showing the pattern - other repositories follow the same approach
/// </summary>
public class MemberRepository : IMemberRepository
{
    public async Task<Member> CreateAsync(Member member, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        var factory = QueryFactoryProvider.Create(transaction);

        var insertData = new Dictionary<string, object?>
        {
            [Columns.Members.MembershipNumber] = member.MembershipNumber,
            [Columns.Members.FirstName] = member.FirstName,
            [Columns.Members.LastName] = member.LastName,
            [Columns.Members.Email] = member.Email,
            [Columns.Members.PhoneNumber] = member.PhoneNumber,
            [Columns.Members.DateOfBirth] = member.DateOfBirth,
            [Columns.Members.Address] = member.Address,
            [Columns.Members.MemberSince] = member.MemberSince,
            [Columns.Members.MembershipExpiresAt] = member.MembershipExpiresAt,
            [Columns.Members.IsActive] = member.IsActive,
            [Columns.Members.MaxBooksAllowed] = member.MaxBooksAllowed,
            [Columns.Members.OutstandingFees] = member.OutstandingFees,
            [Columns.Members.CreatedAt] = member.CreatedAt,
            [Columns.Members.UpdatedAt] = member.UpdatedAt
        };

        var query = factory.Query(Tables.Members).AsInsert(insertData);
        var sql = factory.Compiler.Compile(query).Sql + "; SELECT CAST(SCOPE_IDENTITY() AS INT);";
        var bindings = factory.Compiler.Compile(query).Bindings;

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        for (int i = 0; i < bindings.Count; i++)
        {
            command.Parameters.AddWithValue($"@p{i}", bindings[i] ?? DBNull.Value);
        }

        var newId = (int)await command.ExecuteScalarAsync(cancellationToken);
        return await GetByIdAsync(newId, transaction, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve newly created member");
    }

    public async Task<Member?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        var factory = QueryFactoryProvider.Create(transaction);

        var result = await factory
            .Query(Tables.Members)
            .Select(GetMemberColumns())
            .Where(Columns.Members.Id, id)
            .FirstOrDefaultAsync<dynamic>(transaction: transaction, cancellationToken: cancellationToken);

        return result != null ? MapDynamicToMember(result) : null;
    }

    public async Task<Member?> GetByMembershipNumberAsync(string membershipNumber, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        var factory = QueryFactoryProvider.Create(transaction);

        var result = await factory
            .Query(Tables.Members)
            .Select(GetMemberColumns())
            .Where(Columns.Members.MembershipNumber, membershipNumber)
            .FirstOrDefaultAsync<dynamic>(transaction: transaction, cancellationToken: cancellationToken);

        return result != null ? MapDynamicToMember(result) : null;
    }

    public async Task<List<Member>> GetActiveMembers(SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        var factory = QueryFactoryProvider.Create(transaction);

        var results = await factory
            .Query(Tables.Members)
            .Select(GetMemberColumns())
            .Where(Columns.Members.IsActive, true)
            .OrderBy(Columns.Members.LastName)
            .OrderBy(Columns.Members.FirstName)
            .GetAsync<dynamic>(transaction: transaction, cancellationToken: cancellationToken);

        var members = new List<Member>();
        foreach (var result in results)
        {
            members.Add(MapDynamicToMember(result));
        }
        return members;
    }

    public async Task<bool> UpdateAsync(Member member, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        var factory = QueryFactoryProvider.Create(transaction);

        var updateData = new Dictionary<string, object?>
        {
            [Columns.Members.MembershipNumber] = member.MembershipNumber,
            [Columns.Members.FirstName] = member.FirstName,
            [Columns.Members.LastName] = member.LastName,
            [Columns.Members.Email] = member.Email,
            [Columns.Members.PhoneNumber] = member.PhoneNumber,
            [Columns.Members.DateOfBirth] = member.DateOfBirth,
            [Columns.Members.Address] = member.Address,
            [Columns.Members.MemberSince] = member.MemberSince,
            [Columns.Members.MembershipExpiresAt] = member.MembershipExpiresAt,
            [Columns.Members.IsActive] = member.IsActive,
            [Columns.Members.MaxBooksAllowed] = member.MaxBooksAllowed,
            [Columns.Members.OutstandingFees] = member.OutstandingFees,
            [Columns.Members.UpdatedAt] = DateTime.UtcNow
        };

        var affectedRows = await factory
            .Query(Tables.Members)
            .Where(Columns.Members.Id, member.Id)
            .UpdateAsync(updateData, transaction: transaction, cancellationToken: cancellationToken);

        return affectedRows > 0;
    }

    public Task<Member?> GetByEmailAsync(string email, SqlTransaction transaction, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Follow BookRepository pattern");

    public Task<List<Member>> GetPagedAsync(int pageNumber, int pageSize, bool includeInactive, SqlTransaction transaction, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Follow BookRepository pattern");

    public Task<List<Member>> SearchByNameAsync(string searchTerm, SqlTransaction transaction, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Follow BookRepository pattern");

    public Task<int> GetCountAsync(bool includeInactive, SqlTransaction transaction, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Follow BookRepository pattern");

    public Task<List<Member>> GetExpiringMemberships(int daysUntilExpiry, SqlTransaction transaction, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Follow BookRepository pattern");

    public Task<List<Member>> GetMembersWithOutstandingFees(SqlTransaction transaction, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Follow BookRepository pattern");

    public Task<bool> ExtendMembershipAsync(int memberId, DateTime newExpiryDate, SqlTransaction transaction, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Follow BookRepository pattern");

    public Task<bool> AddFeeAsync(int memberId, decimal amount, SqlTransaction transaction, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Follow BookRepository pattern");

    public Task<bool> PayFeeAsync(int memberId, decimal amount, SqlTransaction transaction, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Follow BookRepository pattern");

    public Task<bool> DeleteAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Follow BookRepository pattern");

    public Task<DbDemo.Application.DTOs.MemberStatistics?> GetStatisticsAsync(int memberId, SqlTransaction transaction, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Follow BookRepository pattern");

    private static string[] GetMemberColumns() => new[]
    {
        Columns.Members.Id,
        Columns.Members.MembershipNumber,
        Columns.Members.FirstName,
        Columns.Members.LastName,
        Columns.Members.Email,
        Columns.Members.PhoneNumber,
        Columns.Members.DateOfBirth,
        Columns.Members.Address,
        Columns.Members.MemberSince,
        Columns.Members.MembershipExpiresAt,
        Columns.Members.IsActive,
        Columns.Members.MaxBooksAllowed,
        Columns.Members.OutstandingFees,
        Columns.Members.CreatedAt,
        Columns.Members.UpdatedAt
    };

    private static Member MapDynamicToMember(dynamic row)
    {
        return Member.FromDatabase(
            id: (int)row.Id,
            membershipNumber: (string)row.MembershipNumber,
            firstName: (string)row.FirstName,
            lastName: (string)row.LastName,
            email: (string)row.Email,
            phoneNumber: row.PhoneNumber,
            dateOfBirth: row.DateOfBirth,
            address: row.Address,
            memberSince: (DateTime)row.MemberSince,
            membershipExpiresAt: (DateTime)row.MembershipExpiresAt,
            isActive: (bool)row.IsActive,
            maxBooksAllowed: (int)row.MaxBooksAllowed,
            outstandingFees: (decimal)row.OutstandingFees,
            createdAt: (DateTime)row.CreatedAt,
            updatedAt: (DateTime)row.UpdatedAt
        );
    }
}
