using DbDemo.Application.Repositories;
using DbDemo.Domain.Entities;
using DbDemo.Infrastructure.SqlKata.Generated;
using Microsoft.Data.SqlClient;
using SqlKata;
using SqlKata.Execution;
using System.Data;

namespace DbDemo.Infrastructure.SqlKata.Repositories;

/// <summary>
/// SqlKata implementation of IBookRepository
/// Demonstrates query builder usage with compile-time checked table/column names
/// </summary>
public class BookRepository : IBookRepository
{
    public BookRepository()
    {
    }

    public async Task<Book> CreateAsync(Book book, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        var factory = QueryFactoryProvider.Create(transaction);

        // Use generated constants for compile-time safety
        var insertData = new Dictionary<string, object?>
        {
            [Columns.Books.ISBN] = book.ISBN,
            [Columns.Books.Title] = book.Title,
            [Columns.Books.Subtitle] = book.Subtitle,
            [Columns.Books.Description] = book.Description,
            [Columns.Books.Publisher] = book.Publisher,
            [Columns.Books.PublishedDate] = book.PublishedDate,
            [Columns.Books.PageCount] = book.PageCount,
            [Columns.Books.Language] = book.Language,
            [Columns.Books.CategoryId] = book.CategoryId,
            [Columns.Books.TotalCopies] = book.TotalCopies,
            [Columns.Books.AvailableCopies] = book.AvailableCopies,
            [Columns.Books.ShelfLocation] = book.ShelfLocation,
            [Columns.Books.IsDeleted] = book.IsDeleted,
            [Columns.Books.CreatedAt] = book.CreatedAt,
            [Columns.Books.UpdatedAt] = book.UpdatedAt,
            [Columns.Books.Metadata] = book.MetadataJson
        };

        // SqlKata doesn't directly support SCOPE_IDENTITY() in insert
        // We need to use raw SQL for the insert with SCOPE_IDENTITY()
        var query = factory.Query(Tables.Books).AsInsert(insertData);
        var sql = factory.Compiler.Compile(query).Sql + "; SELECT CAST(SCOPE_IDENTITY() AS INT);";
        var bindings = factory.Compiler.Compile(query).Bindings;

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);

        // Add parameters from compiled query
        for (int i = 0; i < bindings.Count; i++)
        {
            command.Parameters.AddWithValue($"@p{i}", bindings[i] ?? DBNull.Value);
        }

        var newId = (int)await command.ExecuteScalarAsync(cancellationToken);

        // Fetch the created book using the same transaction
        return await GetByIdAsync(newId, transaction, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve newly created book");
    }

    public async Task<Book?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        var factory = QueryFactoryProvider.Create(transaction);

        var result = await factory
            .Query(Tables.Books)
            .Select(GetBookColumns())
            .Where(Columns.Books.Id, id)
            .FirstOrDefaultAsync<dynamic>(transaction: transaction, cancellationToken: cancellationToken);

