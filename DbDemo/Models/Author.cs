namespace DbDemo.Models;

public class Author
{
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string? _email;

    private Author() { }

    public Author(string firstName, string lastName, string? email = null)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }

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

    public string? Biography { get; private set; }
    public DateTime? DateOfBirth { get; private set; }
    public string? Nationality { get; private set; }

    public string? Email
    {
        get => _email;
        private set
        {
            if (value != null && !IsValidEmail(value))
                throw new ArgumentException("Invalid email format", nameof(Email));

            _email = value;
        }
    }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public List<Book> Books { get; private set; } = new();

    public int Age
    {
        get
        {
            if (!DateOfBirth.HasValue)
                return 0;

            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Value.Year;
            if (DateOfBirth.Value.Date > today.AddYears(-age))
                age--;
            return age;
        }
    }

    public void UpdateDetails(string firstName, string lastName, string? email = null)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateBiography(string? biography, DateTime? dateOfBirth = null, string? nationality = null)
    {
        Biography = biography;

        if (dateOfBirth.HasValue)
            DateOfBirth = dateOfBirth;

        if (nationality != null)
            Nationality = nationality;

        UpdatedAt = DateTime.UtcNow;
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

    public override string ToString() => FullName;
}
