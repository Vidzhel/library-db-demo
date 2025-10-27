using DbDemo.ConsoleApp.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DbDemo.ConsoleApp.Infrastructure.Repositories;

/// <summary>
/// ADO.NET implementation of IBookRepository
/// Demonstrates proper parameterized queries, resource disposal, and async patterns
/// </summary>
public class BookRepository : IBookRepository
{
    private readonly string _connectionString;

    public BookRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<Book> CreateAsync(Book book, CancellationToken cancellationToken = default)
    {
        // SQL with explicit column names and OUTPUT clause to get generated ID
        const string sql = @"
            INSERT INTO Books (
                ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
                ShelfLocation, IsDeleted, CreatedAt, UpdatedAt
            )
            OUTPUT INSERTED.Id
            VALUES (
                @ISBN, @Title, @Subtitle, @Description, @Publisher, @PublishedDate,
                @PageCount, @Language, @CategoryId, @TotalCopies, @AvailableCopies,
                @ShelfLocation, @IsDeleted, @CreatedAt, @UpdatedAt
            );";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);

        // Add parameters - this prevents SQL injection
        AddBookParameters(command, book);

        // ExecuteScalar returns the OUTPUT value (new Id)
        var newId = (int)await command.ExecuteScalarAsync(cancellationToken);

        // Return the book by querying it back with the new ID
        return await GetByIdAsync(newId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve newly created book");
    }

    public async Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // Explicit column selection - better performance and clarity than SELECT *
        const string sql = @"
            SELECT
                Id, ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
                ShelfLocation, IsDeleted, CreatedAt, UpdatedAt
            FROM Books
            WHERE Id = @Id;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return MapReaderToBook(reader);
        }

