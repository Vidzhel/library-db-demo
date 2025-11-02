using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DbDemo.Infrastructure.Repositories;

using DbDemo.Domain.Entities;
using DbDemo.Application.DTOs;
using DbDemo.Application.Repositories;

/// <summary>
/// ADO.NET implementation of IBookRepository
/// Demonstrates proper parameterized queries, resource disposal, and async patterns
/// </summary>
public class BookRepository : IBookRepository
{
    public BookRepository()
    {
    }

    public async Task<Book> CreateAsync(Book book, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        // SQL with explicit column names
        // Note: Using SCOPE_IDENTITY() instead of OUTPUT INSERTED.Id because the table has triggers
        // SQL Server doesn't allow OUTPUT clause without INTO when triggers are present
        const string sql = @"
            INSERT INTO Books (
                ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
                ShelfLocation, IsDeleted, CreatedAt, UpdatedAt, Metadata
            )
            VALUES (
                @ISBN, @Title, @Subtitle, @Description, @Publisher, @PublishedDate,
                @PageCount, @Language, @CategoryId, @TotalCopies, @AvailableCopies,
                @ShelfLocation, @IsDeleted, @CreatedAt, @UpdatedAt, @Metadata
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);

        // Add parameters - this prevents SQL injection
        AddBookParameters(command, book);

        // ExecuteScalar returns the new Id from SCOPE_IDENTITY()
        var newId = (int)await command.ExecuteScalarAsync(cancellationToken);

        // Fetch the created book using the same transaction
        return await GetByIdAsync(newId, transaction, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve newly created book");
    }

    public async Task<Book?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        // Explicit column selection - better performance and clarity than SELECT *
        const string sql = @"
            SELECT
                Id, ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
                ShelfLocation, IsDeleted, CreatedAt, UpdatedAt, Metadata
            FROM Books
            WHERE Id = @Id;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return MapReaderToBook(reader);
        }

