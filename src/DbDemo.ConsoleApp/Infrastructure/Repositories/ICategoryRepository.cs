using DbDemo.ConsoleApp.Models;

namespace DbDemo.ConsoleApp.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Category entity operations
/// Provides abstraction over data access layer
/// Supports hierarchical category structure
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Creates a new category in the database
    /// </summary>
    /// <param name="category">The category to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created category with its assigned ID</returns>
    Task<Category> CreateAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a category by its ID
    /// </summary>
    /// <param name="id">The category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The category if found, null otherwise</returns>
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all categories
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all categories</returns>
    Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves top-level categories (those without a parent)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of top-level categories</returns>
    Task<List<Category>> GetTopLevelCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves child categories for a specific parent category
    /// </summary>
    /// <param name="parentId">The parent category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of child categories</returns>
    Task<List<Category>> GetChildCategoriesAsync(int parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of categories
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count of categories</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing category
    /// </summary>
    /// <param name="category">The category with updated data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if category not found</returns>
    Task<bool> UpdateAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a category (only if it has no children and no books)
    /// </summary>
    /// <param name="id">The category ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if category not found or has dependencies</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
