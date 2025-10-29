using DbDemo.ConsoleApp.Models;
using Microsoft.Data.SqlClient;

namespace DbDemo.ConsoleApp.Infrastructure.Repositories;

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
}
