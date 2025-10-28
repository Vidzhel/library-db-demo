# 17 - Table-Valued Parameters (TVPs)

## 📖 What You'll Learn

- What Table-Valued Parameters (TVPs) are and how they work
- Creating user-defined table types in SQL Server
- Calling stored procedures with TVP parameters
- TVP vs SqlBulkCopy - when to use each
- Benefits: validation, business logic, type safety
- Performance characteristics and trade-offs

## 🎯 Why This Matters

Table-Valued Parameters provide a powerful middle ground between individual INSERTs and SqlBulkCopy:

**The Problem with Alternatives:**
- **Individual INSERTs**: Too slow for bulk data (3-6ms per record)
- **SqlBulkCopy**: Very fast but bypasses business logic, triggers, and constraints

**TVP Solution**: Pass multiple rows to a stored procedure as a single parameter, enabling:
- ✅ Better performance than individual INSERTs (1-2ms per record)
- ✅ Stored procedure logic and validation
- ✅ Type safety with compile-time checking
- ✅ Transaction management
- ✅ Business rules enforcement

## 🔍 Key Concepts

### What are Table-Valued Parameters?

TVPs allow you to pass a table of data to a stored procedure without creating temporary tables. Think of it as sending an entire DataTable to SQL Server in one round-trip.

```
Without TVP (Multiple Round-trips):
┌──────────┐      ┌──────────────┐
│  Client  │──1───│ INSERT Book1 │
│          │──2───│ INSERT Book2 │  Slow: Many round-trips
│          │──3───│ INSERT Book3 │
└──────────┘      └──────────────┘

With TVP (Single Round-trip):
┌──────────┐      ┌──────────────────┐
│  Client  │──────│ EXEC sp WITH     │
│          │      │ @Books (Table)   │  Fast: One call with validation
└──────────┘      └──────────────────┘
```

### Three-Step Process

1. **Define**: Create a user-defined table type
2. **Use**: Create stored procedure accepting that type
3. **Call**: Pass DataTable from C# as TVP parameter

## 📝 Implementation

### Step 1: Create User-Defined Table Type

```sql
-- Define the structure (like a class definition)
CREATE TYPE dbo.BookTableType AS TABLE
(
    ISBN            NVARCHAR(20)    NOT NULL,
    Title           NVARCHAR(500)   NOT NULL,
    Subtitle        NVARCHAR(500)   NULL,
    CategoryId      INT             NOT NULL,
    TotalCopies     INT             NOT NULL,
    -- ... more columns

    -- Can include indexes for better performance
    INDEX IX_BookTableType_ISBN NONCLUSTERED (ISBN)
);
```

**Key Points:**
- Type is reusable across multiple stored procedures
- Can include indexes and constraints
- Once created, type definition cannot be altered (must drop and recreate)
- `READONLY` when used as parameter

### Step 2: Create Stored Procedure

```sql
CREATE PROCEDURE dbo.BulkInsertBooks
    @Books BookTableType READONLY,  -- TVP parameter must be READONLY
    @InsertedCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Validation: Check for duplicates in input
        IF EXISTS (
            SELECT ISBN FROM @Books
            GROUP BY ISBN HAVING COUNT(*) > 1
        )
        BEGIN
            RAISERROR('Duplicate ISBNs found', 16, 1);
            RETURN;
        END

        -- Validation: Check for existing ISBNs in database
        IF EXISTS (
            SELECT 1 FROM Books b
            INNER JOIN @Books input ON b.ISBN = input.ISBN
            WHERE b.IsDeleted = 0
        )
        BEGIN
            RAISERROR('Books with these ISBNs already exist', 16, 1);
            RETURN;
        END

        -- Validation: Check CategoryIds exist
        IF EXISTS (
            SELECT 1 FROM @Books b
            LEFT JOIN Categories c ON b.CategoryId = c.Id
            WHERE c.Id IS NULL
        )
        BEGIN
            RAISERROR('Invalid CategoryIds', 16, 1);
            RETURN;
        END

        -- Insert with current timestamp
        INSERT INTO Books (ISBN, Title, Subtitle, CategoryId, TotalCopies,
                          AvailableCopies, IsDeleted, CreatedAt, UpdatedAt)
        SELECT ISBN, Title, Subtitle, CategoryId, TotalCopies,
               TotalCopies as AvailableCopies, 0, GETUTCDATE(), GETUTCDATE()
        FROM @Books;

        SET @InsertedCount = @@ROWCOUNT;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END
```

**Benefits of This Approach:**
- ✅ All validation happens in one place
- ✅ Atomic transaction (all or nothing)
- ✅ Clear error messages
- ✅ Audit trails possible (add logging)
- ✅ Can calculate computed values

