# 26 - Temporary Tables & Performance Comparison

## 📖 What You'll Learn

- `#TempTable` - Temporary tables with statistics and indexes
- `@TableVariable` - Table variables with limited optimization
- CTE (Common Table Expression) - Inline query optimization
- Performance characteristics of each approach
- When to use which temporary storage method

## 🎯 Why This Matters

Complex queries often need intermediate storage for multi-step calculations:
- **#TempTable**: Best for large datasets, multiple operations
- **@TableVariable**: Best for small datasets, single operation
- **CTE**: Best for single-use, inline calculations

Understanding the trade-offs helps you write efficient SQL that scales with your data.

## 🔍 Key Concepts

### Three Approaches to Temporary Storage

| Feature | #TempTable | @TableVariable | CTE |
|---------|------------|----------------|-----|
| **Storage** | tempdb database | Memory/tempdb | Query plan inline |
| **Statistics** | Yes (auto-updated) | No (fixed estimate) | Query optimizer decides |
| **Indexes** | Yes (explicit creation) | Primary key only | No (underlying table indexes) |
| **Scope** | Session/batch | Batch/procedure | Single query |
| **Transactions** | Participates | Does not participate | Participates |
| **Recompilation** | Minimal | Minimal | May cause recompilation |
| **Best For** | Large data, multi-step | Small data, single-use | Inline calculations |

### 1. #TempTable - Temporary Tables

**Characteristics:**
- Created in `tempdb` database
- Automatically gets statistics for query optimization
- Can create indexes for better performance
- Visible throughout session or until explicitly dropped
- Participates in transactions

**Syntax:**
```sql
CREATE TABLE #TempTableName
(
    Id INT PRIMARY KEY,
    Name NVARCHAR(100)
);

INSERT INTO #TempTableName (Id, Name)
SELECT Id, Name FROM SourceTable;

-- Multiple operations
UPDATE #TempTableName SET Name = UPPER(Name) WHERE Id > 10;

SELECT * FROM #TempTableName;

DROP TABLE #TempTableName;  -- Optional (auto-dropped at session end)
```

### 2. @TableVariable - Table Variables

**Characteristics:**
- Declared like variables with `DECLARE @Name TABLE`
- No statistics (optimizer assumes 1 row)
- Only primary key constraints (no explicit indexes)
- Limited to batch/procedure scope
- Does NOT participate in transactions (modifications not rolled back)

**Syntax:**
```sql
DECLARE @TableVar TABLE
(
    Id INT PRIMARY KEY,
    Name NVARCHAR(100)
);

INSERT INTO @TableVar (Id, Name)
SELECT Id, Name FROM SourceTable;

SELECT * FROM @TableVar;

-- No need to drop (auto-cleaned at end of batch)
```

### 3. CTE - Common Table Expression

**Characteristics:**
- Inline temporary result set
- No physical storage (part of main query)
- Optimizer can push predicates through CTE
- Cannot be reused in same query (define once, reference once)
- Great for readability and maintainability

**Syntax:**
```sql
WITH TemporaryResults AS
(
    SELECT Id, Name
    FROM SourceTable
    WHERE Active = 1
)
SELECT *
FROM TemporaryResults
WHERE Name LIKE 'A%';
```

## 🎯 Our Implementation: Library Statistics

Migration V016 creates three stored procedures that produce identical results using different approaches:

### sp_GetLibraryStatsWithTempTable

```sql
CREATE PROCEDURE dbo.sp_GetLibraryStatsWithTempTable
AS
BEGIN
    -- Step 1: Create temp table with schema
    CREATE TABLE #CategoryStats
    (
        CategoryId INT NOT NULL,
        CategoryName NVARCHAR(100) NOT NULL,
        TotalBooks INT NOT NULL,
        TotalLoans INT NOT NULL,
        ActiveLoans INT NOT NULL,
        PRIMARY KEY (CategoryId)
    );

    -- Step 2: Populate with book counts
    INSERT INTO #CategoryStats (...)
    SELECT ... FROM Categories;

    -- Step 3: Update with loan counts (multiple operations!)
    UPDATE #CategoryStats
    SET TotalLoans = ...
    FROM #CategoryStats cs
    INNER JOIN (...) AS loan_counts ON cs.CategoryId = loan_counts.CategoryId;

    -- Step 4: Calculate metrics and return
    SELECT
        cs.*,
        cs.TotalLoans / cs.TotalBooks AS AverageLoansPerBook,
        (SELECT TOP 1 Title FROM Books WHERE ...) AS MostPopularBook
    FROM #CategoryStats cs;

    DROP TABLE #CategoryStats;
END;
```

