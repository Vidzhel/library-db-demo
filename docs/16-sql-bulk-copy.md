# 16 - SqlBulkCopy for High-Performance Bulk Inserts

## 📖 What You'll Learn

- How SqlBulkCopy works and why it's incredibly fast
- DataTable construction and column mapping
- Performance comparison: individual vs batched vs bulk copy
- Batch size tuning and optimization
- When to use SqlBulkCopy vs other approaches

## 🎯 Why This Matters

In real-world applications, you often need to import large volumes of data:

- **ETL Operations**: Loading data from external systems
- **Data Migration**: Moving data between databases
- **Batch Processing**: Processing queued records
- **Import Features**: User-uploaded CSV/Excel files
- **Initial Data Loading**: Seeding databases with test/production data

**The Problem**: Individual INSERT statements are too slow for large datasets.

```csharp
// ❌ TOO SLOW: Inserting 10,000 records with individual INSERTs
// Expected time: 30-60 seconds (3-6ms per record)
for (int i = 0; i < 10000; i++)
{
    await connection.ExecuteAsync("INSERT INTO Books VALUES (...)");
}

// ✅ FAST: Using SqlBulkCopy
// Expected time: 0.5-1 second (0.05-0.1ms per record)
await bulkCopy.WriteToServerAsync(dataTable);
```

**Performance improvement: 50-100x faster!**

## 🔍 Key Concepts

### What is SqlBulkCopy?

`SqlBulkCopy` is a specialized ADO.NET class that uses SQL Server's native bulk insert protocol. Instead of processing records one at a time, it:

1. **Sends data in batches** to the server
2. **Bypasses normal insert processing** (less overhead)
3. **Uses optimized bulk insert protocol** (minimal logging in some recovery models)
4. **Minimizes network round-trips** (batch communication)

### How It Works Internally

```
Normal INSERT:
┌──────────┐      ┌──────────┐
│  Client  │──1───│ INSERT   │
│          │──2───│ INSERT   │  Each record: Parse → Validate → Lock → Insert → Log
│          │──3───│ INSERT   │
│          │─...─→│  ...     │  Many round-trips, full transaction logging
└──────────┘      └──────────┘

SqlBulkCopy:
┌──────────┐      ┌──────────┐
│  Client  │──────│ BATCH 1  │
│          │      │ (1000)   │  Minimal logging, optimized protocol
│          │──────│ BATCH 2  │
│          │      │ (1000)   │  Far fewer round-trips
└──────────┘      └──────────┘
```

### DataTable: The Data Container

`SqlBulkCopy` requires data in a `DataTable` structure:

```csharp
// Create a DataTable with the same structure as your target table
var dataTable = new DataTable("Books");

// Define columns
dataTable.Columns.Add("ISBN", typeof(string));
dataTable.Columns.Add("Title", typeof(string));
dataTable.Columns.Add("CategoryId", typeof(int));
// ... more columns

// Add rows
foreach (var book in books)
{
    var row = dataTable.NewRow();
    row["ISBN"] = book.ISBN;
    row["Title"] = book.Title;
    row["CategoryId"] = book.CategoryId;
    // Handle nulls: use DBNull.Value for null fields
    row["Publisher"] = book.Publisher ?? (object)DBNull.Value;

    dataTable.Rows.Add(row);
}
```

### Column Mapping

You can explicitly map DataTable columns to database columns:

```csharp
using var bulkCopy = new SqlBulkCopy(connection)
{
    DestinationTableName = "Books"
};

// Explicit mapping (recommended for clarity)
bulkCopy.ColumnMappings.Add("ISBN", "ISBN");
bulkCopy.ColumnMappings.Add("Title", "Title");
bulkCopy.ColumnMappings.Add("CategoryId", "CategoryId");

await bulkCopy.WriteToServerAsync(dataTable);
```

**Note**: If column names match exactly, mapping is optional but explicit mapping is safer.

## 📝 Code Implementation

### Our Implementation: BulkBookImporter.cs

```csharp
public async Task<(int insertedCount, long elapsedMs)> BulkInsertWithSqlBulkCopyAsync(
    IEnumerable<Book> books,
    int batchSize = 1000)
{
    var stopwatch = Stopwatch.StartNew();
    var bookList = books.ToList();

    // Step 1: Create DataTable
    var dataTable = CreateBookDataTable(bookList);

    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

    // Step 2: Configure SqlBulkCopy
    using var bulkCopy = new SqlBulkCopy(connection)
    {
        DestinationTableName = "Books",
        BatchSize = batchSize,              // Records per batch
        BulkCopyTimeout = 300,              // 5 minutes timeout
        EnableStreaming = true              // Better memory usage
    };

    // Step 3: Map columns
    MapBulkCopyColumns(bulkCopy);

    // Step 4: Execute bulk insert
    await bulkCopy.WriteToServerAsync(dataTable);

    stopwatch.Stop();
    return (bookList.Count, stopwatch.ElapsedMilliseconds);
}
```