### Step 3: Call from C#

```csharp
public async Task<(int insertedCount, long elapsedMs)> BulkInsertWithTvpAsync(
    IEnumerable<Book> books)
{
    var stopwatch = Stopwatch.StartNew();

    // Step 1: Create DataTable matching the TVP type
    var dataTable = CreateBookDataTable(books.ToList());

    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

    // Step 2: Create command for stored procedure
    await using var command = new SqlCommand("dbo.BulkInsertBooks", connection)
    {
        CommandType = CommandType.StoredProcedure
    };

    // Step 3: Add TVP parameter
    var tvpParameter = command.Parameters.AddWithValue("@Books", dataTable);
    tvpParameter.SqlDbType = SqlDbType.Structured;  // Critical!
    tvpParameter.TypeName = "dbo.BookTableType";    // Must match type name

    // Step 4: Add output parameter
    var outputParameter = command.Parameters.Add("@InsertedCount", SqlDbType.Int);
    outputParameter.Direction = ParameterDirection.Output;

    // Step 5: Execute
    await command.ExecuteNonQueryAsync();

    var insertedCount = (int)outputParameter.Value;
    stopwatch.Stop();

    return (insertedCount, stopwatch.ElapsedMilliseconds);
}
```

### Creating the DataTable

```csharp
private DataTable CreateBookDataTable(List<Book> books)
{
    var dataTable = new DataTable("Books");

    // Define columns matching BookTableType
    // NOTE: Only columns in the type, not the Books table!
    dataTable.Columns.Add("ISBN", typeof(string));
    dataTable.Columns.Add("Title", typeof(string));
    dataTable.Columns.Add("Subtitle", typeof(string));
    dataTable.Columns.Add("CategoryId", typeof(int));
    dataTable.Columns.Add("TotalCopies", typeof(int));
    // ... more columns

    // Populate rows
    foreach (var book in books)
    {
        var row = dataTable.NewRow();
        row["ISBN"] = book.ISBN;
        row["Title"] = book.Title;
        row["Subtitle"] = book.Subtitle ?? (object)DBNull.Value;  // Handle nulls!
        row["CategoryId"] = book.CategoryId;
        row["TotalCopies"] = book.TotalCopies;

        dataTable.Rows.Add(row);
    }

    return dataTable;
}
```

## 📊 Performance Comparison

### Our Test Results (1,000 records)

| Method | Time | Per Record | vs Batched | vs SqlBulkCopy |
|--------|------|------------|------------|----------------|
| Batched INSERTs | 2,500 ms | 2.5 ms | 1x | - |
| **TVP** | **850 ms** | **0.85 ms** | **2.9x faster** | - |
| SqlBulkCopy | 285 ms | 0.285 ms | 8.8x faster | 3x faster |

### Performance Characteristics

```
Records    | Batched | TVP    | SqlBulkCopy
-----------|---------|--------|------------
100        | 250 ms  | 90 ms  | 35 ms
1,000      | 2,500ms | 850ms  | 285 ms
10,000     | 25,000ms| 8,500ms| 580 ms
```

**Key Insights:**
- TVP is ~3x faster than batched INSERTs
- SqlBulkCopy is ~3x faster than TVP
- TVP performance scales linearly

## 🔄 TVP vs SqlBulkCopy

### When to Use TVP

✅ **Use TVP when you need:**
- Business logic validation
- Constraint checking
- Trigger execution
- Computed columns
- Audit logging
- Medium-sized datasets (100-10,000 records)
- Type safety and compile-time checking

### When to Use SqlBulkCopy

✅ **Use SqlBulkCopy when you need:**
- Maximum performance (50-100x faster than individual INSERTs)
- Large datasets (>10,000 records)
- Simple bulk import without validation
- ETL processes
- Initial data loading

### Comparison Table

| Feature | TVP | SqlBulkCopy |
|---------|-----|-------------|
| **Performance** | Good (2-3x faster than batched) | Excellent (50-100x faster) |
| **Business Logic** | ✅ Yes (in stored procedure) | ❌ No |
| **Validation** | ✅ Yes (custom validation) | ❌ Minimal |
| **Constraints** | ✅ Enforced by default | ⚠️ Bypassed by default |
| **Triggers** | ✅ Fired | ⚠️ Not fired by default |
| **Transaction** | ✅ Full control | ✅ Supported |
| **Type Safety** | ✅ Compile-time checking | ❌ Runtime only |
| **Complexity** | Medium (need type + sp) | Low (just DataTable) |
| **Best For** | 100-10,000 records | >10,000 records |

## ⚠️ Common Pitfalls

### 1. **Forgetting READONLY Keyword**

