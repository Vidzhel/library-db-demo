using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient;

namespace DbDemo.Application.Repositories;

using DbDemo.Domain.Entities;
using DbDemo.Application.DTOs;

/// <summary>
/// Repository interface for Loan entity operations
/// Provides abstraction over data access layer
/// All methods require an active SqlTransaction to ensure proper transaction management
/// </summary>
public interface ILoanRepository
{
    /// <summary>
    /// Creates a new loan in the database
    /// </summary>
    /// <param name="loan">The loan to create</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created loan with its assigned ID</returns>
    Task<Loan> CreateAsync(Loan loan, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a loan by its ID
    /// </summary>
    /// <param name="id">The loan ID</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The loan if found, null otherwise</returns>
    Task<Loan?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of loans
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of loans</returns>
    Task<List<Loan>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active loans for a specific member
    /// </summary>
    /// <param name="memberId">The member ID</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active loans</returns>
    Task<List<Loan>> GetActiveLoansByMemberIdAsync(int memberId, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all overdue loans (loans past due date and not returned)
    /// </summary>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of overdue loans</returns>
    Task<List<Loan>> GetOverdueLoansAsync(SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the complete loan history for a member
    /// </summary>
    /// <param name="memberId">The member ID</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all loans for the member</returns>
    Task<List<Loan>> GetLoanHistoryByMemberIdAsync(int memberId, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the loan history for a specific book
    /// </summary>
    /// <param name="bookId">The book ID</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all loans for the book</returns>
    Task<List<Loan>> GetLoanHistoryByBookIdAsync(int bookId, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of loans
    /// </summary>
    /// <param name="status">Optional loan status filter</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count of loans</returns>
    Task<int> GetCountAsync(LoanStatus? status, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing loan
    /// </summary>
    /// <param name="loan">The loan with updated data</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if loan not found</returns>
    Task<bool> UpdateAsync(Loan loan, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a loan
    /// </summary>
    /// <param name="id">The loan ID to delete</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if loan not found</returns>
    Task<bool> DeleteAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an overdue loans report by calling the sp_GetOverdueLoans stored procedure.
    /// Returns detailed information about overdue loans including member and book details.
    /// </summary>
    /// <param name="asOfDate">Optional date to check for overdue status (defaults to current UTC time if null)</param>
    /// <param name="minDaysOverdue">Minimum number of days overdue to include (defaults to 0)</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing the list of overdue loan reports and total count</returns>
    Task<(List<OverdueLoanReport> Loans, int TotalCount)> GetOverdueLoansReportAsync(
        DateTime? asOfDate,
        int minDaysOverdue,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the late fee for a loan using the fn_CalculateLateFee scalar function.
    /// The fee is calculated based on the number of days overdue multiplied by the late fee per day rate.
    /// </summary>
    /// <param name="loanId">The ID of the loan to calculate the late fee for</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The calculated late fee amount</returns>
    Task<decimal> CalculateLateFeeAsync(
        int loanId,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default);
}
