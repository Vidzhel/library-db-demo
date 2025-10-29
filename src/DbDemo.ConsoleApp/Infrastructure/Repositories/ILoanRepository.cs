using DbDemo.ConsoleApp.Models;
using Microsoft.Data.SqlClient;

namespace DbDemo.ConsoleApp.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Loan entity operations
/// Provides abstraction over data access layer
/// </summary>
public interface ILoanRepository
{
    /// <summary>
    /// Creates a new loan in the database
    /// </summary>
    /// <param name="loan">The loan to create</param>
    /// <param name="transaction">Optional transaction to participate in</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created loan with its assigned ID</returns>
    Task<Loan> CreateAsync(Loan loan, SqlTransaction? transaction = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a loan by its ID
    /// </summary>
    /// <param name="id">The loan ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The loan if found, null otherwise</returns>
    Task<Loan?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of loans
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of loans</returns>
    Task<List<Loan>> GetPagedAsync(
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active loans for a specific member
    /// </summary>
    /// <param name="memberId">The member ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active loans</returns>
    Task<List<Loan>> GetActiveLoansByMemberIdAsync(int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all overdue loans (loans past due date and not returned)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of overdue loans</returns>
    Task<List<Loan>> GetOverdueLoansAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the complete loan history for a member
    /// </summary>
    /// <param name="memberId">The member ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all loans for the member</returns>
    Task<List<Loan>> GetLoanHistoryByMemberIdAsync(int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the loan history for a specific book
    /// </summary>
    /// <param name="bookId">The book ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all loans for the book</returns>
    Task<List<Loan>> GetLoanHistoryByBookIdAsync(int bookId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of loans
    /// </summary>
    /// <param name="status">Optional loan status filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count of loans</returns>
    Task<int> GetCountAsync(LoanStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing loan
    /// </summary>
    /// <param name="loan">The loan with updated data</param>
    /// <param name="transaction">Optional transaction to participate in</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if loan not found</returns>
    Task<bool> UpdateAsync(Loan loan, SqlTransaction? transaction = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a loan
    /// </summary>
    /// <param name="id">The loan ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if loan not found</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
