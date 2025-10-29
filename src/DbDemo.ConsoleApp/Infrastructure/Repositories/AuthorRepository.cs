using DbDemo.ConsoleApp.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DbDemo.ConsoleApp.Infrastructure.Repositories;

/// <summary>
/// ADO.NET implementation of IAuthorRepository
/// Demonstrates proper parameterized queries, resource disposal, and async patterns
/// All operations require an active transaction for consistency and atomicity
/// </summary>
public class AuthorRepository : IAuthorRepository
{
    // Constructor kept for backward compatibility with demos that instantiate this class directly
    // Connection string is not used as all operations now require explicit transactions
    public AuthorRepository(string connectionString)
    {
        // Connection string parameter ignored - all operations use transactions
    }

    public async Task<Author> CreateAsync(Author author, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO Authors (
                FirstName, LastName, Biography, DateOfBirth, Nationality, Email,
                CreatedAt, UpdatedAt
            )
            OUTPUT INSERTED.Id
            VALUES (
                @FirstName, @LastName, @Biography, @DateOfBirth, @Nationality, @Email,
                @CreatedAt, @UpdatedAt
            );";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        AddAuthorParameters(command, author);

        var newId = (int)await command.ExecuteScalarAsync(cancellationToken);

        return await GetByIdAsync(newId, transaction, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve newly created author");
    }

    public async Task<Author?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, FirstName, LastName, Biography, DateOfBirth, Nationality, Email,
                CreatedAt, UpdatedAt
            FROM Authors
            WHERE Id = @Id;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return MapReaderToAuthor(reader);
        }

        return null;
    }

    public async Task<Author?> GetByEmailAsync(string email, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, FirstName, LastName, Biography, DateOfBirth, Nationality, Email,
                CreatedAt, UpdatedAt
            FROM Authors
            WHERE Email = @Email;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = email;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return MapReaderToAuthor(reader);
        }

        return null;
    }

    public async Task<List<Author>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) throw new ArgumentException("Page number must be >= 1", nameof(pageNumber));
        if (pageSize < 1) throw new ArgumentException("Page size must be >= 1", nameof(pageSize));

        const string sql = @"
            SELECT
                Id, FirstName, LastName, Biography, DateOfBirth, Nationality, Email,
                CreatedAt, UpdatedAt
            FROM Authors
            ORDER BY LastName, FirstName
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Offset", SqlDbType.Int).Value = (pageNumber - 1) * pageSize;
        command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var authors = new List<Author>();
        while (await reader.ReadAsync(cancellationToken))
        {
            authors.Add(MapReaderToAuthor(reader));
        }

        return authors;
    }

    public async Task<List<Author>> SearchByNameAsync(string searchTerm, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException("Search term cannot be empty", nameof(searchTerm));

        const string sql = @"
            SELECT
                Id, FirstName, LastName, Biography, DateOfBirth, Nationality, Email,
                CreatedAt, UpdatedAt
            FROM Authors
            WHERE FirstName LIKE @SearchPattern OR LastName LIKE @SearchPattern
            ORDER BY LastName, FirstName;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@SearchPattern", SqlDbType.NVarChar, 102).Value = $"%{searchTerm}%";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var authors = new List<Author>();
        while (await reader.ReadAsync(cancellationToken))
        {
            authors.Add(MapReaderToAuthor(reader));
        }

        return authors;
    }

    public async Task<int> GetCountAsync(SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM Authors;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);

        var count = (int)await command.ExecuteScalarAsync(cancellationToken);
        return count;
    }

    public async Task<bool> UpdateAsync(Author author, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE Authors
            SET
                FirstName = @FirstName,
                LastName = @LastName,
                Biography = @Biography,
                DateOfBirth = @DateOfBirth,
                Nationality = @Nationality,
                Email = @Email,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        AddAuthorParameters(command, author);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = author.Id;

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        // Hard delete for authors
        const string sql = "DELETE FROM Authors WHERE Id = @Id;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    /// <summary>
    /// Helper method to add all author parameters to a command
    /// Centralizes parameter creation to avoid duplication
    /// </summary>
    private static void AddAuthorParameters(SqlCommand command, Author author)
    {
        command.Parameters.Add("@FirstName", SqlDbType.NVarChar, 50).Value = author.FirstName;
        command.Parameters.Add("@LastName", SqlDbType.NVarChar, 50).Value = author.LastName;
        command.Parameters.Add("@Biography", SqlDbType.NVarChar, -1).Value = (object?)author.Biography ?? DBNull.Value;
        command.Parameters.Add("@DateOfBirth", SqlDbType.DateTime2).Value = (object?)author.DateOfBirth ?? DBNull.Value;
        command.Parameters.Add("@Nationality", SqlDbType.NVarChar, 100).Value = (object?)author.Nationality ?? DBNull.Value;
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = (object?)author.Email ?? DBNull.Value;
        command.Parameters.Add("@CreatedAt", SqlDbType.DateTime2).Value = author.CreatedAt;
        command.Parameters.Add("@UpdatedAt", SqlDbType.DateTime2).Value = author.UpdatedAt;
    }

    /// <summary>
    /// Maps a SqlDataReader row to an Author entity
    /// Uses internal factory method to bypass validation for database-sourced data
    /// </summary>
    private static Author MapReaderToAuthor(SqlDataReader reader)
    {
        var id = reader.GetInt32(0);
        var firstName = reader.GetString(1);
        var lastName = reader.GetString(2);
        var biography = reader.IsDBNull(3) ? null : reader.GetString(3);
        var dateOfBirth = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4);
        var nationality = reader.IsDBNull(5) ? null : reader.GetString(5);
        var email = reader.IsDBNull(6) ? null : reader.GetString(6);
        var createdAt = reader.GetDateTime(7);
        var updatedAt = reader.GetDateTime(8);

        return Author.FromDatabase(
            id,
            firstName,
            lastName,
            biography,
            dateOfBirth,
            nationality,
            email,
            createdAt,
            updatedAt
        );
    }
}
