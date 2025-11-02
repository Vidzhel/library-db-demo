using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace DbDemo.Infrastructure.BulkOperations;

using DbDemo.Domain.Entities;

/// <summary>
/// Demonstrates bulk insert operations using SqlBulkCopy
/// Compares performance against individual INSERT statements
/// </summary>
public class BulkBookImporter
{
    private readonly string _connectionString;

    public BulkBookImporter(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Bulk inserts books using SqlBulkCopy - the fastest method
    /// </summary>
    /// <param name="books">Collection of books to insert</param>
    /// <param name="batchSize">Number of rows to send per batch (default: 1000)</param>
    /// <returns>Tuple of (insertedCount, elapsedMilliseconds)</returns>
    public async Task<(int insertedCount, long elapsedMs)> BulkInsertWithSqlBulkCopyAsync(
        IEnumerable<Book> books,
        int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var bookList = books.ToList();

        // Create DataTable with book data
        var dataTable = CreateBookDataTable(bookList);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Configure SqlBulkCopy
        using var bulkCopy = new SqlBulkCopy(connection)
        {
            DestinationTableName = "Books",
            BatchSize = batchSize,
            BulkCopyTimeout = 300, // 5 minutes timeout for large datasets
            EnableStreaming = true  // Better memory usage for large datasets
        };

        // Map DataTable columns to database columns
        MapBulkCopyColumns(bulkCopy);

        // Perform the bulk insert
        await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);

        stopwatch.Stop();

        return (bookList.Count, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Inserts books using individual INSERT statements - slow baseline for comparison
    /// </summary>
    /// <param name="books">Collection of books to insert</param>
    /// <returns>Tuple of (insertedCount, elapsedMilliseconds)</returns>
    public async Task<(int insertedCount, long elapsedMs)> BulkInsertWithIndividualInsertsAsync(
        IEnumerable<Book> books,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var bookList = books.ToList();
        int insertedCount = 0;

        const string sql = @"
            INSERT INTO Books (
                ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
                ShelfLocation, IsDeleted, CreatedAt, UpdatedAt
            )
            VALUES (
                @ISBN, @Title, @Subtitle, @Description, @Publisher, @PublishedDate,
                @PageCount, @Language, @CategoryId, @TotalCopies, @AvailableCopies,
                @ShelfLocation, @IsDeleted, @CreatedAt, @UpdatedAt
            );";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var book in bookList)
        {
            await using var command = new SqlCommand(sql, connection);
            AddBookParameters(command, book);
            await command.ExecuteNonQueryAsync(cancellationToken);
            insertedCount++;
        }

        stopwatch.Stop();

        return (insertedCount, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Inserts books using batched INSERT statements with transactions
    /// Middle-ground approach between individual and bulk copy
    /// </summary>
    /// <param name="books">Collection of books to insert</param>
    /// <param name="batchSize">Number of books to insert per transaction</param>
    /// <returns>Tuple of (insertedCount, elapsedMilliseconds)</returns>
    public async Task<(int insertedCount, long elapsedMs)> BulkInsertWithBatchedInsertsAsync(
        IEnumerable<Book> books,
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var bookList = books.ToList();
        int insertedCount = 0;

        const string sql = @"
            INSERT INTO Books (
                ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
                ShelfLocation, IsDeleted, CreatedAt, UpdatedAt
            )
            VALUES (
                @ISBN, @Title, @Subtitle, @Description, @Publisher, @PublishedDate,
                @PageCount, @Language, @CategoryId, @TotalCopies, @AvailableCopies,
                @ShelfLocation, @IsDeleted, @CreatedAt, @UpdatedAt
            );";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Process in batches with transactions
        for (int i = 0; i < bookList.Count; i += batchSize)
        {
            var batch = bookList.Skip(i).Take(batchSize);

            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var book in batch)
                {
                    await using var command = new SqlCommand(sql, connection, transaction);
                    AddBookParameters(command, book);
                    await command.ExecuteNonQueryAsync(cancellationToken);
                    insertedCount++;
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        stopwatch.Stop();

        return (insertedCount, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Generates sample book data for testing bulk operations
    /// </summary>
    /// <param name="count">Number of books to generate</param>
    /// <param name="categoryId">Category ID for all books</param>
    /// <returns>List of generated books</returns>
    public static List<Book> GenerateSampleBooks(int count, int categoryId = 1)
    {
        var books = new List<Book>(count);
        var random = new Random(42); // Fixed seed for reproducibility

        var titlePrefixes = new[] { "The", "A", "An", "My", "Our", "Your" };
        var titleNouns = new[] { "Journey", "Adventure", "Story", "Tale", "Legend", "Chronicle", "Mystery", "Secret" };
        var titleAdjectives = new[] { "Lost", "Hidden", "Ancient", "Forgotten", "Unknown", "Mysterious", "Great" };

        for (int i = 0; i < count; i++)
        {
            // Generate unique ISBN-13
            var isbn = $"978-{random.Next(0, 10)}-{random.Next(100, 999)}-{random.Next(10000, 99999)}-{random.Next(0, 10)}";

            // Generate random title
            var prefix = titlePrefixes[random.Next(titlePrefixes.Length)];
            var adjective = titleAdjectives[random.Next(titleAdjectives.Length)];
            var noun = titleNouns[random.Next(titleNouns.Length)];
            var title = $"{prefix} {adjective} {noun} {i + 1}";

            // Create book with random data
            var book = new Book(isbn, title, categoryId, random.Next(1, 10));

            // Add optional details for some books
            if (i % 3 == 0)
            {
                book.UpdateDetails(
                    title,
                    $"Volume {(i % 5) + 1}",
                    $"An exciting story about {noun.ToLower()} number {i + 1}",
                    $"Publisher {random.Next(1, 20)}"
                );
            }

            if (i % 2 == 0)
            {
                var year = random.Next(1950, 2024);
                var month = random.Next(1, 13);
                var day = random.Next(1, 28);
                book.UpdatePublishingInfo(
                    new DateTime(year, month, day),
                    random.Next(100, 800),
                    "English"
                );
            }

            if (i % 4 == 0)
            {
                book.UpdateShelfLocation($"{(char)('A' + (i % 26))}-{random.Next(1, 50)}-{random.Next(1, 10)}");
            }

            books.Add(book);
        }

        return books;
    }

    #region Helper Methods

    /// <summary>
    /// Creates a DataTable structure matching the Books table schema
    /// </summary>
    private DataTable CreateBookDataTable(List<Book> books)
    {
        var dataTable = new DataTable("Books");

        // Define columns matching Books table structure
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
        dataTable.Columns.Add("IsDeleted", typeof(bool));
        dataTable.Columns.Add("CreatedAt", typeof(DateTime));
        dataTable.Columns.Add("UpdatedAt", typeof(DateTime));

        // Populate rows
        foreach (var book in books)
        {
            var row = dataTable.NewRow();

            row["ISBN"] = book.ISBN;
            row["Title"] = book.Title;
            row["Subtitle"] = (object?)book.Subtitle ?? DBNull.Value;
            row["Description"] = (object?)book.Description ?? DBNull.Value;
            row["Publisher"] = (object?)book.Publisher ?? DBNull.Value;
            row["PublishedDate"] = book.PublishedDate.HasValue ? book.PublishedDate.Value : DBNull.Value;
            row["PageCount"] = book.PageCount.HasValue ? book.PageCount.Value : DBNull.Value;
            row["Language"] = (object?)book.Language ?? DBNull.Value;
            row["CategoryId"] = book.CategoryId;
            row["TotalCopies"] = book.TotalCopies;
            row["AvailableCopies"] = book.AvailableCopies;
            row["ShelfLocation"] = (object?)book.ShelfLocation ?? DBNull.Value;
            row["IsDeleted"] = book.IsDeleted;
            row["CreatedAt"] = book.CreatedAt;
            row["UpdatedAt"] = book.UpdatedAt;

            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    /// <summary>
    /// Maps DataTable columns to database table columns for SqlBulkCopy
    /// </summary>
    private void MapBulkCopyColumns(SqlBulkCopy bulkCopy)
    {
        // Explicit column mapping (not strictly required if names match, but more explicit and safer)
        bulkCopy.ColumnMappings.Add("ISBN", "ISBN");
        bulkCopy.ColumnMappings.Add("Title", "Title");
        bulkCopy.ColumnMappings.Add("Subtitle", "Subtitle");
        bulkCopy.ColumnMappings.Add("Description", "Description");
        bulkCopy.ColumnMappings.Add("Publisher", "Publisher");
        bulkCopy.ColumnMappings.Add("PublishedDate", "PublishedDate");
        bulkCopy.ColumnMappings.Add("PageCount", "PageCount");
        bulkCopy.ColumnMappings.Add("Language", "Language");
        bulkCopy.ColumnMappings.Add("CategoryId", "CategoryId");
        bulkCopy.ColumnMappings.Add("TotalCopies", "TotalCopies");
        bulkCopy.ColumnMappings.Add("AvailableCopies", "AvailableCopies");
        bulkCopy.ColumnMappings.Add("ShelfLocation", "ShelfLocation");
        bulkCopy.ColumnMappings.Add("IsDeleted", "IsDeleted");
        bulkCopy.ColumnMappings.Add("CreatedAt", "CreatedAt");
        bulkCopy.ColumnMappings.Add("UpdatedAt", "UpdatedAt");
    }

    /// <summary>
    /// Adds book parameters to a SQL command (for individual INSERT operations)
    /// </summary>
    private void AddBookParameters(SqlCommand command, Book book)
    {
        command.Parameters.Add("@ISBN", SqlDbType.NVarChar, 20).Value = book.ISBN;
        command.Parameters.Add("@Title", SqlDbType.NVarChar, 500).Value = book.Title;
        command.Parameters.Add("@Subtitle", SqlDbType.NVarChar, 500).Value = (object?)book.Subtitle ?? DBNull.Value;
        command.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = (object?)book.Description ?? DBNull.Value;
        command.Parameters.Add("@Publisher", SqlDbType.NVarChar, 200).Value = (object?)book.Publisher ?? DBNull.Value;
        command.Parameters.Add("@PublishedDate", SqlDbType.Date).Value = book.PublishedDate.HasValue ? book.PublishedDate.Value : DBNull.Value;
        command.Parameters.Add("@PageCount", SqlDbType.Int).Value = book.PageCount.HasValue ? book.PageCount.Value : DBNull.Value;
        command.Parameters.Add("@Language", SqlDbType.NVarChar, 50).Value = (object?)book.Language ?? DBNull.Value;
        command.Parameters.Add("@CategoryId", SqlDbType.Int).Value = book.CategoryId;
        command.Parameters.Add("@TotalCopies", SqlDbType.Int).Value = book.TotalCopies;
        command.Parameters.Add("@AvailableCopies", SqlDbType.Int).Value = book.AvailableCopies;
        command.Parameters.Add("@ShelfLocation", SqlDbType.NVarChar, 50).Value = (object?)book.ShelfLocation ?? DBNull.Value;
        command.Parameters.Add("@IsDeleted", SqlDbType.Bit).Value = book.IsDeleted;
        command.Parameters.Add("@CreatedAt", SqlDbType.DateTime2).Value = book.CreatedAt;
        command.Parameters.Add("@UpdatedAt", SqlDbType.DateTime2).Value = book.UpdatedAt;
    }

    #endregion
}
