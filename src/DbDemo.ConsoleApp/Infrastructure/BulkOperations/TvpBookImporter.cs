using DbDemo.ConsoleApp.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace DbDemo.ConsoleApp.Infrastructure.BulkOperations;

/// <summary>
/// Demonstrates bulk insert operations using Table-Valued Parameters (TVPs)
/// TVPs offer a middle ground between SqlBulkCopy and individual INSERTs,
/// with support for stored procedure logic and validation
/// </summary>
public class TvpBookImporter
{
    private readonly string _connectionString;

    public TvpBookImporter(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Bulk inserts books using Table-Valued Parameters (TVP)
    /// Calls stored procedure with user-defined table type
    /// </summary>
    /// <param name="books">Collection of books to insert</param>
    /// <returns>Tuple of (insertedCount, elapsedMilliseconds)</returns>
    public async Task<(int insertedCount, long elapsedMs)> BulkInsertWithTvpAsync(
        IEnumerable<Book> books,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var bookList = books.ToList();

        // Create DataTable with book data (same structure as BookTableType)
        var dataTable = CreateBookDataTable(bookList);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Create command to call stored procedure
        await using var command = new SqlCommand("dbo.BulkInsertBooks", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 300 // 5 minutes timeout
        };

        // Add table-valued parameter
        var tvpParameter = command.Parameters.AddWithValue("@Books", dataTable);
        tvpParameter.SqlDbType = SqlDbType.Structured;
        tvpParameter.TypeName = "dbo.BookTableType";

        // Add output parameter for inserted count
        var outputParameter = command.Parameters.Add("@InsertedCount", SqlDbType.Int);
        outputParameter.Direction = ParameterDirection.Output;

        // Execute stored procedure
        await command.ExecuteNonQueryAsync(cancellationToken);

        // Get inserted count from output parameter
        var insertedCount = outputParameter.Value != DBNull.Value
            ? (int)outputParameter.Value
            : 0;

        stopwatch.Stop();

        return (insertedCount, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Bulk inserts books using TVP with explicit transaction management
    /// Demonstrates how to use TVP within an application-level transaction
    /// </summary>
    public async Task<(int insertedCount, long elapsedMs)> BulkInsertWithTvpAndTransactionAsync(
        IEnumerable<Book> books,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var bookList = books.ToList();

        var dataTable = CreateBookDataTable(bookList);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Begin transaction at application level
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = new SqlCommand("dbo.BulkInsertBooks", connection, transaction)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 300
            };

            var tvpParameter = command.Parameters.AddWithValue("@Books", dataTable);
            tvpParameter.SqlDbType = SqlDbType.Structured;
            tvpParameter.TypeName = "dbo.BookTableType";

            var outputParameter = command.Parameters.Add("@InsertedCount", SqlDbType.Int);
            outputParameter.Direction = ParameterDirection.Output;

            await command.ExecuteNonQueryAsync(cancellationToken);

            var insertedCount = outputParameter.Value != DBNull.Value
                ? (int)outputParameter.Value
                : 0;

            // Commit the transaction
            await transaction.CommitAsync(cancellationToken);

            stopwatch.Stop();
            return (insertedCount, stopwatch.ElapsedMilliseconds);
        }
        catch
        {
            // Rollback on any error
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Retrieves books by ISBNs using helper stored procedure
    /// </summary>
    public async Task<List<Book>> GetBooksByISBNsAsync(
        IEnumerable<string> isbns,
        CancellationToken cancellationToken = default)
    {
        var isbnList = string.Join(",", isbns);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.GetBooksByISBNs", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@ISBNs", isbnList);

        var books = new List<Book>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var book = MapReaderToBook(reader);
            books.Add(book);
        }

        return books;
    }

    /// <summary>
    /// Checks if the TVP infrastructure (type and stored procedure) exists
    /// </summary>
    public async Task<bool> IsTvpInfrastructureAvailableAsync(CancellationToken cancellationToken = default)
    {
        const string checkSql = @"
            SELECT
                CASE WHEN EXISTS (SELECT 1 FROM sys.types WHERE is_table_type = 1 AND name = 'BookTableType') THEN 1 ELSE 0 END as HasType,
                CASE WHEN EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'BulkInsertBooks') THEN 1 ELSE 0 END as HasProcedure";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(checkSql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            var hasType = reader.GetInt32(0) == 1;
            var hasProcedure = reader.GetInt32(1) == 1;
            return hasType && hasProcedure;
        }