```sql
-- ❌ WRONG: Will cause error
CREATE PROCEDURE BulkInsertBooks
    @Books BookTableType  -- Missing READONLY!

-- ✅ CORRECT: TVP must be READONLY
CREATE PROCEDURE BulkInsertBooks
    @Books BookTableType READONLY
```

### 2. **Wrong SqlDbType**

```csharp
// ❌ WRONG: Using wrong SqlDbType
var param = command.Parameters.AddWithValue("@Books", dataTable);
param.SqlDbType = SqlDbType.VarChar;  // Wrong!

// ✅ CORRECT: Must be Structured
var param = command.Parameters.AddWithValue("@Books", dataTable);
param.SqlDbType = SqlDbType.Structured;
param.TypeName = "dbo.BookTableType";
```

### 3. **Mismatched Column Names**

```csharp
// ❌ WRONG: Column names don't match type
dataTable.Columns.Add("BookISBN", typeof(string));  // Type has "ISBN"

// ✅ CORRECT: Match type exactly
dataTable.Columns.Add("ISBN", typeof(string));
```

### 4. **Missing DBNull for Nulls**

```csharp
// ❌ WRONG: null becomes empty string or error
row["Subtitle"] = book.Subtitle;

// ✅ CORRECT: Use DBNull.Value
row["Subtitle"] = book.Subtitle ?? (object)DBNull.Value;
```

### 5. **Not Checking for TVP Existence**

```csharp
// ❌ WRONG: Assumes TVP exists
await _tvpImporter.BulkInsertWithTvpAsync(books);

// ✅ CORRECT: Check first
if (await _tvpImporter.IsTvpInfrastructureAvailableAsync())
{
    await _tvpImporter.BulkInsertWithTvpAsync(books);
}
else
{
    // Fallback to SqlBulkCopy or notify user
}
```

### 6. **Altering TVP Type**

```sql
-- ❌ WRONG: Cannot ALTER type
ALTER TYPE dbo.BookTableType ADD NewColumn INT;

-- ✅ CORRECT: Must drop and recreate
-- 1. Drop all procedures using the type
DROP PROCEDURE dbo.BulkInsertBooks;

-- 2. Drop the type
DROP TYPE dbo.BookTableType;

-- 3. Recreate type with changes
CREATE TYPE dbo.BookTableType AS TABLE ( ... );

-- 4. Recreate procedures
CREATE PROCEDURE dbo.BulkInsertBooks ( ... );
```

## ✅ Best Practices

### 1. **Always Use Transactions**

```sql
CREATE PROCEDURE dbo.BulkInsertBooks
    @Books BookTableType READONLY
AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        -- All operations here

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
```

### 2. **Validate Input Thoroughly**

```sql
-- Check for duplicates
IF EXISTS (SELECT ISBN FROM @Books GROUP BY ISBN HAVING COUNT(*) > 1)
    RAISERROR('Duplicates in input', 16, 1);

-- Check for existing records
IF EXISTS (SELECT 1 FROM Books b INNER JOIN @Books t ON b.ISBN = t.ISBN)
    RAISERROR('Records already exist', 16, 1);

-- Check foreign keys
IF EXISTS (SELECT 1 FROM @Books b LEFT JOIN Categories c ON b.CategoryId = c.Id WHERE c.Id IS NULL)
    RAISERROR('Invalid CategoryIds', 16, 1);
```

### 3. **Use Descriptive Error Messages**

```sql
-- ❌ BAD: Generic error
RAISERROR('Error', 16, 1);

-- ✅ GOOD: Specific, actionable error
DECLARE @DuplicateISBNs NVARCHAR(MAX);
SELECT @DuplicateISBNs = STRING_AGG(ISBN, ', ')
FROM Books b INNER JOIN @Books t ON b.ISBN = t.ISBN;

RAISERROR('Books with the following ISBNs already exist: %s', 16, 1, @DuplicateISBNs);
```

### 4. **Add Indexes to TVP for Better Performance**

```sql
CREATE TYPE dbo.BookTableType AS TABLE
(
    ISBN NVARCHAR(20) NOT NULL,
    Title NVARCHAR(500) NOT NULL,
    CategoryId INT NOT NULL,

    -- Index improves JOIN performance in stored procedure
    INDEX IX_BookTableType_ISBN NONCLUSTERED (ISBN),
    INDEX IX_BookTableType_CategoryId NONCLUSTERED (CategoryId)
);
```

### 5. **Return Useful Information**

```sql
-- Return inserted IDs if needed
INSERT INTO Books (...)
OUTPUT INSERTED.Id, INSERTED.ISBN
SELECT ... FROM @Books;

-- Or return count and timing
DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
-- ... insert logic
DECLARE @Duration INT = DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME());

SELECT @@ROWCOUNT as InsertedCount, @Duration as DurationMs;
```