**Key Features:**
- Multiple operations on temp table (INSERT, UPDATE)
- Statistics enable efficient JOIN in UPDATE
- Can add indexes if needed
- Clean separation of concerns

### sp_GetLibraryStatsWithTableVariable

```sql
CREATE PROCEDURE dbo.sp_GetLibraryStatsWithTableVariable
AS
BEGIN
    DECLARE @CategoryStats TABLE
    (
        CategoryId INT NOT NULL PRIMARY KEY,
        ...
    );

    -- Single operation: populate table variable
    INSERT INTO @CategoryStats (...)
    SELECT
        C.Id,
        C.Name,
        COUNT(DISTINCT B.Id) AS TotalBooks,
        COUNT(L.Id) AS TotalLoans,
        SUM(CASE WHEN L.Status = 0 THEN 1 ELSE 0 END) AS ActiveLoans
    FROM Categories C
    LEFT JOIN Books B ON...
    LEFT JOIN Loans L ON...
    GROUP BY C.Id, C.Name;

    -- Return results
    SELECT ... FROM @CategoryStats cs;
END;
```

**Key Features:**
- Single INSERT operation (best practice)
- No statistics (works for small result sets)
- Scoped to procedure only
- No explicit DROP needed

### sp_GetLibraryStatsWithCTE

```sql
CREATE PROCEDURE dbo.sp_GetLibraryStatsWithCTE
AS
BEGIN
    WITH CategoryStats AS
    (
        SELECT
            C.Id,
            C.Name,
            COUNT(DISTINCT B.Id) AS TotalBooks,
            COUNT(L.Id) AS TotalLoans,
            SUM(CASE WHEN L.Status = 0 THEN 1 ELSE 0 END) AS ActiveLoans
        FROM Categories C
        LEFT JOIN Books B ON...
        LEFT JOIN Loans L ON...
        GROUP BY C.Id, C.Name
    )
    SELECT
        cs.*,
        CASE WHEN cs.TotalBooks > 0 THEN ... END AS AverageLoansPerBook,
        (SELECT TOP 1 Title FROM Books WHERE ...) AS MostPopularBook
    FROM CategoryStats cs;
END;
```

**Key Features:**
- No materialization (inline with main query)
- Optimizer can push predicates into CTE
- Clean, readable query structure
- No cleanup required

## ⚠️ Common Pitfalls

### ❌ Don't: Use @TableVariable for Large Datasets

```sql
-- BAD: Table variable with 100,000 rows
DECLARE @BigData TABLE (Id INT, Data NVARCHAR(MAX));
INSERT INTO @BigData SELECT ... FROM LargeTable;  -- Slow!

-- Optimizer assumes 1 row, creates terrible plan
SELECT * FROM @BigData WHERE Id = 12345;  -- Table scan!
```

**Why:** No statistics means optimizer assumes 1 row, leading to poor execution plans.

### ❌ Don't: Reuse Temp Tables Without Dropping

```sql
-- BAD: Reusing temp table name without drop
CREATE TABLE #Temp (Id INT);
-- ... later in same session ...
CREATE TABLE #Temp (Id INT);  -- Error! Already exists
```

**Fix:** Always `DROP TABLE #Temp` before recreating, or use `IF OBJECT_ID('tempdb..#Temp') IS NOT NULL DROP TABLE #Temp`

### ❌ Don't: Expect @TableVariable to Rollback

```sql
BEGIN TRANSACTION;
    DECLARE @Temp TABLE (Id INT);
    INSERT INTO @Temp VALUES (1), (2), (3);
ROLLBACK;  -- @Temp still contains 1, 2, 3!
```

**Why:** Table variables do NOT participate in transactions.

### ❌ Don't: Use CTE for Multi-Step Operations

```sql
-- BAD: Trying to "update" a CTE
WITH TempData AS (SELECT * FROM Table1)
UPDATE TempData SET Name = 'New';  -- Error!
```

**Fix:** Use #TempTable for multi-step operations.

## ✅ Best Practices

### 1. Choose Based on Data Size & Operations

```sql
-- Small dataset (< 100 rows), single use → @TableVariable
DECLARE @SmallList TABLE (Id INT);

-- Medium dataset (100-10,000 rows), multiple operations → #TempTable
CREATE TABLE #MediumData (Id INT PRIMARY KEY, Name NVARCHAR(100));

-- Large dataset (> 10,000 rows), complex logic → #TempTable with indexes
CREATE TABLE #LargeData (Id INT);
CREATE INDEX IX_LargeData_Name ON #LargeData(Name);

-- Inline calculation, single query → CTE
WITH Calculations AS (SELECT ...) SELECT * FROM Calculations;
```

