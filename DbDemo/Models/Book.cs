namespace DbDemo.Models;

public class Book
{
    private string _isbn = string.Empty;
    private string _title = string.Empty;
    private int _availableCopies;
    private int _totalCopies;

    private Book() { }

    public Book(string isbn, string title, int categoryId, int totalCopies)
    {
        ISBN = isbn;
        Title = title;
        CategoryId = categoryId;
        TotalCopies = totalCopies;
        _availableCopies = totalCopies;
        IsDeleted = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }

    public string ISBN
    {
        get => _isbn;
        private set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("ISBN cannot be empty", nameof(ISBN));

            var cleanIsbn = value.Replace("-", "").Replace(" ", "");

            if (!IsValidIsbn(cleanIsbn))
                throw new ArgumentException("Invalid ISBN format. Must be 10 or 13 digits", nameof(ISBN));

            _isbn = value.Trim();
        }
    }

    public string Title
    {
        get => _title;
        private set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Title cannot be empty", nameof(Title));

            if (value.Length > 200)
                throw new ArgumentException("Title cannot exceed 200 characters", nameof(Title));

            _title = value.Trim();
        }
    }

    public string? Subtitle { get; private set; }
    public string? Description { get; private set; }
    public string? Publisher { get; private set; }
    public DateTime? PublishedDate { get; private set; }
    public int? PageCount { get; private set; }
    public string? Language { get; private set; }
    public int CategoryId { get; private set; }

    public int TotalCopies
    {
        get => _totalCopies;
        private set
        {
            if (value < 0)
                throw new ArgumentException("Total copies cannot be negative", nameof(TotalCopies));

            if (value < AvailableCopies)
                throw new ArgumentException("Total copies cannot be less than available copies", nameof(TotalCopies));

            _totalCopies = value;
        }
    }

    public int AvailableCopies
    {
        get => _availableCopies;
        private set
        {
            if (value < 0)
                throw new ArgumentException("Available copies cannot be negative", nameof(AvailableCopies));

            if (value > TotalCopies)
                throw new ArgumentException("Available copies cannot exceed total copies", nameof(AvailableCopies));

            _availableCopies = value;
        }
    }

    public string? ShelfLocation { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation properties
    public Category? Category { get; private set; }
    public List<Author> Authors { get; private set; } = new();
    public List<Loan> Loans { get; private set; } = new();

    public bool IsAvailable => AvailableCopies > 0 && !IsDeleted;
    public int CopiesOnLoan => TotalCopies - AvailableCopies;

    public void UpdateDetails(string title, string? subtitle = null, string? description = null, string? publisher = null)
    {
        Title = title;
        Subtitle = subtitle;
        Description = description;
        Publisher = publisher;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePublishingInfo(DateTime? publishedDate, int? pageCount, string? language)
    {
        PublishedDate = publishedDate;
        PageCount = pageCount;
        Language = language;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateShelfLocation(string location)
    {
        ShelfLocation = location;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddCopies(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be positive", nameof(count));

        TotalCopies += count;
        AvailableCopies += count;
        UpdatedAt = DateTime.UtcNow;
    }

    public void BorrowCopy()
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Book is not available for borrowing");

        AvailableCopies--;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReturnCopy()
    {
        if (AvailableCopies >= TotalCopies)
            throw new InvalidOperationException("Cannot return more copies than total");

        AvailableCopies++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsDeleted()
    {
        if (CopiesOnLoan > 0)
            throw new InvalidOperationException("Cannot delete book with copies on loan");

        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private static bool IsValidIsbn(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            return false;

        return isbn.Length == 10 && isbn.All(char.IsDigit) ||
               isbn.Length == 13 && isbn.All(char.IsDigit);
    }

    public override string ToString() => $"{Title} (ISBN: {ISBN})";
}
