using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient;

namespace DbDemo.Application.Services;

using DbDemo.Domain.Entities;
using DbDemo.Application.Repositories;

/// <summary>
/// Service layer for loan operations with proper transaction management.
///
/// ✅ This service demonstrates CORRECT transaction handling for multi-step operations.
/// All operations that modify multiple tables are wrapped in explicit transactions
/// to ensure atomicity (all-or-nothing behavior).
///
/// See docs/21-transactions.md for detailed explanation of transaction patterns.
/// </summary>
public class LoanService
{
    private readonly ILoanRepository _loanRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly string _connectionString;

    public LoanService(
        ILoanRepository loanRepository,
        IBookRepository bookRepository,
        IMemberRepository memberRepository,
        string connectionString)
    {
        _loanRepository = loanRepository ?? throw new ArgumentNullException(nameof(loanRepository));
        _bookRepository = bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));
        _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Creates a new loan with proper transaction handling.
    ///
    /// ✅ This method demonstrates CORRECT multi-step transaction handling:
    ///   1. Validate member eligibility (within transaction)
    ///   2. Check book availability (within transaction)
    ///   3. Decrement book available copies (within transaction)
    ///   4. Create loan record (within transaction)
    ///
    /// Transaction commit/rollback is handled by the caller at the top level.
    /// </summary>
    public async Task<Loan> CreateLoanAsync(int memberId, int bookId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        // Step 1: Validate member exists and is eligible (within transaction)
        var member = await _memberRepository.GetByIdAsync(memberId, transaction, cancellationToken);
        if (member == null)
        {
            throw new InvalidOperationException($"Member with ID {memberId} not found.");
        }

        if (!member.IsActive)
        {
            throw new InvalidOperationException($"Member {member.MembershipNumber} is not active.");
        }

        if (member.MembershipExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException($"Member {member.MembershipNumber} membership has expired.");
        }

        // Check if member has reached max books limit
        var activeLoans = await _loanRepository.GetActiveLoansByMemberIdAsync(memberId, transaction, cancellationToken);
        if (activeLoans.Count >= member.MaxBooksAllowed)
        {
            throw new InvalidOperationException(
                $"Member {member.MembershipNumber} has reached the maximum limit of {member.MaxBooksAllowed} books.");
        }

        // Check if member has outstanding fees
        if (member.OutstandingFees > 0)
        {
            throw new InvalidOperationException(
                $"Member {member.MembershipNumber} has outstanding fees of ${member.OutstandingFees:F2}. Please clear fees before borrowing.");
        }

        // Step 2: Atomically decrement available copies
        // This single operation checks availability AND decrements in one atomic UPDATE
        // Prevents TOCTOU (Time-of-Check to Time-of-Use) race conditions
        var bookBorrowed = await _bookRepository.BorrowCopyAsync(bookId, transaction, cancellationToken);
        if (!bookBorrowed)
        {
            throw new InvalidOperationException(
                $"Book with ID {bookId} is not available (no copies available, book deleted, or not found).");
        }

        // Step 3: Create loan record (within transaction)
        var loan = Loan.Create(memberId, bookId);
        var createdLoan = await _loanRepository.CreateAsync(loan, transaction, cancellationToken);

        return createdLoan!;
    }

    /// <summary>
    /// Returns a loan with proper transaction handling.
    ///
    /// ✅ This method demonstrates CORRECT multi-step transaction handling:
    ///   1. Get loan and validate it exists (within transaction)
    ///   2. Mark loan as returned (within transaction)
    ///   3. Increment book available copies (within transaction)
    ///
    /// Transaction commit/rollback is handled by the caller at the top level.
    /// </summary>
    public async Task<Loan> ReturnLoanAsync(int loanId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        // Step 1: Get loan and validate (within transaction)
        var loan = await _loanRepository.GetByIdAsync(loanId, transaction, cancellationToken);
        if (loan == null)
        {
            throw new InvalidOperationException($"Loan with ID {loanId} not found.");
        }

        if (loan.Status != LoanStatus.Active && loan.Status != LoanStatus.Overdue)
        {
            throw new InvalidOperationException($"Loan {loanId} cannot be returned. Current status: {loan.Status}");
        }

        // Step 2: Mark loan as returned (within transaction)
        loan.Return();
        await _loanRepository.UpdateAsync(loan, transaction, cancellationToken);

        // Step 3: Atomically increment book available copies (within transaction)
        var bookReturned = await _bookRepository.ReturnCopyAsync(loan.BookId, transaction, cancellationToken);
        if (!bookReturned)
        {
            throw new InvalidOperationException($"Book with ID {loan.BookId} not found.");
        }

        return loan;
    }

    /// <summary>
    /// Renews a loan by extending the due date.
    ///
    /// While this is a single-step operation (only updates one table),
    /// we still use a transaction for consistency and to follow best practices.
    /// In a real application, you might add audit logging or other side effects
    /// that would require transaction coordination.
    ///
    /// Transaction commit/rollback is handled by the caller at the top level.
    /// </summary>
    public async Task<Loan> RenewLoanAsync(int loanId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        // Get loan and validate (within transaction)
        var loan = await _loanRepository.GetByIdAsync(loanId, transaction, cancellationToken);
        if (loan == null)
        {
            throw new InvalidOperationException($"Loan with ID {loanId} not found.");
        }

        if (loan.Status != LoanStatus.Active)
        {
            throw new InvalidOperationException($"Only active loans can be renewed. Current status: {loan.Status}");
        }

        // Extend due date by 14 days
        // Entity validates renewal limits and other business rules
        loan.Renew(14);
        await _loanRepository.UpdateAsync(loan, transaction, cancellationToken);

        return loan;
    }

    /// <summary>
    /// Gets all active loans for a member.
    ///
    /// Transaction commit/rollback is handled by the caller at the top level.
    /// </summary>
    public async Task<List<Loan>> GetActiveLoansByMemberAsync(int memberId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        var loans = await _loanRepository.GetActiveLoansByMemberIdAsync(memberId, transaction, cancellationToken);
        return loans;
    }

    /// <summary>
    /// Gets all overdue loans.
    ///
    /// Transaction commit/rollback is handled by the caller at the top level.
    /// </summary>
    public async Task<List<Loan>> GetOverdueLoansAsync(SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        var loans = await _loanRepository.GetOverdueLoansAsync(transaction, cancellationToken);
        return loans;
    }

    /// <summary>
    /// Calculates the late fee for a loan.
    /// </summary>
    public decimal CalculateLateFee(Loan loan)
    {
        if (loan == null)
        {
            throw new ArgumentNullException(nameof(loan));
        }

        return loan.CalculateLateFee();
    }
}