### 2. Use Explicit Schema for #TempTable

```sql
-- GOOD: Explicit data types and constraints
CREATE TABLE #Orders
(
    OrderId INT NOT NULL PRIMARY KEY,
    CustomerId INT NOT NULL,
    OrderDate DATETIME2 NOT NULL,
    Total DECIMAL(10,2) NOT NULL
);
```

### 3. Drop #TempTable Explicitly

```sql
-- GOOD: Explicit cleanup
DROP TABLE #TempTable;

-- Even better: Check first
IF OBJECT_ID('tempdb..#TempTable') IS NOT NULL
    DROP TABLE #TempTable;
```

### 4. Populate @TableVariable in Single Operation

```sql
-- GOOD: Single INSERT
DECLARE @Data TABLE (Id INT, Value NVARCHAR(100));
INSERT INTO @Data (Id, Value)
SELECT Id, Value FROM SourceTable WHERE Active = 1;

-- AVOID: Multiple inserts (no statistics to optimize)
INSERT INTO @Data VALUES (1, 'A');
INSERT INTO @Data VALUES (2, 'B');  -- Slow for many rows
```

### 5. Use CTE for Readable Complex Queries

```sql
-- GOOD: Clear, readable hierarchy
WITH ActiveCustomers AS
(
    SELECT * FROM Customers WHERE Active = 1
),
CustomerOrders AS
(
    SELECT C.CustomerId, COUNT(*) AS OrderCount
    FROM ActiveCustomers C
    INNER JOIN Orders O ON C.CustomerId = O.CustomerId
    GROUP BY C.CustomerId
)
SELECT * FROM CustomerOrders WHERE OrderCount > 5;
```

## 🧪 Testing This Feature

Our tests (`TempTablePerformanceTests.cs`) verify:
1. **Result Consistency** - All three methods return identical results
2. **TempTable Correctness** - Calculations are accurate
3. **TableVariable Correctness** - Works for small datasets
4. **CTE Correctness** - Inline calculation produces correct results
5. **Performance** - TempTable scales well with large datasets
6. **Timing Comparison** - Track execution time for each method

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~TempTablePerformanceTests"
```

## 🔍 Performance Comparison from C#

```csharp
// Measure and compare all three approaches
var sw = Stopwatch.StartNew();
var tempTableResults = await reportRepository.GetLibraryStatsWithTempTableAsync(tx);
sw.Stop();
Console.WriteLine($"TempTable: {sw.ElapsedMilliseconds}ms for {tempTableResults.Count} rows");

sw.Restart();
var tableVariableResults = await reportRepository.GetLibraryStatsWithTableVariableAsync(tx);
sw.Stop();
Console.WriteLine($"TableVariable: {sw.ElapsedMilliseconds}ms for {tableVariableResults.Count} rows");

sw.Restart();
var cteResults = await reportRepository.GetLibraryStatsWithCTEAsync(tx);
sw.Stop();
Console.WriteLine($"CTE: {sw.ElapsedMilliseconds}ms for {cteResults.Count} rows");
```

## 🔗 Learn More

- [Temporary Tables vs Table Variables](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-table-transact-sql)
- [Common Table Expressions (CTEs)](https://learn.microsoft.com/en-us/sql/t-sql/queries/with-common-table-expression-transact-sql)
- [tempdb Best Practices](https://learn.microsoft.com/en-us/sql/relational-databases/databases/tempdb-database)

## ❓ Discussion Questions

1. Why do table variables not have statistics? What are the trade-offs?
2. When would you choose CTE over #TempTable despite performance?
3. How does transaction participation affect choice between #TempTable and @TableVariable?
4. What happens if multiple sessions create #TempTable with the same name?

## 💡 Try It Yourself

### Exercise 1: Add Index to #TempTable

Modify `sp_GetLibraryStatsWithTempTable` to add an index on `CategoryName`. Measure performance improvement.

### Exercise 2: Break @TableVariable

Create a table variable with 10,000 rows and query with WHERE clause. Compare execution plan with #TempTable.

### Exercise 3: Recursive CTE

Create a CTE that recursively calculates Fibonacci numbers up to N.

### Exercise 4: Transaction Behavior

Test transaction rollback with #TempTable vs @TableVariable to see the difference in behavior.

---

**Key Takeaway:** Choose #TempTable for large datasets and multi-step operations, @TableVariable for small single-use datasets, and CTE for inline calculations and readability. Understanding their differences is crucial for writing performant SQL.