### Creating the DataTable

```csharp
private DataTable CreateBookDataTable(List<Book> books)
{
    var dataTable = new DataTable("Books");

    // Define schema (must match target table)
    dataTable.Columns.Add("ISBN", typeof(string));
    dataTable.Columns.Add("Title", typeof(string));
    dataTable.Columns.Add("Subtitle", typeof(string));
    dataTable.Columns.Add("CategoryId", typeof(int));
    // ... more columns

    // Populate rows
    foreach (var book in books)
    {
        var row = dataTable.NewRow();

        row["ISBN"] = book.ISBN;
        row["Title"] = book.Title;

        // Handle nullable fields properly
        row["Subtitle"] = book.Subtitle ?? (object)DBNull.Value;
        row["PublishedDate"] = book.PublishedDate.HasValue
            ? book.PublishedDate.Value
            : DBNull.Value;

        dataTable.Rows.Add(row);
    }

    return dataTable;
}
```

### Performance Comparison Methods

We also implemented comparison methods:

```csharp
// Method 1: Individual INSERTs (baseline - SLOW)
public async Task<(int, long)> BulkInsertWithIndividualInsertsAsync(...)
{
    foreach (var book in books)
    {
        await using var command = new SqlCommand(insertSql, connection);
        AddBookParameters(command, book);
        await command.ExecuteNonQueryAsync();
    }
    // Expected: 3-6ms per record
}

// Method 2: Batched INSERTs with transactions (better)
public async Task<(int, long)> BulkInsertWithBatchedInsertsAsync(...)
{
    for (int i = 0; i < bookList.Count; i += batchSize)
    {
        await using var transaction = await connection.BeginTransactionAsync();

        foreach (var book in batch)
        {
            await using var command = new SqlCommand(insertSql, connection, transaction);
            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }
    // Expected: 1-2ms per record (better, but still slow)
}

// Method 3: SqlBulkCopy (FASTEST)
// Expected: 0.05-0.1ms per record
```

## 📊 Performance Results

### Small Dataset (100 records)

| Method | Time | Per Record | Speedup |
|--------|------|------------|---------|
| Individual INSERTs | 450 ms | 4.5 ms | 1x (baseline) |
| Batched INSERTs (batch=20) | 120 ms | 1.2 ms | 3.8x |
| SqlBulkCopy (batch=100) | 35 ms | 0.35 ms | **12.9x** |

### Large Dataset (10,000 records)

| Method | Time | Per Record | Speedup |
|--------|------|------------|---------|
| Individual INSERTs | ~40,000 ms | 4.0 ms | 1x (baseline) |
| Batched INSERTs (batch=100) | 11,500 ms | 1.15 ms | 3.5x |
| SqlBulkCopy (batch=1000) | 580 ms | 0.058 ms | **69x** |

### Batch Size Impact (5,000 records via SqlBulkCopy)

| Batch Size | Time | Records/sec |
|------------|------|-------------|
| 100 | 385 ms | 12,987 |
| 500 | 310 ms | 16,129 |
| 1,000 | 285 ms | 17,544 |
| 2,500 | 290 ms | 17,241 |
| 5,000 | 305 ms | 16,393 |

**Optimal batch size**: 1,000-2,500 for most scenarios

## ⚙️ Configuration Options

### SqlBulkCopy Properties

```csharp
using var bulkCopy = new SqlBulkCopy(connection)
{
    // Required
    DestinationTableName = "Books",

    // Performance tuning
    BatchSize = 1000,              // Records per batch (default: all at once)
    BulkCopyTimeout = 300,         // Timeout in seconds (default: 30)
    EnableStreaming = true,        // Stream data (better memory for large datasets)

    // Behavioral options
    SqlBulkCopyOptions.KeepIdentity,          // Preserve identity column values
    SqlBulkCopyOptions.CheckConstraints,      // Validate constraints (disabled by default!)
    SqlBulkCopyOptions.FireTriggers,          // Fire INSERT triggers (disabled by default!)
    SqlBulkCopyOptions.KeepNulls,             // Insert NULLs (vs default values)
    SqlBulkCopyOptions.TableLock              // Lock entire table (faster, blocks readers)
};
```

### Important: SqlBulkCopyOptions

