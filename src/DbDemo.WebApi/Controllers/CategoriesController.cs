using Microsoft.AspNetCore.Mvc;
using DbDemo.Application.Repositories;
using DbDemo.Domain.Entities;
using DbDemo.WebApi.DTOs;
using DbDemo.WebApi.Services;

namespace DbDemo.WebApi.Controllers;

/// <summary>
/// API controller for managing book categories
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITransactionContext _transactionContext;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        ICategoryRepository categoryRepository,
        ITransactionContext transactionContext,
        ILogger<CategoriesController> logger)
    {
        _categoryRepository = categoryRepository;
        _transactionContext = transactionContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all categories</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CategoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories(CancellationToken cancellationToken = default)
    {
        var transaction = _transactionContext.Transaction;

        var categories = await _categoryRepository.GetAllAsync(transaction, cancellationToken);
        var categoryDtos = categories.Select(MapToDto).ToList();

        return Ok(ApiResponse<List<CategoryDto>>.SuccessResponse(categoryDtos, $"Retrieved {categoryDtos.Count} categories"));
    }

    /// <summary>
    /// Get a specific category by ID
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategory(int id, CancellationToken cancellationToken = default)
    {
        var transaction = _transactionContext.Transaction;

        var category = await _categoryRepository.GetByIdAsync(id, transaction, cancellationToken);
        if (category == null)
            return NotFound(ApiResponse<CategoryDto>.ErrorResponse($"Category with ID {id} not found"));

        var categoryDto = MapToDto(category);

        return Ok(ApiResponse<CategoryDto>.SuccessResponse(categoryDto));
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    /// <param name="request">Category creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created category</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<CategoryDto>.ErrorResponse("Invalid category data", GetModelStateErrors()));

        var transaction = _transactionContext.Transaction;

        var category = new Category(request.Name, request.Description);
        var createdCategory = await _categoryRepository.CreateAsync(category, transaction, cancellationToken);
        var categoryDto = MapToDto(createdCategory);

        return CreatedAtAction(nameof(GetCategory), new { id = createdCategory.Id }, ApiResponse<CategoryDto>.SuccessResponse(categoryDto, "Category created successfully"));
    }

    /// <summary>
    /// Update an existing category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated category</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(
        int id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<CategoryDto>.ErrorResponse("Invalid category data", GetModelStateErrors()));

        var transaction = _transactionContext.Transaction;

        var category = await _categoryRepository.GetByIdAsync(id, transaction, cancellationToken);
        if (category == null)
            return NotFound(ApiResponse<CategoryDto>.ErrorResponse($"Category with ID {id} not found"));

        // Update category properties if provided
        if (!string.IsNullOrWhiteSpace(request.Name) || !string.IsNullOrWhiteSpace(request.Description))
        {
            category.UpdateDetails(
                name: request.Name ?? category.Name,
                description: request.Description ?? category.Description
            );
        }

        var success = await _categoryRepository.UpdateAsync(category, transaction, cancellationToken);
        if (!success)
            return NotFound(ApiResponse<CategoryDto>.ErrorResponse($"Failed to update category with ID {id}"));

        var categoryDto = MapToDto(category);

        return Ok(ApiResponse<CategoryDto>.SuccessResponse(categoryDto, "Category updated successfully"));
    }

    /// <summary>
    /// Delete a category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCategory(int id, CancellationToken cancellationToken = default)
    {
        var transaction = _transactionContext.Transaction;

        var success = await _categoryRepository.DeleteAsync(id, transaction, cancellationToken);
        if (!success)
            return NotFound(ApiResponse<object>.ErrorResponse($"Category with ID {id} not found"));

        return Ok(ApiResponse<object>.SuccessResponse(default, "Category deleted successfully"));
    }

    // Helper method for mapping entity to DTO
    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    private List<string> GetModelStateErrors()
    {
        return ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
