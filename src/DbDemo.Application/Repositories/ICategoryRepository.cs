using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient;

namespace DbDemo.Application.Repositories;

using DbDemo.Domain.Entities;
using DbDemo.Application.DTOs;

/// <summary>
/// Repository interface for Category entity operations
/// Provides abstraction over data access layer
/// All methods require an active SqlTransaction to ensure proper transaction management
/// </summary>
public interface ICategoryRepository
{
    Task<Category> CreateAsync(Category category, SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<Category?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<List<Category>> GetAllAsync(SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<List<Category>> GetTopLevelCategoriesAsync(SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<List<Category>> GetChildCategoriesAsync(int parentId, SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Category category, SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves complete category hierarchy using recursive CTE (fn_GetCategoryHierarchy)
    /// </summary>
    /// <param name="rootCategoryId">Optional: Start from specific category (NULL = entire tree)</param>
    /// <param name="transaction">Transaction to participate in (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of categories with hierarchy information (Level, Path, etc.)</returns>
    Task<List<CategoryHierarchy>> GetHierarchyAsync(int? rootCategoryId, SqlTransaction transaction, CancellationToken cancellationToken = default);
}
