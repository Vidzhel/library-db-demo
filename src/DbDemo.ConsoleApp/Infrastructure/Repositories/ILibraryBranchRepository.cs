using DbDemo.ConsoleApp.Models;
using Microsoft.Data.SqlClient;

namespace DbDemo.ConsoleApp.Infrastructure.Repositories;

/// <summary>
/// Repository for managing library branches with spatial data support
/// </summary>
public interface ILibraryBranchRepository
{
    Task<LibraryBranch> CreateAsync(LibraryBranch branch, SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<LibraryBranch?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<List<LibraryBranch>> GetAllAsync(SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<List<(LibraryBranch Branch, double DistanceKm)>> FindWithinDistanceAsync(double latitude, double longitude, double radiusKm, SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<List<(LibraryBranch Branch, double DistanceKm)>> FindNearestAsync(double latitude, double longitude, int topN, SqlTransaction transaction, CancellationToken cancellationToken = default);
}