        return null;
    }

    public async Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
                ShelfLocation, IsDeleted, CreatedAt, UpdatedAt
            FROM Books
            WHERE ISBN = @ISBN;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@ISBN", SqlDbType.NVarChar, 20).Value = isbn;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return MapReaderToBook(reader);
        }

        return null;
    }

    public async Task<List<Book>> GetPagedAsync(
        int pageNumber = 1,
        int pageSize = 10,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) throw new ArgumentException("Page number must be >= 1", nameof(pageNumber));
        if (pageSize < 1) throw new ArgumentException("Page size must be >= 1", nameof(pageSize));

        // OFFSET-FETCH for pagination (SQL Server 2012+)
        string sql = @"
            SELECT
                Id, ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
                ShelfLocation, IsDeleted, CreatedAt, UpdatedAt
            FROM Books"
            + (includeDeleted ? "" : " WHERE IsDeleted = 0")
            + @"
            ORDER BY Title
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Offset", SqlDbType.Int).Value = (pageNumber - 1) * pageSize;
        command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var books = new List<Book>();
        while (await reader.ReadAsync(cancellationToken))
        {
            books.Add(MapReaderToBook(reader));
        }

        return books;
    }

    public async Task<List<Book>> SearchByTitleAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException("Search term cannot be empty", nameof(searchTerm));

        // LIKE with parameterized wildcards - safe from SQL injection
        const string sql = @"
            SELECT
                Id, ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
                ShelfLocation, IsDeleted, CreatedAt, UpdatedAt
            FROM Books
            WHERE IsDeleted = 0 AND Title LIKE @SearchPattern
            ORDER BY Title;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        // Add wildcards in code, not in SQL - still parameterized and safe
        command.Parameters.Add("@SearchPattern", SqlDbType.NVarChar, 202).Value = $"%{searchTerm}%";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var books = new List<Book>();
        while (await reader.ReadAsync(cancellationToken))
        {
            books.Add(MapReaderToBook(reader));
        }

        return books;
    }

    public async Task<List<Book>> GetByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
                ShelfLocation, IsDeleted, CreatedAt, UpdatedAt
            FROM Books
            WHERE CategoryId = @CategoryId AND IsDeleted = 0
            ORDER BY Title;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@CategoryId", SqlDbType.Int).Value = categoryId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var books = new List<Book>();
        while (await reader.ReadAsync(cancellationToken))
        {
            books.Add(MapReaderToBook(reader));
        }

        return books;
    }

    public async Task<int> GetCountAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        string sql = "SELECT COUNT(*) FROM Books"
            + (includeDeleted ? ";" : " WHERE IsDeleted = 0;");

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);

        var count = (int)await command.ExecuteScalarAsync(cancellationToken);
        return count;
    }

    public async Task<bool> UpdateAsync(Book book, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE Books
            SET
                ISBN = @ISBN,
                Title = @Title,
                Subtitle = @Subtitle,
                Description = @Description,
                Publisher = @Publisher,
                PublishedDate = @PublishedDate,
                PageCount = @PageCount,
                Language = @Language,
                CategoryId = @CategoryId,
                TotalCopies = @TotalCopies,
                AvailableCopies = @AvailableCopies,
                ShelfLocation = @ShelfLocation,
                IsDeleted = @IsDeleted,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        AddBookParameters(command, book);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = book.Id;

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        // Soft delete - set IsDeleted flag instead of actually deleting
        const string sql = @"
            UPDATE Books
            SET IsDeleted = 1, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        command.Parameters.Add("@UpdatedAt", SqlDbType.DateTime2).Value = DateTime.UtcNow;

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    /// <summary>
    /// Helper method to add all book parameters to a command
    /// Centralizes parameter creation to avoid duplication
    /// </summary>
    private static void AddBookParameters(SqlCommand command, Book book)
    {
        command.Parameters.Add("@ISBN", SqlDbType.NVarChar, 20).Value = book.ISBN;
        command.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = book.Title;
        command.Parameters.Add("@Subtitle", SqlDbType.NVarChar, 200).Value = (object?)book.Subtitle ?? DBNull.Value;
        command.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = (object?)book.Description ?? DBNull.Value;
        command.Parameters.Add("@Publisher", SqlDbType.NVarChar, 200).Value = (object?)book.Publisher ?? DBNull.Value;
        command.Parameters.Add("@PublishedDate", SqlDbType.DateTime2).Value = (object?)book.PublishedDate ?? DBNull.Value;
        command.Parameters.Add("@PageCount", SqlDbType.Int).Value = (object?)book.PageCount ?? DBNull.Value;
        command.Parameters.Add("@Language", SqlDbType.NVarChar, 50).Value = (object?)book.Language ?? DBNull.Value;
        command.Parameters.Add("@CategoryId", SqlDbType.Int).Value = book.CategoryId;
        command.Parameters.Add("@TotalCopies", SqlDbType.Int).Value = book.TotalCopies;
        command.Parameters.Add("@AvailableCopies", SqlDbType.Int).Value = book.AvailableCopies;
        command.Parameters.Add("@ShelfLocation", SqlDbType.NVarChar, 50).Value = (object?)book.ShelfLocation ?? DBNull.Value;
        command.Parameters.Add("@IsDeleted", SqlDbType.Bit).Value = book.IsDeleted;
        command.Parameters.Add("@CreatedAt", SqlDbType.DateTime2).Value = book.CreatedAt;
        command.Parameters.Add("@UpdatedAt", SqlDbType.DateTime2).Value = book.UpdatedAt;
    }

    /// <summary>
    /// Maps a SqlDataReader row to a Book entity
    /// Uses internal factory method to bypass validation for database-sourced data
    /// </summary>
    private static Book MapReaderToBook(SqlDataReader reader)
    {
        // Read all values from the reader first
        var id = reader.GetInt32(0);
        var isbn = reader.GetString(1);
        var title = reader.GetString(2);
        var subtitle = reader.IsDBNull(3) ? null : reader.GetString(3);
        var description = reader.IsDBNull(4) ? null : reader.GetString(4);
        var publisher = reader.IsDBNull(5) ? null : reader.GetString(5);
        var publishedDate = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6);
        var pageCount = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7);
        var language = reader.IsDBNull(8) ? null : reader.GetString(8);
        var categoryId = reader.GetInt32(9);
        var totalCopies = reader.GetInt32(10);
        var availableCopies = reader.GetInt32(11);
        var shelfLocation = reader.IsDBNull(12) ? null : reader.GetString(12);
        var isDeleted = reader.GetBoolean(13);
        var createdAt = reader.GetDateTime(14);
        var updatedAt = reader.GetDateTime(15);

        return Book.FromDatabase(
            id,
            isbn,
            title,
            subtitle,
            description,
            publisher,
            publishedDate,
            pageCount,
            language,
            categoryId,
            totalCopies,
            availableCopies,
            shelfLocation,
            isDeleted,
            createdAt,
            updatedAt
        );
    }
}