        return false;
    }

    #region Helper Methods

    /// <summary>
    /// Creates a DataTable matching the BookTableType structure
    /// Note: Does NOT include Id, IsDeleted, CreatedAt, UpdatedAt as these are set by the stored procedure
    /// </summary>
    private DataTable CreateBookDataTable(List<Book> books)
    {
        var dataTable = new DataTable("Books");

        // Define columns matching BookTableType (NOT the Books table)
        dataTable.Columns.Add("ISBN", typeof(string));
        dataTable.Columns.Add("Title", typeof(string));
        dataTable.Columns.Add("Subtitle", typeof(string));
        dataTable.Columns.Add("Description", typeof(string));
        dataTable.Columns.Add("Publisher", typeof(string));
        dataTable.Columns.Add("PublishedDate", typeof(DateTime));
        dataTable.Columns.Add("PageCount", typeof(int));
        dataTable.Columns.Add("Language", typeof(string));
        dataTable.Columns.Add("CategoryId", typeof(int));
        dataTable.Columns.Add("TotalCopies", typeof(int));
        dataTable.Columns.Add("AvailableCopies", typeof(int));
        dataTable.Columns.Add("ShelfLocation", typeof(string));

        // Populate rows
        foreach (var book in books)
        {
            var row = dataTable.NewRow();

            row["ISBN"] = book.ISBN;
            row["Title"] = book.Title;
            row["Subtitle"] = book.Subtitle ?? (object)DBNull.Value;
            row["Description"] = book.Description ?? (object)DBNull.Value;
            row["Publisher"] = book.Publisher ?? (object)DBNull.Value;
            row["PublishedDate"] = book.PublishedDate.HasValue ? book.PublishedDate.Value : DBNull.Value;
            row["PageCount"] = book.PageCount.HasValue ? book.PageCount.Value : DBNull.Value;
            row["Language"] = book.Language ?? (object)DBNull.Value;
            row["CategoryId"] = book.CategoryId;
            row["TotalCopies"] = book.TotalCopies;
            row["AvailableCopies"] = book.AvailableCopies;
            row["ShelfLocation"] = book.ShelfLocation ?? (object)DBNull.Value;

            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    /// <summary>
    /// Maps a SqlDataReader row to a Book entity
    /// </summary>
    private Book MapReaderToBook(SqlDataReader reader)
    {
        var isbn = reader.GetString(reader.GetOrdinal("ISBN"));
        var title = reader.GetString(reader.GetOrdinal("Title"));
        var categoryId = reader.GetInt32(reader.GetOrdinal("CategoryId"));
        var totalCopies = reader.GetInt32(reader.GetOrdinal("TotalCopies"));

        var book = new Book(isbn, title, categoryId, totalCopies);

        // Set optional fields
        var subtitleOrdinal = reader.GetOrdinal("Subtitle");
        if (!reader.IsDBNull(subtitleOrdinal))
        {
            var subtitle = reader.GetString(subtitleOrdinal);
            var descriptionOrdinal = reader.GetOrdinal("Description");
            var description = !reader.IsDBNull(descriptionOrdinal)
                ? reader.GetString(descriptionOrdinal)
                : null;
            var publisherOrdinal = reader.GetOrdinal("Publisher");
            var publisher = !reader.IsDBNull(publisherOrdinal)
                ? reader.GetString(publisherOrdinal)
                : null;

            book.UpdateDetails(title, subtitle, description, publisher);
        }

        // Set publishing info if available
        var publishedDateOrdinal = reader.GetOrdinal("PublishedDate");
        var pageCountOrdinal = reader.GetOrdinal("PageCount");
        var languageOrdinal = reader.GetOrdinal("Language");

        if (!reader.IsDBNull(publishedDateOrdinal) ||
            !reader.IsDBNull(pageCountOrdinal) ||
            !reader.IsDBNull(languageOrdinal))
        {
            var publishedDate = !reader.IsDBNull(publishedDateOrdinal)
                ? reader.GetDateTime(publishedDateOrdinal)
                : (DateTime?)null;
            var pageCount = !reader.IsDBNull(pageCountOrdinal)
                ? reader.GetInt32(pageCountOrdinal)
                : (int?)null;
            var language = !reader.IsDBNull(languageOrdinal)
                ? reader.GetString(languageOrdinal)
                : null;

            book.UpdatePublishingInfo(publishedDate, pageCount, language);
        }

        // Set shelf location if available
        var shelfLocationOrdinal = reader.GetOrdinal("ShelfLocation");
        if (!reader.IsDBNull(shelfLocationOrdinal))
        {
            var shelfLocation = reader.GetString(shelfLocationOrdinal);
            book.UpdateShelfLocation(shelfLocation);
        }

        return book;
    }

    #endregion
}
