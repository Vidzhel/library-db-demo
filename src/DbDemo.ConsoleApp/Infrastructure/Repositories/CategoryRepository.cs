using DbDemo.ConsoleApp.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DbDemo.ConsoleApp.Infrastructure.Repositories;

/// <summary>
/// ADO.NET implementation of ICategoryRepository
/// Demonstrates proper parameterized queries, resource disposal, and async patterns
/// Handles hierarchical category structure
/// All operations require an active transaction for consistency and atomicity
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    public CategoryRepository()
    {
    }

    public async Task<Category> CreateAsync(Category category, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO Categories (
                Name, Description, ParentCategoryId, CreatedAt, UpdatedAt
            )
            OUTPUT INSERTED.Id
            VALUES (
                @Name, @Description, @ParentCategoryId, @CreatedAt, @UpdatedAt
            );";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        AddCategoryParameters(command, category);

        var newId = (int)await command.ExecuteScalarAsync(cancellationToken);

        return await GetByIdAsync(newId, transaction, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve newly created category");
    }

    public async Task<Category?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, Name, Description, ParentCategoryId, CreatedAt, UpdatedAt
            FROM Categories
            WHERE Id = @Id;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return MapReaderToCategory(reader);
        }

        return null;
    }

    public async Task<List<Category>> GetAllAsync(SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, Name, Description, ParentCategoryId, CreatedAt, UpdatedAt
            FROM Categories
            ORDER BY Name;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var categories = new List<Category>();
        while (await reader.ReadAsync(cancellationToken))
        {
            categories.Add(MapReaderToCategory(reader));
        }

        return categories;
    }

    public async Task<List<Category>> GetTopLevelCategoriesAsync(SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, Name, Description, ParentCategoryId, CreatedAt, UpdatedAt
            FROM Categories
            WHERE ParentCategoryId IS NULL
            ORDER BY Name;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var categories = new List<Category>();
        while (await reader.ReadAsync(cancellationToken))
        {
            categories.Add(MapReaderToCategory(reader));
        }

        return categories;
    }

    public async Task<List<Category>> GetChildCategoriesAsync(int parentId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, Name, Description, ParentCategoryId, CreatedAt, UpdatedAt
            FROM Categories
            WHERE ParentCategoryId = @ParentId
            ORDER BY Name;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@ParentId", SqlDbType.Int).Value = parentId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var categories = new List<Category>();
        while (await reader.ReadAsync(cancellationToken))
        {
            categories.Add(MapReaderToCategory(reader));
        }

        return categories;
    }

    public async Task<int> GetCountAsync(SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM Categories;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);

        var count = (int)await command.ExecuteScalarAsync(cancellationToken);
        return count;
    }

    public async Task<bool> UpdateAsync(Category category, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE Categories
            SET
                Name = @Name,
                Description = @Description,
                ParentCategoryId = @ParentCategoryId,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        AddCategoryParameters(command, category);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = category.Id;

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        // Hard delete - will fail if category has children or books due to FK constraints
        const string sql = "DELETE FROM Categories WHERE Id = @Id;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        try
        {
            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            return rowsAffected > 0;
        }
        catch (SqlException)
        {
            // FK constraint violation - category has dependencies
            return false;
        }
    }

    /// <summary>
    /// Helper method to add all category parameters to a command
    /// Centralizes parameter creation to avoid duplication
    /// </summary>
    private static void AddCategoryParameters(SqlCommand command, Category category)
    {
        command.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = category.Name;
        command.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = (object?)category.Description ?? DBNull.Value;
        command.Parameters.Add("@ParentCategoryId", SqlDbType.Int).Value = (object?)category.ParentCategoryId ?? DBNull.Value;
        command.Parameters.Add("@CreatedAt", SqlDbType.DateTime2).Value = category.CreatedAt;
        command.Parameters.Add("@UpdatedAt", SqlDbType.DateTime2).Value = category.UpdatedAt;
    }

    public async Task<List<CategoryHierarchy>> GetHierarchyAsync(
        int? rootCategoryId,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        // Query the recursive CTE function fn_GetCategoryHierarchy
        const string sql = @"
            SELECT
                CategoryId,
                Name,
                ParentCategoryId,
                Level,
                HierarchyPath,
                FullPath
            FROM dbo.fn_GetCategoryHierarchy(@RootCategoryId)
            ORDER BY FullPath;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@RootCategoryId", SqlDbType.Int).Value = (object?)rootCategoryId ?? DBNull.Value;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var hierarchy = new List<CategoryHierarchy>();
        while (await reader.ReadAsync(cancellationToken))
        {
            hierarchy.Add(MapReaderToCategoryHierarchy(reader));
        }

        return hierarchy;
    }

    /// <summary>
    /// Maps a SqlDataReader row to a Category entity
    /// Uses internal factory method to bypass validation for database-sourced data
    /// </summary>
    private static Category MapReaderToCategory(SqlDataReader reader)
    {
        var id = reader.GetInt32(0);
        var name = reader.GetString(1);
        var description = reader.IsDBNull(2) ? null : reader.GetString(2);
        var parentCategoryId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
        var createdAt = reader.GetDateTime(4);
        var updatedAt = reader.GetDateTime(5);

        return Category.FromDatabase(
            id,
            name,
            description,
            parentCategoryId,
            createdAt,
            updatedAt
        );
    }

    /// <summary>
    /// Maps a SqlDataReader row to a CategoryHierarchy DTO
    /// Reads results from fn_GetCategoryHierarchy recursive CTE function
    /// </summary>
    private static CategoryHierarchy MapReaderToCategoryHierarchy(SqlDataReader reader)
    {
        var categoryId = reader.GetInt32(0);
        var name = reader.GetString(1);
        var parentCategoryId = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2);
        var level = reader.GetInt32(3);
        var hierarchyPath = reader.GetString(4);
        var fullPath = reader.GetString(5);

        return CategoryHierarchy.FromDatabase(
            categoryId,
            name,
            parentCategoryId,
            level,
            hierarchyPath,
            fullPath
        );
    }
}
