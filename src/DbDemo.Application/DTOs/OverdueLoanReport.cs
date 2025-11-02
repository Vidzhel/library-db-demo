namespace DbDemo.Application.DTOs;

using DbDemo.Domain.Entities;

/// <summary>
/// DTO representing an overdue loan report entry returned by sp_GetOverdueLoans.
/// This model combines data from Loans, Members, and Books tables.
/// </summary>
public class OverdueLoanReport
{
    // Loan information
    public int LoanId { get; init; }
    public DateTime BorrowedAt { get; init; }
    public DateTime DueDate { get; init; }
    public int DaysOverdue { get; init; }
    public decimal CalculatedLateFee { get; init; }
    public LoanStatus Status { get; init; }
    public string? Notes { get; init; }

    // Member information
    public int MemberId { get; init; }
    public string MemberName { get; init; } = string.Empty;
    public string MemberEmail { get; init; } = string.Empty;
    public string? MemberPhone { get; init; }

    // Book information
    public int BookId { get; init; }
    public string ISBN { get; init; } = string.Empty;
    public string BookTitle { get; init; } = string.Empty;
    public string? Publisher { get; init; }

    /// <summary>
    /// Creates an OverdueLoanReport instance from database reader results.
    /// </summary>
    public static OverdueLoanReport FromDatabase(
        int loanId,
        int memberId,
        string memberName,
        string memberEmail,
        string? memberPhone,
        int bookId,
        string isbn,
        string bookTitle,
        string? publisher,
        DateTime borrowedAt,
        DateTime dueDate,
        int daysOverdue,
        decimal calculatedLateFee,
        LoanStatus status,
        string? notes)
    {
        return new OverdueLoanReport
        {
            LoanId = loanId,
            MemberId = memberId,
            MemberName = memberName,
            MemberEmail = memberEmail,
            MemberPhone = memberPhone,
            BookId = bookId,
            ISBN = isbn,
            BookTitle = bookTitle,
            Publisher = publisher,
            BorrowedAt = borrowedAt,
            DueDate = dueDate,
            DaysOverdue = daysOverdue,
            CalculatedLateFee = calculatedLateFee,
            Status = status,
            Notes = notes
        };
    }

    /// <summary>
    /// Returns a formatted summary of the overdue loan.
    /// </summary>
    public override string ToString()
    {
        return $"[{DaysOverdue}d overdue] {MemberName} - \"{BookTitle}\" (Due: {DueDate:yyyy-MM-dd}, Fee: £{CalculatedLateFee:F2})";
    }

    /// <summary>
    /// Returns a detailed multi-line description of the overdue loan.
    /// </summary>
    public string ToDetailedString()
    {
        return $@"Loan ID: {LoanId}
Member: {MemberName} ({MemberEmail})
Book: ""{BookTitle}"" (ISBN: {ISBN})
Borrowed: {BorrowedAt:yyyy-MM-dd}
Due Date: {DueDate:yyyy-MM-dd}
Days Overdue: {DaysOverdue}
Calculated Late Fee: £{CalculatedLateFee:F2}
Status: {Status}
Notes: {Notes ?? "None"}";
    }
}