        return null;
    }

    public async Task<Book?> GetByIsbnAsync(string isbn, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
                ShelfLocation, IsDeleted, CreatedAt, UpdatedAt, Metadata
            FROM Books
            WHERE ISBN = @ISBN;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@ISBN", SqlDbType.NVarChar, 20).Value = isbn;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return MapReaderToBook(reader);
        }

        return null;
    }

    public async Task<List<Book>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        bool includeDeleted,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) throw new ArgumentException("Page number must be >= 1", nameof(pageNumber));
        if (pageSize < 1) throw new ArgumentException("Page size must be >= 1", nameof(pageSize));

        // OFFSET-FETCH for pagination (SQL Server 2012+)
        string sql = @"
            SELECT
                Id, ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
                ShelfLocation, IsDeleted, CreatedAt, UpdatedAt, Metadata
            FROM Books"
            + (includeDeleted ? "" : " WHERE IsDeleted = 0")
            + @"
            ORDER BY Title
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
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

    public async Task<List<Book>> SearchByTitleAsync(string searchTerm, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException("Search term cannot be empty", nameof(searchTerm));

        // LIKE with parameterized wildcards - safe from SQL injection
        const string sql = @"
            SELECT
                Id, ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
                ShelfLocation, IsDeleted, CreatedAt, UpdatedAt, Metadata
            FROM Books
            WHERE IsDeleted = 0 AND Title LIKE @SearchPattern
            ORDER BY Title;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
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

    public async Task<List<Book>> GetByCategoryAsync(int categoryId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                Id, ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
                ShelfLocation, IsDeleted, CreatedAt, UpdatedAt, Metadata
            FROM Books
            WHERE CategoryId = @CategoryId AND IsDeleted = 0
            ORDER BY Title;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@CategoryId", SqlDbType.Int).Value = categoryId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var books = new List<Book>();
        while (await reader.ReadAsync(cancellationToken))
        {
            books.Add(MapReaderToBook(reader));
        }

        return books;
    }

    public async Task<int> GetCountAsync(bool includeDeleted, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        string sql = "SELECT COUNT(*) FROM Books"
            + (includeDeleted ? ";" : " WHERE IsDeleted = 0;");

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);

        var count = (int)await command.ExecuteScalarAsync(cancellationToken);
        return count;
    }

    public async Task<bool> UpdateAsync(Book book, SqlTransaction transaction, CancellationToken cancellationToken = default)
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
                UpdatedAt = @UpdatedAt,
                Metadata = @Metadata
            WHERE Id = @Id;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);

        AddBookParameters(command, book);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = book.Id;

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> BorrowCopyAsync(int bookId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        // Atomic UPDATE with WHERE conditions to prevent TOCTOU race conditions
        // This checks availability and decrements in a single database operation
        const string sql = @"
            UPDATE Books
            SET AvailableCopies = AvailableCopies - 1,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id
              AND AvailableCopies > 0
              AND IsDeleted = 0;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = bookId;
        command.Parameters.Add("@UpdatedAt", SqlDbType.DateTime2).Value = DateTime.UtcNow;

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;  // Returns false if book unavailable or deleted
    }

    public async Task<bool> ReturnCopyAsync(int bookId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        // Atomic UPDATE to increment available copies
        const string sql = @"
            UPDATE Books
            SET AvailableCopies = AvailableCopies + 1,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = bookId;
        command.Parameters.Add("@UpdatedAt", SqlDbType.DateTime2).Value = DateTime.UtcNow;

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;  // Returns false if book not found
    }

    public async Task<bool> DeleteAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        // Soft delete - set IsDeleted flag instead of actually deleting
        const string sql = @"
            UPDATE Books
            SET IsDeleted = 1, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;";

        var connection = transaction.Connection ;

        await using var command = new SqlCommand(sql, connection, transaction);
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
        command.Parameters.Add("@Metadata", SqlDbType.NVarChar, -1).Value = (object?)book.MetadataJson ?? DBNull.Value;
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
        var metadataJson = reader.IsDBNull(16) ? null : reader.GetString(16);

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
            updatedAt,
            metadataJson
        );
    }

    public async Task<List<Book>> SearchByMetadataValueAsync(
        string jsonPath,
        string searchValue,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jsonPath))
            throw new ArgumentException("JSON path cannot be empty", nameof(jsonPath));

        if (string.IsNullOrWhiteSpace(searchValue))
            throw new ArgumentException("Search value cannot be empty", nameof(searchValue));

        // Use JSON_VALUE to extract and filter by JSON property
        const string sql = @"
            SELECT
                Id, ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
                ShelfLocation, IsDeleted, CreatedAt, UpdatedAt, Metadata
            FROM Books
            WHERE JSON_VALUE(Metadata, @JsonPath) = @SearchValue
                AND IsDeleted = 0
            ORDER BY Title;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@JsonPath", SqlDbType.NVarChar, 100).Value = jsonPath;
        command.Parameters.Add("@SearchValue", SqlDbType.NVarChar, 200).Value = searchValue;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var books = new List<Book>();
        while (await reader.ReadAsync(cancellationToken))
        {
            books.Add(MapReaderToBook(reader));
        }

        return books;
    }

    public async Task<List<Book>> GetBooksByTagAsync(
        string tag,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));

        // Use OPENJSON to expand tags array and filter
        const string sql = @"
            SELECT DISTINCT
                b.Id, b.ISBN, b.Title, b.Subtitle, b.Description, b.Publisher, b.PublishedDate,
                b.PageCount, b.Language, b.CategoryId, b.TotalCopies, b.AvailableCopies,
                b.ShelfLocation, b.IsDeleted, b.CreatedAt, b.UpdatedAt, b.Metadata
            FROM Books b
            CROSS APPLY OPENJSON(b.Metadata, '$.tags') AS tags
            WHERE tags.[value] = @Tag
                AND b.IsDeleted = 0
            ORDER BY b.Title;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Tag", SqlDbType.NVarChar, 50).Value = tag;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var books = new List<Book>();
        while (await reader.ReadAsync(cancellationToken))
        {
            books.Add(MapReaderToBook(reader));
        }

        return books;
    }

    public async Task<List<Book>> GetBooksWithMetadataAsync(
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        // Get all books that have metadata populated
        const string sql = @"
            SELECT
                Id, ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
                ShelfLocation, IsDeleted, CreatedAt, UpdatedAt, Metadata
            FROM Books
            WHERE Metadata IS NOT NULL
                AND IsDeleted = 0
            ORDER BY Title;";

        var connection = transaction.Connection;

        await using var command = new SqlCommand(sql, connection, transaction);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var books = new List<Book>();
        while (await reader.ReadAsync(cancellationToken))
        {
            books.Add(MapReaderToBook(reader));
        }

        return books;
    }
}
