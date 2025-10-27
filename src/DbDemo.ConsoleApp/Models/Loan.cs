namespace DbDemo.ConsoleApp.Models;

public class Loan
{
    private const decimal LateFeePerDay = 0.50m;
    private const int DefaultLoanPeriodDays = 14;

    private Loan() { }

    private Loan(int memberId, int bookId, DateTime borrowedAt, DateTime dueDate)
    {
        MemberId = memberId;
        BookId = bookId;
        BorrowedAt = borrowedAt;
        DueDate = dueDate;
        Status = LoanStatus.Active;
        RenewalCount = 0;
        MaxRenewalsAllowed = 2;
        IsFeePaid = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Loan Create(int memberId, int bookId)
    {
        var now = DateTime.UtcNow;
        return new Loan(memberId, bookId, now, now.AddDays(DefaultLoanPeriodDays));
    }

    public int Id { get; private set; }
    public int MemberId { get; private set; }
    public int BookId { get; private set; }
    public DateTime BorrowedAt { get; private set; }
    public DateTime DueDate { get; private set; }
    public DateTime? ReturnedAt { get; private set; }
    public LoanStatus Status { get; private set; }
    public decimal? LateFee { get; private set; }
    public bool IsFeePaid { get; private set; }
    public int RenewalCount { get; private set; }
    public int MaxRenewalsAllowed { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public Member? Member { get; private set; }
    public Book? Book { get; private set; }

    public bool IsOverdue
    {
        get
        {
            if (ReturnedAt.HasValue)
                return false;

            return DateTime.UtcNow > DueDate;
        }
    }

    public int DaysOverdue
    {
        get
        {
            if (ReturnedAt.HasValue || !IsOverdue)
                return 0;

            return (DateTime.UtcNow - DueDate).Days;
        }
    }

    public bool CanBeRenewed => RenewalCount < MaxRenewalsAllowed && !IsOverdue && Status == LoanStatus.Active;

    public void Renew(int additionalDays = DefaultLoanPeriodDays)
    {
        if (!CanBeRenewed)
            throw new InvalidOperationException("Loan cannot be renewed");

        if (additionalDays <= 0)
            throw new ArgumentException("Additional days must be positive", nameof(additionalDays));

        DueDate = DueDate.AddDays(additionalDays);
        RenewalCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Return()
    {
        if (ReturnedAt.HasValue)
            throw new InvalidOperationException("Loan has already been returned");

        // Check if overdue BEFORE setting ReturnedAt
        var wasOverdue = IsOverdue;
        ReturnedAt = DateTime.UtcNow;

        if (wasOverdue)
        {
            Status = LoanStatus.ReturnedLate;
            LateFee = CalculateLateFee();
        }
        else
        {
            Status = LoanStatus.Returned;
            LateFee = 0;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public decimal CalculateLateFee()
    {
        if (!IsOverdue && !ReturnedAt.HasValue)
            return 0;

        var endDate = ReturnedAt ?? DateTime.UtcNow;
        var daysLate = (endDate - DueDate).Days;

        if (daysLate <= 0)
            return 0;

        return daysLate * LateFeePerDay;
    }

    public void MarkAsLost()
    {
        if (ReturnedAt.HasValue)
            throw new InvalidOperationException("Cannot mark returned loan as lost");

        Status = LoanStatus.Lost;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsDamaged(string damageNotes)
    {
        if (!ReturnedAt.HasValue)
            throw new InvalidOperationException("Book must be returned before marking as damaged");

        Status = LoanStatus.Damaged;
        Notes = damageNotes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void PayLateFee()
    {
        if (!LateFee.HasValue || LateFee.Value == 0)
            throw new InvalidOperationException("No late fee to pay");

        IsFeePaid = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public override string ToString()
    {
        var statusText = ReturnedAt.HasValue ? "Returned" : (IsOverdue ? "Overdue" : "Active");
        return $"Loan #{Id}: Book {BookId} to Member {MemberId} - {statusText}";
    }

    /// <summary>
    /// Internal factory method for repository hydration - bypasses validation since data comes from database
    /// </summary>
    internal static Loan FromDatabase(
        int id,
        int memberId,
        int bookId,
        DateTime borrowedAt,
        DateTime dueDate,
        DateTime? returnedAt,
        LoanStatus status,
        decimal? lateFee,
        bool isFeePaid,
        int renewalCount,
        int maxRenewalsAllowed,
        string? notes,
        DateTime createdAt,
        DateTime updatedAt)
    {
        var loan = new Loan();
        loan.Id = id;
        loan.MemberId = memberId;
        loan.BookId = bookId;
        loan.BorrowedAt = borrowedAt;
        loan.DueDate = dueDate;
        loan.ReturnedAt = returnedAt;
        loan.Status = status;
        loan.LateFee = lateFee;
        loan.IsFeePaid = isFeePaid;
        loan.RenewalCount = renewalCount;
        loan.MaxRenewalsAllowed = maxRenewalsAllowed;
        loan.Notes = notes;
        loan.CreatedAt = createdAt;
        loan.UpdatedAt = updatedAt;
        return loan;
    }
}

public enum LoanStatus
{
    Active = 0,
    Returned = 1,
    Overdue = 2,
    ReturnedLate = 3,
    Lost = 4,
    Damaged = 5,
    Cancelled = 6
}