        return result != null ? MapDynamicToBook(result) : null;
    }

    public async Task<Book?> GetByIsbnAsync(string isbn, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        var factory = QueryFactoryProvider.Create(transaction);

        var result = await factory
            .Query(Tables.Books)
            .Select(GetBookColumns())
            .Where(Columns.Books.ISBN, isbn)
            .FirstOrDefaultAsync<dynamic>(transaction: transaction, cancellationToken: cancellationToken);

        return result != null ? MapDynamicToBook(result) : null;
    }

    public async Task<List<Book>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        bool includeDeleted,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var factory = QueryFactoryProvider.Create(transaction);

        var query = factory
            .Query(Tables.Books)
            .Select(GetBookColumns());

        if (!includeDeleted)
        {
            query = query.Where(Columns.Books.IsDeleted, false);
        }

        var results = await query
            .OrderBy(Columns.Books.Title)
            .ForPage(pageNumber, pageSize)
            .GetAsync<dynamic>(transaction: transaction, cancellationToken: cancellationToken);

        var books = new List<Book>();
        foreach (var result in results)
        {
            books.Add(MapDynamicToBook(result));
        }
        return books;
    }

    public async Task<List<Book>> SearchByTitleAsync(string searchTerm, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException("Search term cannot be empty", nameof(searchTerm));

        var factory = QueryFactoryProvider.Create(transaction);

        var results = await factory
            .Query(Tables.Books)
            .Select(GetBookColumns())
            .WhereContains(Columns.Books.Title, searchTerm)
            .Where(Columns.Books.IsDeleted, false)
            .OrderBy(Columns.Books.Title)
            .GetAsync<dynamic>(transaction: transaction, cancellationToken: cancellationToken);

        var books = new List<Book>();
        foreach (var result in results)
        {
            books.Add(MapDynamicToBook(result));
        }
        return books;
    }

    public async Task<List<Book>> GetByCategoryAsync(int categoryId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        var factory = QueryFactoryProvider.Create(transaction);

        var results = await factory
            .Query(Tables.Books)
            .Select(GetBookColumns())
            .Where(Columns.Books.CategoryId, categoryId)
            .Where(Columns.Books.IsDeleted, false)
            .OrderBy(Columns.Books.Title)
            .GetAsync<dynamic>(transaction: transaction, cancellationToken: cancellationToken);

        var books = new List<Book>();
        foreach (var result in results)
        {
            books.Add(MapDynamicToBook(result));
        }
        return books;
    }

    public async Task<int> GetCountAsync(bool includeDeleted, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        var factory = QueryFactoryProvider.Create(transaction);

        var query = factory.Query(Tables.Books);

        if (!includeDeleted)
        {
            query = query.Where(Columns.Books.IsDeleted, false);
        }

        return await query.CountAsync<int>(transaction: transaction, cancellationToken: cancellationToken);
    }

    public async Task<bool> UpdateAsync(Book book, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        var factory = QueryFactoryProvider.Create(transaction);

        var updateData = new Dictionary<string, object?>
        {
            [Columns.Books.ISBN] = book.ISBN,
            [Columns.Books.Title] = book.Title,
            [Columns.Books.Subtitle] = book.Subtitle,
            [Columns.Books.Description] = book.Description,
            [Columns.Books.Publisher] = book.Publisher,
            [Columns.Books.PublishedDate] = book.PublishedDate,
            [Columns.Books.PageCount] = book.PageCount,
            [Columns.Books.Language] = book.Language,
            [Columns.Books.CategoryId] = book.CategoryId,
            [Columns.Books.TotalCopies] = book.TotalCopies,
            [Columns.Books.AvailableCopies] = book.AvailableCopies,
            [Columns.Books.ShelfLocation] = book.ShelfLocation,
            [Columns.Books.UpdatedAt] = DateTime.UtcNow,
            [Columns.Books.Metadata] = book.MetadataJson
        };

        var affectedRows = await factory
            .Query(Tables.Books)
            .Where(Columns.Books.Id, book.Id)
            .UpdateAsync(updateData, transaction: transaction, cancellationToken: cancellationToken);

        return affectedRows > 0;
    }

    public async Task<bool> BorrowCopyAsync(int bookId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        // SqlKata doesn't support UPDATE with complex WHERE conditions easily
        // We use raw SQL for atomic check-and-update
        const string sql = @"
            UPDATE Books
            SET AvailableCopies = AvailableCopies - 1,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id
                AND AvailableCopies > 0
                AND IsDeleted = 0;";

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = bookId;
        command.Parameters.Add("@UpdatedAt", SqlDbType.DateTime2).Value = DateTime.UtcNow;

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> ReturnCopyAsync(int bookId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        // Atomic increment
        const string sql = @"
            UPDATE Books
            SET AvailableCopies = AvailableCopies + 1,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;";

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = bookId;
        command.Parameters.Add("@UpdatedAt", SqlDbType.DateTime2).Value = DateTime.UtcNow;

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        var factory = QueryFactoryProvider.Create(transaction);

        var updateData = new Dictionary<string, object?>
        {
            [Columns.Books.IsDeleted] = true,
            [Columns.Books.UpdatedAt] = DateTime.UtcNow
        };

        var affectedRows = await factory
            .Query(Tables.Books)
            .Where(Columns.Books.Id, id)
            .UpdateAsync(updateData, transaction: transaction, cancellationToken: cancellationToken);

        return affectedRows > 0;
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

        // SqlKata doesn't have built-in JSON_VALUE support, use raw SQL
        const string sql = @"
            SELECT
                Id, ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
                ShelfLocation, IsDeleted, CreatedAt, UpdatedAt, Metadata
            FROM Books
            WHERE JSON_VALUE(Metadata, @JsonPath) = @SearchValue
                AND IsDeleted = 0
            ORDER BY Title;";

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
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

        // Use OPENJSON for array expansion
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

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
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
        var factory = QueryFactoryProvider.Create(transaction);

        var results = await factory
            .Query(Tables.Books)
            .Select(GetBookColumns())
            .WhereNotNull(Columns.Books.Metadata)
            .Where(Columns.Books.IsDeleted, false)
            .OrderBy(Columns.Books.Title)
            .GetAsync<dynamic>(transaction: transaction, cancellationToken: cancellationToken);

        var books = new List<Book>();
        foreach (var result in results)
        {
            books.Add(MapDynamicToBook(result));
        }
        return books;
    }

    /// <summary>
    /// Returns the list of columns to select for Book queries.
    /// Uses generated column constants for compile-time safety.
    /// </summary>
    private static string[] GetBookColumns() => new[]
    {
        Columns.Books.Id,
        Columns.Books.ISBN,
        Columns.Books.Title,
        Columns.Books.Subtitle,
        Columns.Books.Description,
        Columns.Books.Publisher,
        Columns.Books.PublishedDate,
        Columns.Books.PageCount,
        Columns.Books.Language,
        Columns.Books.CategoryId,
        Columns.Books.TotalCopies,
        Columns.Books.AvailableCopies,
        Columns.Books.ShelfLocation,
        Columns.Books.IsDeleted,
        Columns.Books.CreatedAt,
        Columns.Books.UpdatedAt,
        Columns.Books.Metadata
    };

    /// <summary>
    /// Maps a dynamic result from SqlKata to a Book entity.
    /// </summary>
    private static Book MapDynamicToBook(dynamic row)
    {
        return Book.FromDatabase(
            id: (int)row.Id,
            isbn: (string)row.ISBN,
            title: (string)row.Title,
            subtitle: row.Subtitle,
            description: row.Description,
            publisher: row.Publisher,
            publishedDate: row.PublishedDate,
            pageCount: row.PageCount,
            language: row.Language,
            categoryId: (int)row.CategoryId,
            totalCopies: (int)row.TotalCopies,
            availableCopies: (int)row.AvailableCopies,
            shelfLocation: row.ShelfLocation,
            isDeleted: (bool)row.IsDeleted,
            createdAt: (DateTime)row.CreatedAt,
            updatedAt: (DateTime)row.UpdatedAt,
            metadataJson: row.Metadata
        );
    }

    /// <summary>
    /// Maps a SqlDataReader row to a Book entity (used for raw SQL queries).
    /// </summary>
    private static Book MapReaderToBook(SqlDataReader reader)
    {
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
}
