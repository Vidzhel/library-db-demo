using DbDemo.ConsoleApp.Models;
using Microsoft.Data.SqlClient;

namespace DbDemo.ConsoleApp.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Book entity operations
/// Provides abstraction over data access layer
/// All methods require an active SqlTransaction to ensure proper transaction management
/// </summary>
public interface IBookRepository
{
    /// <summary>
    /// Creates a new book in the database
    /// </summary>
    /// <param name="book">The book to create</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created book with its assigned ID</returns>
    Task<Book> CreateAsync(Book book, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a book by its ID
    /// </summary>
    /// <param name="id">The book ID</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The book if found, null otherwise</returns>
    Task<Book?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a book by its ISBN
    /// </summary>
    /// <param name="isbn">The book ISBN</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The book if found, null otherwise</returns>
    Task<Book?> GetByIsbnAsync(string isbn, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of books
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="includeDeleted">Whether to include deleted books</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of books</returns>
    Task<List<Book>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        bool includeDeleted,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for books by title
    /// </summary>
    /// <param name="searchTerm">The search term to match against book titles</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching books</returns>
    Task<List<Book>> SearchByTitleAsync(string searchTerm, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets books by category
    /// </summary>
    /// <param name="categoryId">The category ID</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of books in the category</returns>
    Task<List<Book>> GetByCategoryAsync(int categoryId, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of books
    /// </summary>
    /// <param name="includeDeleted">Whether to include deleted books in the count</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count of books</returns>
    Task<int> GetCountAsync(bool includeDeleted, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing book
    /// </summary>
    /// <param name="book">The book with updated data</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if book not found</returns>
    Task<bool> UpdateAsync(Book book, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically decrements available copies for a book.
    /// This operation checks book availability and decrements in a single atomic UPDATE statement
    /// to prevent Time-of-Check to Time-of-Use (TOCTOU) race conditions.
    /// </summary>
    /// <param name="bookId">The book ID</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if book was available and decremented, false if book unavailable or deleted</returns>
    Task<bool> BorrowCopyAsync(int bookId, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically increments available copies for a book when it's returned.
    /// This operation increments in a single atomic UPDATE statement.
    /// </summary>
    /// <param name="bookId">The book ID</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if book was incremented, false if book not found</returns>
    Task<bool> ReturnCopyAsync(int bookId, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a book (marks as deleted)
    /// </summary>
    /// <param name="id">The book ID to delete</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if book not found</returns>
    Task<bool> DeleteAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default);
}
