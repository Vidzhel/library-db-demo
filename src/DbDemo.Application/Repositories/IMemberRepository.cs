using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient;

namespace DbDemo.Application.Repositories;

using DbDemo.Domain.Entities;
using DbDemo.Application.DTOs;

/// <summary>
/// Repository interface for Member entity operations
/// Provides abstraction over data access layer
/// All methods require an active SqlTransaction to ensure proper transaction management
/// </summary>
public interface IMemberRepository
{
    /// <summary>
    /// Creates a new member in the database
    /// </summary>
    /// <param name="member">The member to create</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created member with its assigned ID</returns>
    Task<Member> CreateAsync(Member member, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a member by its ID
    /// </summary>
    /// <param name="id">The member ID</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The member if found, null otherwise</returns>
    Task<Member?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a member by their membership number
    /// </summary>
    /// <param name="membershipNumber">The unique membership number</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The member if found, null otherwise</returns>
    Task<Member?> GetByMembershipNumberAsync(string membershipNumber, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a member by email
    /// </summary>
    /// <param name="email">The member email</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The member if found, null otherwise</returns>
    Task<Member?> GetByEmailAsync(string email, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of members
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="activeOnly">Whether to return only active members</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of members</returns>
    Task<List<Member>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for members by name (first name or last name)
    /// </summary>
    /// <param name="searchTerm">The search term to match against member names</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching members</returns>
    Task<List<Member>> SearchByNameAsync(string searchTerm, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of members
    /// </summary>
    /// <param name="activeOnly">Whether to count only active members</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count of members</returns>
    Task<int> GetCountAsync(bool activeOnly, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing member
    /// </summary>
    /// <param name="member">The member with updated data</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if member not found</returns>
    Task<bool> UpdateAsync(Member member, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a member
    /// </summary>
    /// <param name="id">The member ID to delete</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if member not found</returns>
    Task<bool> DeleteAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves comprehensive statistics for a member using the fn_GetMemberStatistics table-valued function
    /// </summary>
    /// <param name="memberId">The member ID to get statistics for</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Member statistics if member exists, null otherwise</returns>
    Task<MemberStatistics?> GetStatisticsAsync(int memberId, SqlTransaction transaction, CancellationToken cancellationToken = default);
}
