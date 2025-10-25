namespace DbDemo.ConsoleApp.Models;

public class Member
{
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _email = string.Empty;
    private string _membershipNumber = string.Empty;
    private int _maxBooksAllowed = 5;
    private decimal _outstandingFees;

    private Member() { }

    public Member(string membershipNumber, string firstName, string lastName, string email, DateTime dateOfBirth)
    {
        MembershipNumber = membershipNumber;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        DateOfBirth = dateOfBirth;
        MemberSince = DateTime.UtcNow;
        MembershipExpiresAt = DateTime.UtcNow.AddYears(1);
        IsActive = true;
        MaxBooksAllowed = 5;
        OutstandingFees = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }

    public string MembershipNumber
    {
        get => _membershipNumber;
        private set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Membership number cannot be empty", nameof(MembershipNumber));

            _membershipNumber = value.Trim();
        }
    }

    public string FirstName
    {
        get => _firstName;
        private set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("First name cannot be empty", nameof(FirstName));

            if (value.Length > 50)
                throw new ArgumentException("First name cannot exceed 50 characters", nameof(FirstName));

            _firstName = value.Trim();
        }
    }

    public string LastName
    {
        get => _lastName;
        private set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Last name cannot be empty", nameof(LastName));

            if (value.Length > 50)
                throw new ArgumentException("Last name cannot exceed 50 characters", nameof(LastName));

            _lastName = value.Trim();
        }
    }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public string Email
    {
        get => _email;
        private set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email cannot be empty", nameof(Email));

            if (!IsValidEmail(value))
                throw new ArgumentException("Invalid email format", nameof(Email));

            _email = value.Trim().ToLowerInvariant();
        }
    }

    public string? PhoneNumber { get; private set; }
    public DateTime DateOfBirth { get; private set; }
    public string? Address { get; private set; }
    public DateTime MemberSince { get; private set; }
    public DateTime MembershipExpiresAt { get; private set; }
    public bool IsActive { get; private set; }

    public int MaxBooksAllowed
    {
        get => _maxBooksAllowed;
        private set
        {
            if (value <= 0)
                throw new ArgumentException("Max books allowed must be positive", nameof(MaxBooksAllowed));

            _maxBooksAllowed = value;
        }
    }

    public decimal OutstandingFees
    {
        get => _outstandingFees;
        private set
        {
            if (value < 0)
                throw new ArgumentException("Outstanding fees cannot be negative", nameof(OutstandingFees));

            _outstandingFees = value;
        }
    }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public List<Loan> Loans { get; private set; } = new();

    public bool IsMembershipValid => IsActive && MembershipExpiresAt > DateTime.UtcNow;

    public int Age
    {
        get
        {
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > today.AddYears(-age))
                age--;
            return age;
        }
    }

    public void UpdateContactInfo(string email, string? phoneNumber = null, string? address = null)
    {
        Email = email;
        PhoneNumber = phoneNumber;
        Address = address;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ExtendMembership(int months)
    {
        if (months <= 0)
            throw new ArgumentException("Months must be positive", nameof(months));

        MembershipExpiresAt = MembershipExpiresAt.AddMonths(months);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddFee(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Fee amount must be positive", nameof(amount));

        OutstandingFees += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void PayFee(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be positive", nameof(amount));

        if (amount > OutstandingFees)
            throw new ArgumentException("Payment amount exceeds outstanding fees", nameof(amount));

        OutstandingFees -= amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanBorrowBooks()
    {
        if (!IsMembershipValid)
            return false;

        if (OutstandingFees > 10m)
            return false;

        return true;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var parts = email.Split('@');
        return parts.Length == 2 &&
               !string.IsNullOrWhiteSpace(parts[0]) &&
               !string.IsNullOrWhiteSpace(parts[1]) &&
               parts[1].Contains('.');
    }

    public override string ToString() => $"{FullName} ({MembershipNumber})";
}
