using DbDemo.ConsoleApp.Models;
using Microsoft.Data.SqlClient;

namespace DbDemo.ConsoleApp.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Author entity operations
/// Provides abstraction over data access layer
/// All methods require an active SqlTransaction to ensure proper transaction management
/// </summary>
public interface IAuthorRepository
{
    Task<Author> CreateAsync(Author author, SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<Author?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<Author?> GetByEmailAsync(string email, SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<List<Author>> GetPagedAsync(int pageNumber, int pageSize, SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<List<Author>> SearchByNameAsync(string searchTerm, SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Author author, SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default);
}
