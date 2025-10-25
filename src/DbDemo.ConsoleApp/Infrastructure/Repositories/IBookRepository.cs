using DbDemo.ConsoleApp.Models;

namespace DbDemo.ConsoleApp.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Book entity operations
/// Provides abstraction over data access layer
/// </summary>
public interface IBookRepository
{
    /// <summary>
    /// Creates a new book in the database
    /// </summary>
    /// <param name="book">The book to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created book with its assigned ID</returns>
    Task<Book> CreateAsync(Book book, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a book by its ID
    /// </summary>
    /// <param name="id">The book ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The book if found, null otherwise</returns>
    Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a book by its ISBN
    /// </summary>
    /// <param name="isbn">The book ISBN</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The book if found, null otherwise</returns>
    Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of books
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="includeDeleted">Whether to include deleted books</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of books</returns>
    Task<List<Book>> GetPagedAsync(
        int pageNumber = 1,
        int pageSize = 10,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for books by title
    /// </summary>
    /// <param name="searchTerm">The search term to match against book titles</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching books</returns>
    Task<List<Book>> SearchByTitleAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets books by category
    /// </summary>
    /// <param name="categoryId">The category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of books in the category</returns>
    Task<List<Book>> GetByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of books
    /// </summary>
    /// <param name="includeDeleted">Whether to include deleted books in the count</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count of books</returns>
    Task<int> GetCountAsync(bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing book
    /// </summary>
    /// <param name="book">The book with updated data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if book not found</returns>
    Task<bool> UpdateAsync(Book book, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a book (marks as deleted)
    /// </summary>
    /// <param name="id">The book ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if book not found</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
