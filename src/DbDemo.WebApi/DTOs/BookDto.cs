namespace DbDemo.WebApi.DTOs;

/// <summary>
/// Data Transfer Object for Book entity returned from the API
/// </summary>
public class BookDto
{
    public int Id { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? Publisher { get; set; }
    public DateTime? PublishedDate { get; set; }
    public int? PageCount { get; set; }
    public string? Language { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public string? ShelfLocation { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
