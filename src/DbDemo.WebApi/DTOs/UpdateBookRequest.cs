using System.ComponentModel.DataAnnotations;

namespace DbDemo.WebApi.DTOs;

/// <summary>
/// Request model for updating an existing book
/// </summary>
public class UpdateBookRequest
{
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string? Title { get; set; }

    [StringLength(200, ErrorMessage = "Subtitle cannot exceed 200 characters")]
    public string? Subtitle { get; set; }

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    [StringLength(100, ErrorMessage = "Publisher cannot exceed 100 characters")]
    public string? Publisher { get; set; }

    public DateTime? PublishedDate { get; set; }

    [Range(1, 10000, ErrorMessage = "Page count must be between 1 and 10000")]
    public int? PageCount { get; set; }

    [StringLength(50, ErrorMessage = "Language cannot exceed 50 characters")]
    public string? Language { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Category ID must be a positive number")]
    public int? CategoryId { get; set; }

    [Range(0, 1000, ErrorMessage = "Total copies must be between 0 and 1000")]
    public int? TotalCopies { get; set; }

    [StringLength(50, ErrorMessage = "Shelf location cannot exceed 50 characters")]
    public string? ShelfLocation { get; set; }
}
