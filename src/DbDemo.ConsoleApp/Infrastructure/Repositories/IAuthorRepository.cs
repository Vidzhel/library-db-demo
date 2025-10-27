using DbDemo.ConsoleApp.Models;

namespace DbDemo.ConsoleApp.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Author entity operations
/// Provides abstraction over data access layer
/// </summary>
public interface IAuthorRepository
{
    /// <summary>
    /// Creates a new author in the database
    /// </summary>
    /// <param name="author">The author to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created author with its assigned ID</returns>
    Task<Author> CreateAsync(Author author, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an author by its ID
    /// </summary>
    /// <param name="id">The author ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The author if found, null otherwise</returns>
    Task<Author?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an author by email
    /// </summary>
    /// <param name="email">The author email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The author if found, null otherwise</returns>
    Task<Author?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of authors
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of authors</returns>
    Task<List<Author>> GetPagedAsync(
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for authors by name (first name or last name)
    /// </summary>
    /// <param name="searchTerm">The search term to match against author names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching authors</returns>
    Task<List<Author>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of authors
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count of authors</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing author
    /// </summary>
    /// <param name="author">The author with updated data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if author not found</returns>
    Task<bool> UpdateAsync(Author author, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an author
    /// </summary>
    /// <param name="id">The author ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if author not found</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
