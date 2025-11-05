using System.ComponentModel.DataAnnotations;

namespace DbDemo.WebApi.DTOs;

/// <summary>
/// Request model for creating a new category
/// </summary>
public class CreateCategoryRequest
{
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(50, ErrorMessage = "Category name cannot exceed 50 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
}
