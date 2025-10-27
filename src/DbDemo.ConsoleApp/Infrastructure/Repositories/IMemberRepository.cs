using DbDemo.ConsoleApp.Models;

namespace DbDemo.ConsoleApp.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Member entity operations
/// Provides abstraction over data access layer
/// </summary>
public interface IMemberRepository
{
    /// <summary>
    /// Creates a new member in the database
    /// </summary>
    /// <param name="member">The member to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created member with its assigned ID</returns>
    Task<Member> CreateAsync(Member member, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a member by its ID
    /// </summary>
    /// <param name="id">The member ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The member if found, null otherwise</returns>
    Task<Member?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a member by their membership number
    /// </summary>
    /// <param name="membershipNumber">The unique membership number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The member if found, null otherwise</returns>
    Task<Member?> GetByMembershipNumberAsync(string membershipNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a member by email
    /// </summary>
    /// <param name="email">The member email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The member if found, null otherwise</returns>
    Task<Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of members
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="activeOnly">Whether to return only active members</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of members</returns>
    Task<List<Member>> GetPagedAsync(
        int pageNumber = 1,
        int pageSize = 10,
        bool activeOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for members by name (first name or last name)
    /// </summary>
    /// <param name="searchTerm">The search term to match against member names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching members</returns>
    Task<List<Member>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of members
    /// </summary>
    /// <param name="activeOnly">Whether to count only active members</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count of members</returns>
    Task<int> GetCountAsync(bool activeOnly = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing member
    /// </summary>
    /// <param name="member">The member with updated data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if member not found</returns>
    Task<bool> UpdateAsync(Member member, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a member
    /// </summary>
    /// <param name="id">The member ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if member not found</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
