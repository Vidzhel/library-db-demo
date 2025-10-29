using DbDemo.ConsoleApp.Infrastructure.Repositories;
using DbDemo.ConsoleApp.Models;

namespace DbDemo.ConsoleApp.Services;

/// <summary>
/// ⚠️ WARNING: This is an ANTI-PATTERN demonstration!
/// This service performs multi-step operations WITHOUT transaction support.
/// This can lead to data inconsistency and corruption.
/// See docs/20-transaction-problem.md for details.
/// This will be fixed in Commit 22.
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
    /// ⚠️ DANGER: Multi-step operation WITHOUT transaction!
    /// Steps:
    ///   1. Validate member eligibility
    ///   2. Check book availability
    ///   3. Decrement book available copies
    ///   4. Create loan record
    ///
    /// Problem: If step 4 fails, step 3 is already committed!
    /// This leaves the database in an inconsistent state.
    /// </summary>
    public async Task<Loan> CreateLoanAsync(int memberId, int bookId, CancellationToken cancellationToken = default)
    {
        // Step 1: Validate member exists and is eligible
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken);
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
        var activeLoans = await _loanRepository.GetActiveLoansByMemberIdAsync(memberId, cancellationToken);
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

        // Step 2: Check book availability
        var book = await _bookRepository.GetByIdAsync(bookId, cancellationToken);
        if (book == null)
        {
            throw new InvalidOperationException($"Book with ID {bookId} not found.");
        }

        if (book.IsDeleted)
        {
            throw new InvalidOperationException($"Book '{book.Title}' is no longer available in the catalog.");
        }

        if (book.AvailableCopies <= 0)
        {
            throw new InvalidOperationException($"Book '{book.Title}' is not available. All copies are currently on loan.");
        }

        // Step 3: Decrement available copies
        // ⚠️ DANGER: This update is committed immediately without a transaction!
        book.BorrowCopy();
        await _bookRepository.UpdateAsync(book, null, cancellationToken);

        // ⚠️ CRITICAL PROBLEM: If the loan creation below fails (step 4),
        // the book's available copies have already been decremented (step 3).
        // This creates data inconsistency!

        // Step 4: Create loan record
        var loan = Loan.Create(memberId, bookId);

        // If this fails, we're in trouble - book copies were already decremented!
        var createdLoan = await _loanRepository.CreateAsync(loan, null, cancellationToken);

        return createdLoan;
    }

    /// <summary>
    /// ⚠️ DANGER: Multi-step operation WITHOUT transaction!
    /// Steps:
    ///   1. Get loan and validate it exists
    ///   2. Mark loan as returned
    ///   3. Increment book available copies
    ///
    /// Problem: If step 3 fails, step 2 is already committed!
    /// </summary>
    public async Task<Loan> ReturnLoanAsync(int loanId, CancellationToken cancellationToken = default)
    {
        // Step 1: Get loan
        var loan = await _loanRepository.GetByIdAsync(loanId, cancellationToken);
        if (loan == null)
        {
            throw new InvalidOperationException($"Loan with ID {loanId} not found.");
        }

        if (loan.Status != LoanStatus.Active && loan.Status != LoanStatus.Overdue)
        {
            throw new InvalidOperationException($"Loan {loanId} cannot be returned. Current status: {loan.Status}");
        }

        // Step 2: Mark loan as returned
        // ⚠️ DANGER: This update is committed immediately without a transaction!
        loan.Return();
        await _loanRepository.UpdateAsync(loan, null, cancellationToken);

        // ⚠️ CRITICAL PROBLEM: If the book update below fails (step 3),
        // the loan is already marked as returned (step 2).
        // The book's available copies won't be incremented!

        // Step 3: Increment book available copies
        var book = await _bookRepository.GetByIdAsync(loan.BookId, cancellationToken);
        if (book == null)
        {
            throw new InvalidOperationException($"Book with ID {loan.BookId} not found.");
        }

        // If this fails, the loan is marked returned but book copies weren't incremented!
        book.ReturnCopy();
        await _bookRepository.UpdateAsync(book, null, cancellationToken);

        return loan;
    }

    /// <summary>
    /// Renews a loan by extending the due date.
    /// This is a single-step operation, so it doesn't demonstrate the transaction problem as clearly.
    /// </summary>
    public async Task<Loan> RenewLoanAsync(int loanId, CancellationToken cancellationToken = default)
    {
        var loan = await _loanRepository.GetByIdAsync(loanId, cancellationToken);
        if (loan == null)
        {
            throw new InvalidOperationException($"Loan with ID {loanId} not found.");
        }

        if (loan.Status != LoanStatus.Active)
        {
            throw new InvalidOperationException($"Only active loans can be renewed. Current status: {loan.Status}");
        }

        // Check renewal limit
        const int maxRenewals = 3;
        if (loan.RenewalCount >= maxRenewals)
        {
            throw new InvalidOperationException($"Loan has reached the maximum number of renewals ({maxRenewals}).");
        }

        // Extend due date by 14 days
        loan.Renew(14);
        await _loanRepository.UpdateAsync(loan, null, cancellationToken);

        return loan;
    }

    /// <summary>
    /// Gets all active loans for a member.
    /// </summary>
    public async Task<List<Loan>> GetActiveLoansByMemberAsync(int memberId, CancellationToken cancellationToken = default)
    {
        return await _loanRepository.GetActiveLoansByMemberIdAsync(memberId, cancellationToken);
    }

    /// <summary>
    /// Gets all overdue loans.
    /// </summary>
    public async Task<List<Loan>> GetOverdueLoansAsync(CancellationToken cancellationToken = default)
    {
        return await _loanRepository.GetOverdueLoansAsync(cancellationToken);
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