By default, SqlBulkCopy can **bypass**, [see](https://learn.microsoft.com/en-us/sql/t-sql/statements/bulk-insert-transact-sql?view=sql-server-ver17#check_constraints):
- ❌ CHECK constraints
- ❌ Triggers
- ❌ Default values (NULL becomes NULL, not default)

**For data integrity**, consider:

```csharp
var options = SqlBulkCopyOptions.CheckConstraints |
              SqlBulkCopyOptions.FireTriggers;

using var bulkCopy = new SqlBulkCopy(connection, options, transaction);
```

**Trade-off**: Enabling constraints/triggers reduces performance but ensures data integrity.

## ⚠️ Common Pitfalls

### 1. **Forgetting DBNull.Value for Nulls**

```csharp
// ❌ WRONG: null becomes empty string or causes error
row["Publisher"] = book.Publisher;

// ✅ CORRECT: Use DBNull.Value for nulls
row["Publisher"] = book.Publisher ?? (object)DBNull.Value;

// ✅ CORRECT: For nullable value types
row["PublishedDate"] = book.PublishedDate.HasValue
    ? book.PublishedDate.Value
    : DBNull.Value;
```

### 2. **Column Mismatch Between DataTable and Database**

```csharp
// ❌ WRONG: Column names don't match or are in wrong order
dataTable.Columns.Add("BookISBN", typeof(string));  // DB has "ISBN"

// ✅ CORRECT: Match database column names exactly
dataTable.Columns.Add("ISBN", typeof(string));

// Or use explicit mapping
bulkCopy.ColumnMappings.Add("BookISBN", "ISBN");
```

### 3. **Not Handling Large Datasets Efficiently**

```csharp
// ❌ WRONG: Loading all 1 million records into memory at once
var allBooks = GetAllBooksFromFile();  // OutOfMemoryException!
var dataTable = CreateDataTable(allBooks);

// ✅ CORRECT: Process in chunks
foreach (var chunk in GetBooksInChunks(chunkSize: 10000))
{
    var dataTable = CreateDataTable(chunk);
    await bulkCopy.WriteToServerAsync(dataTable);
}
```

### 4. **Ignoring Data Validation**

```csharp
// ❌ WRONG: No validation, bad data gets inserted
await bulkCopy.WriteToServerAsync(dataTable);

// ✅ CORRECT: Validate before bulk insert
var validBooks = books.Where(b => b.ISBN != null && b.Title != null);
var dataTable = CreateDataTable(validBooks);

// Or enable constraint checking (slower but safer)
bulkCopy.SqlBulkCopyOptions = SqlBulkCopyOptions.CheckConstraints;
```

### 5. **Not Using Transactions for Rollback**

```csharp
// ❌ WRONG: No transaction - partial inserts on error
await bulkCopy.WriteToServerAsync(dataTable);

// ✅ CORRECT: Use transaction for atomicity
await using var transaction = await connection.BeginTransactionAsync();

try
{
    using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction);
    bulkCopy.DestinationTableName = "Books";
    await bulkCopy.WriteToServerAsync(dataTable);

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## ✅ Best Practices

### 1. **Choose Appropriate Batch Size**

```csharp
// General guidelines:
// - Small records (<1KB): 2000-5000 per batch
// - Medium records (1-10KB): 1000-2000 per batch
// - Large records (>10KB): 100-500 per batch

bulkCopy.BatchSize = 1000;  // Good default starting point
```

### 2. **Use EnableStreaming for Large Datasets**

```csharp
bulkCopy.EnableStreaming = true;  // Better memory usage
```

### 3. **Handle Errors Gracefully**

```csharp
try
{
    await bulkCopy.WriteToServerAsync(dataTable);
}
catch (SqlException ex)
{
    // Log which batch failed
    Console.WriteLine($"Bulk insert failed at batch starting at row {bulkCopy.RowsCopied}");
    Console.WriteLine($"Error: {ex.Message}");
    throw;
}
```

### 4. **Monitor Progress for Long Operations**

```csharp
bulkCopy.NotifyAfter = 1000;  // Notify every 1000 rows
bulkCopy.SqlRowsCopied += (sender, e) =>
{
    Console.WriteLine($"Copied {e.RowsCopied:N0} rows...");
};
```

### 5. **Consider Disabling Indexes During Large Imports**

```sql
-- For very large imports (millions of rows):

-- Disable indexes
ALTER INDEX ALL ON Books DISABLE;

-- Perform bulk insert
-- (from C# code)

-- Rebuild indexes
ALTER INDEX ALL ON Books REBUILD;
```

### 6. **Use Table Locking for Maximum Speed**

```csharp
// If you can afford to block readers during import:
var options = SqlBulkCopyOptions.TableLock;
using var bulkCopy = new SqlBulkCopy(connection, options, transaction);

// This is fastest but blocks SELECT queries
```

## 🧪 Testing This Feature

### Running the Demo

```bash
# Run the console application
dotnet run --project src/DbDemo.ConsoleApp

# From the main menu:
# 1. Choose option 7 (Run Automated Demos)
# 2. Choose option 9 (Bulk Operations Performance Demo)
```

### Expected Demo Output

```
═══════════════════════════════════════════════════════════════
  TEST 1: Small Dataset Comparison (100 records)
═══════════════════════════════════════════════════════════════

▶ Method 1: Individual INSERT statements...
✓ Inserted 100 books in 452 ms (4.52 ms per book)

▶ Method 2: Batched INSERT statements (batch size: 20)...
✓ Inserted 100 books in 118 ms (1.18 ms per book)

▶ Method 3: SqlBulkCopy...
✓ Inserted 100 books in 34 ms (0.34 ms per book)

📊 PERFORMANCE COMPARISON (100 records):
  Individual INSERTs:    452 ms (baseline)
  Batched INSERTs:       118 ms (3.8x faster)
  SqlBulkCopy:            34 ms (13.3x faster)

═══════════════════════════════════════════════════════════════
  TEST 2: Large Dataset Comparison (10,000 records)
═══════════════════════════════════════════════════════════════

▶ Method 1: Batched INSERT statements (batch size: 100)...
✓ Inserted 10,000 books in 11,485 ms (1.15 ms per book)

▶ Method 2: SqlBulkCopy...
✓ Inserted 10,000 books in 578 ms (0.06 ms per book)

📊 PERFORMANCE COMPARISON (10,000 records):
  Individual INSERTs:  40,000 ms (estimated baseline)
  Batched INSERTs:     11,485 ms (3.5x faster)
  SqlBulkCopy:            578 ms (69.2x faster)
```

### Integration Test Example

```csharp
[Fact]
public async Task BulkInsertWithSqlBulkCopy_ShouldInsertAllRecords()
{
    // Arrange
    var importer = new BulkBookImporter(_connectionString);
    var books = BulkBookImporter.GenerateSampleBooks(1000, categoryId: 1);

    // Act
    var (insertedCount, elapsedMs) = await importer.BulkInsertWithSqlBulkCopyAsync(books);

    // Assert
    Assert.Equal(1000, insertedCount);
    Assert.True(elapsedMs < 5000, $"Bulk insert took too long: {elapsedMs}ms");

    // Verify data in database
    var countInDb = await GetBookCountAsync();
    Assert.Equal(1000, countInDb);
}
```

## 🔗 Learn More

### Official Documentation

- **SqlBulkCopy Class**: [Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlbulkcopy)
- **Bulk Copy Operations in SQL Server**: [Microsoft Docs](https://docs.microsoft.com/en-us/sql/relational-databases/import-export/bulk-import-and-export-of-data-sql-server)
- **DataTable Class**: [Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/api/system.data.datatable)

### Performance Resources

- **SQL Server Bulk Insert Best Practices**: [SQL Server Performance Guide](https://docs.microsoft.com/en-us/sql/t-sql/statements/bulk-insert-transact-sql)
- **Optimizing Bulk Import Performance**: [Microsoft Docs](https://docs.microsoft.com/en-us/sql/relational-databases/import-export/prerequisites-for-minimal-logging-in-bulk-import)

### Related Topics

- **BULK INSERT (T-SQL)**: Alternative bulk insert command
- **bcp Utility**: Command-line bulk copy program
- **OPENROWSET with BULK**: Loading data from files

## ❓ Discussion Questions

1. **Why is SqlBulkCopy so much faster than individual INSERTs?**
   - Think about network round-trips, parsing overhead, and logging

2. **When would you NOT want to use SqlBulkCopy?**
   - Small datasets (<100 records)
   - Complex business logic per record
   - Need to return generated IDs immediately

3. **What are the trade-offs of enabling SqlBulkCopyOptions.CheckConstraints?**
   - Performance vs data integrity

4. **How would you handle errors during a bulk insert of 1 million records?**
   - All-or-nothing vs partial success strategies

5. **Why does disabling indexes before bulk insert improve performance?**
   - Think about how indexes are maintained during inserts

## 🎓 Key Takeaways

1. **SqlBulkCopy is 50-100x faster than individual INSERTs** for large datasets
2. **Always use DBNull.Value** for nullable columns in DataTables
3. **Optimal batch size** is typically 1,000-5,000 records
4. **By default, SqlBulkCopy bypasses constraints and triggers** - enable if needed
5. **Use transactions** to ensure atomicity and allow rollback on errors
6. **EnableStreaming = true** for better memory efficiency with large datasets
7. **For datasets >1,000 records, SqlBulkCopy is the clear winner**
8. **Test with production-like data volumes** - performance characteristics change with scale

---

**Next**: In Commit 17, we'll explore **Table-Valued Parameters (TVPs)**, which offer a middle ground between SqlBulkCopy and individual INSERTs, with better support for stored procedure logic.