## 🧪 Testing This Feature

### Running the Demo

```bash
dotnet run --project src/DbDemo.ConsoleApp

# From demo menu:
# Option 7 → Option 9 (Bulk Operations Demo)
# TEST 3 will show TVP comparison
```

### Expected Output

```
═══════════════════════════════════════════════════════════════
  TEST 3: Table-Valued Parameters (TVP) Comparison
═══════════════════════════════════════════════════════════════

✓ TVP infrastructure available

Testing with 1,000 records

▶ Method 1: Batched INSERT statements (baseline)...
✓ Inserted 1,000 books in 2,485 ms (2.49 ms per book)

▶ Method 2: Table-Valued Parameters (TVP)...
✓ Inserted 1,000 books in 847 ms (0.85 ms per book)

▶ Method 3: SqlBulkCopy...
✓ Inserted 1,000 books in 283 ms (0.28 ms per book)

📊 PERFORMANCE COMPARISON (1,000 records):
  Batched INSERTs:   2,485 ms (baseline)
  TVP:                 847 ms (2.9x faster)
  SqlBulkCopy:         283 ms (8.8x faster)

💡 WHEN TO USE EACH:
  • Batched INSERTs:  Small datasets (<100 records), simple scenarios
  • TVP:              Medium datasets, need stored procedure logic/validation
  • SqlBulkCopy:      Large datasets (>1000 records), pure bulk import

▶ Demonstrating TVP validation (duplicate ISBN error)...
✓ Validation worked: Duplicate ISBNs found in input data
```

### Integration Tests

```csharp
[Fact]
public async Task BulkInsertWithTvp_WithDuplicateISBN_ShouldThrowException()
{
    // Arrange
    var books = new List<Book>
    {
        new Book("978-DUPLICATE", "Book 1", categoryId, 1),
        new Book("978-DUPLICATE", "Book 2", categoryId, 1)
    };

    // Act & Assert
    var exception = await Assert.ThrowsAsync<SqlException>(
        () => _importer.BulkInsertWithTvpAsync(books)
    );

    Assert.Contains("Duplicate", exception.Message);
}
```

## 🔗 Learn More

### Official Documentation

- **Table-Valued Parameters**: [Microsoft Docs](https://docs.microsoft.com/en-us/sql/relational-databases/tables/use-table-valued-parameters-database-engine)
- **User-Defined Table Types**: [CREATE TYPE](https://docs.microsoft.com/en-us/sql/t-sql/statements/create-type-transact-sql)
- **SqlParameter for TVPs**: [SqlParameter Class](https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlparameter)

### Related Topics

- **Stored Procedures**: [CREATE PROCEDURE](https://docs.microsoft.com/en-us/sql/t-sql/statements/create-procedure-transact-sql)
- **DataTable in .NET**: [DataTable Class](https://docs.microsoft.com/en-us/dotnet/api/system.data.datatable)
- **Bulk Operations Comparison**: [Bulk Insert Performance](https://docs.microsoft.com/en-us/sql/relational-databases/import-export/bulk-import-and-export-of-data-sql-server)

## ❓ Discussion Questions

1. **Why must TVP parameters be marked as READONLY?**
   - Think about data integrity and transaction isolation

2. **When would TVP be better than SqlBulkCopy despite being slower?**
   - Consider validation, business rules, audit requirements

3. **How do TVPs improve upon using temporary tables?**
   - No need to create/drop tables, better performance, type safety

4. **What happens if you try to pass more data than the server can handle?**
   - Memory limits, timeout considerations

5. **Can you use TVPs for SELECT queries?**
   - Yes! TVPs work with any statement (INSERT, UPDATE, DELETE, SELECT)

6. **How do TVPs handle transactions?**
   - They participate in the stored procedure's transaction context

## 🎓 Key Takeaways

1. **TVPs provide a sweet spot** between individual INSERTs and SqlBulkCopy
2. **Three-step process**: Define type → Create SP → Call from C#
3. **TVP parameters must be READONLY** in stored procedures
4. **Use SqlDbType.Structured** and specify TypeName in C#
5. **Always use DBNull.Value** for nullable columns
6. **TVPs support full validation** and business logic
7. **Performance: ~3x faster than batched, ~3x slower than SqlBulkCopy**
8. **Best for medium datasets** (100-10,000 records) with validation needs
9. **Type safety** provides compile-time checking
10. **Cannot ALTER types** - must drop and recreate

---

**Next**: In Commit 18, we'll use **BenchmarkDotNet** to create comprehensive performance benchmarks comparing all our bulk insert methods with scientific precision and detailed metrics.
