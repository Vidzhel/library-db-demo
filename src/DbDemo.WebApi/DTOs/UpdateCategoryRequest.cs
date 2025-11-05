using System.ComponentModel.DataAnnotations;

namespace DbDemo.WebApi.DTOs;

/// <summary>
/// Request model for updating an existing category
/// </summary>
public class UpdateCategoryRequest
{
    [StringLength(50, ErrorMessage = "Category name cannot exceed 50 characters")]
    public string? Name { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
}
