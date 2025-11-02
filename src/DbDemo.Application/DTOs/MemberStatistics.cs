namespace DbDemo.Application.DTOs;

using DbDemo.Domain.Entities;

/// <summary>
/// DTO representing comprehensive statistics for a library member.
/// Returned by the fn_GetMemberStatistics table-valued function.
/// </summary>
public class MemberStatistics
{
    /// <summary>
    /// The member ID these statistics belong to.
    /// </summary>
    public int MemberId { get; init; }

    /// <summary>
    /// Total number of books ever loaned by this member (including returned books).
    /// </summary>
    public int TotalBooksLoaned { get; init; }

    /// <summary>
    /// Number of currently active loans (books not yet returned).
    /// </summary>
    public int ActiveLoans { get; init; }

    /// <summary>
    /// Number of currently overdue loans.
    /// </summary>
    public int OverdueLoans { get; init; }

    /// <summary>
    /// Number of loans that were returned late.
    /// </summary>
    public int ReturnedLateCount { get; init; }

    /// <summary>
    /// Total late fees accumulated by this member (paid and unpaid).
    /// </summary>
    public decimal TotalLateFees { get; init; }

    /// <summary>
    /// Total unpaid late fees currently owed.
    /// </summary>
    public decimal UnpaidLateFees { get; init; }

    /// <summary>
    /// Average loan duration in days for returned books.
    /// NULL if the member has never returned a book.
    /// </summary>
    public int? AvgLoanDurationDays { get; init; }

    /// <summary>
    /// The date of the member's most recent loan.
    /// NULL if the member has never borrowed a book.
    /// </summary>
    public DateTime? LastBorrowDate { get; init; }

    /// <summary>
    /// Total number of times this member has renewed their loans.
    /// </summary>
    public int TotalRenewals { get; init; }

    /// <summary>
    /// Number of books marked as lost or damaged by this member.
    /// </summary>
    public int LostOrDamagedCount { get; init; }

    /// <summary>
    /// Creates a MemberStatistics instance from database reader results.
    /// </summary>
    public static MemberStatistics FromDatabase(
        int memberId,
        int totalBooksLoaned,
        int activeLoans,
        int overdueLoans,
        int returnedLateCount,
        decimal totalLateFees,
        decimal unpaidLateFees,
        int? avgLoanDurationDays,
        DateTime? lastBorrowDate,
        int totalRenewals,
        int lostOrDamagedCount)
    {
        return new MemberStatistics
        {
            MemberId = memberId,
            TotalBooksLoaned = totalBooksLoaned,
            ActiveLoans = activeLoans,
            OverdueLoans = overdueLoans,
            ReturnedLateCount = returnedLateCount,
            TotalLateFees = totalLateFees,
            UnpaidLateFees = unpaidLateFees,
            AvgLoanDurationDays = avgLoanDurationDays,
            LastBorrowDate = lastBorrowDate,
            TotalRenewals = totalRenewals,
            LostOrDamagedCount = lostOrDamagedCount
        };
    }

    /// <summary>
    /// Returns a formatted summary of the member's borrowing statistics.
    /// </summary>
    public override string ToString()
    {
        return $"Member {MemberId}: {TotalBooksLoaned} total loans, {ActiveLoans} active, {OverdueLoans} overdue, £{UnpaidLateFees:F2} owed";
    }

    /// <summary>
    /// Returns a detailed multi-line description of all member statistics.
    /// </summary>
    public string ToDetailedString()
    {
        return $@"Member Statistics (ID: {MemberId})
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Borrowing History:
  • Total Books Loaned: {TotalBooksLoaned}
  • Active Loans: {ActiveLoans}
  • Overdue Loans: {OverdueLoans}
  • Returned Late: {ReturnedLateCount}
  • Total Renewals: {TotalRenewals}
  • Lost/Damaged Books: {LostOrDamagedCount}

Financial:
  • Total Late Fees: £{TotalLateFees:F2}
  • Unpaid Fees: £{UnpaidLateFees:F2}

Performance:
  • Avg Loan Duration: {(AvgLoanDurationDays.HasValue ? $"{AvgLoanDurationDays.Value} days" : "N/A")}
  • Last Borrow Date: {(LastBorrowDate.HasValue ? LastBorrowDate.Value.ToString("yyyy-MM-dd") : "Never")}";
    }

    /// <summary>
    /// Indicates whether the member has a good borrowing record (no overdue loans, low late fees).
    /// </summary>
    public bool IsGoodStanding => OverdueLoans == 0 && UnpaidLateFees == 0 && LostOrDamagedCount == 0;

    /// <summary>
    /// Indicates whether the member is an active borrower (has borrowed in the last 90 days).
    /// </summary>
    public bool IsActiveBorrower => LastBorrowDate.HasValue &&
                                     LastBorrowDate.Value >= DateTime.UtcNow.AddDays(-90);
}
