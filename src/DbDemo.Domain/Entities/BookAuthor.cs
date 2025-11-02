namespace DbDemo.Domain.Entities;

/// <summary>
/// Represents the relationship between Books and Authors.
/// This handles the many-to-many association and includes ordering/role information.
/// </summary>
public class BookAuthor
{
    private BookAuthor() { }

    public BookAuthor(int bookId, int authorId, int authorOrder, string? role = null)
    {
        BookId = bookId;
        AuthorId = authorId;
        AuthorOrder = authorOrder;
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }

    public int BookId { get; private set; }
    public int AuthorId { get; private set; }
    public int AuthorOrder { get; private set; }
    public string? Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public Book? Book { get; private set; }
    public Author? Author { get; private set; }

    public void UpdateOrder(int newOrder)
    {
        if (newOrder < 0)
            throw new ArgumentException("Order cannot be negative", nameof(newOrder));

        AuthorOrder = newOrder;
    }

    public void UpdateRole(string? role)
    {
        Role = role;
    }

    public override string ToString()
    {
        var roleText = string.IsNullOrEmpty(Role) ? "Author" : Role;
        return $"Book {BookId} - Author {AuthorId} ({roleText}, Order: {AuthorOrder})";
    }

    /// <summary>
    /// Factory method for repository hydration
    /// </summary>
    public static BookAuthor FromDatabase(int bookId, int authorId, int authorOrder, string? role, DateTime createdAt)
    {
        var bookAuthor = new BookAuthor();
        bookAuthor.BookId = bookId;
        bookAuthor.AuthorId = authorId;
        bookAuthor.AuthorOrder = authorOrder;
        bookAuthor.Role = role;
        bookAuthor.CreatedAt = createdAt;
        return bookAuthor;
    }
}
