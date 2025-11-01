# 22 - Table-Valued Functions

## 📖 What You'll Learn

- What table-valued functions (TVFs) are and the two main types
- Differences between inline and multi-statement TVFs
- Creating inline TVFs for better performance
- Querying TVFs like tables in C# with ADO.NET
- When to use TVFs vs. views vs. stored procedures vs. scalar functions
- Performance characteristics and indexing considerations

## 🎯 Why This Matters

Table-valued functions are powerful for:
- **Parameterized Views**: Return filtered result sets based on input parameters
- **Reusable Queries**: Encapsulate complex queries with joins and aggregations
- **Composability**: Join TVF results with other tables, apply WHERE clauses, use in subqueries
- **Better Abstraction**: Hide complexity while providing a clean, queryable interface
- **Statistics & Reporting**: Aggregate data into meaningful summaries (like member statistics)

Unlike views, TVFs accept parameters. Unlike stored procedures, TVFs return a single result set that can be queried like a table.

## 🔍 Key Concepts

### What is a Table-Valued Function?

A **table-valued function (TVF)** is a user-defined function that returns a table (result set):

```sql
-- Inline TVF (better performance)
CREATE FUNCTION dbo.fn_GetActiveLoans(@MemberId INT)
RETURNS TABLE
AS
RETURN
(
    SELECT Id, BookId, BorrowedAt, DueDate
    FROM Loans
    WHERE MemberId = @MemberId AND Status = 0
);
```

**Key Characteristics:**
- **Returns a Table**: Not a single value, but a complete result set
- **Used in FROM Clause**: `SELECT * FROM dbo.fn_GetActiveLoans(123)`
- **Composable**: Can be joined, filtered, ordered like any table
- **Parameterized**: Accept input parameters (unlike views)

### TVF Types Comparison

| Type | Declaration | Performance | Use Case |
|------|-------------|-------------|----------|
| **Inline TVF** | `RETURNS TABLE AS RETURN (SELECT...)` | **Excellent** - query optimizer sees through it | Simple queries, filters, joins |
| **Multi-Statement TVF** | `RETURNS @TableVar TABLE (...) AS BEGIN ... END` | **Poor** - treated as black box | Complex logic, multiple steps, temp results |

### Inline vs. Multi-Statement TVFs

#### ✅ Inline TVF (Recommended)

```sql
CREATE FUNCTION dbo.fn_GetMemberLoans(@MemberId INT, @Status INT)
RETURNS TABLE
AS
RETURN
(
    SELECT
        L.Id,
        L.BorrowedAt,
        B.Title AS BookTitle,
        B.ISBN
    FROM Loans L
    INNER JOIN Books B ON L.BookId = B.Id
    WHERE L.MemberId = @MemberId
      AND (@Status IS NULL OR L.Status = @Status)
);
```

**Advantages:**
- SQL Server can inline the function into the calling query
- Query optimizer can generate better execution plans
- Can use indexes on underlying tables effectively
- No overhead from table variables

**Limitations:**
- Must be a single SELECT statement
- Cannot contain procedural logic (IF, WHILE, etc.)
- Cannot declare variables

#### ⚠️ Multi-Statement TVF (Use Sparingly)

```sql
CREATE FUNCTION dbo.fn_ComplexStats(@MemberId INT)
RETURNS @Results TABLE (
    StatName VARCHAR(50),
    StatValue INT
)
AS
BEGIN
    DECLARE @Count INT;

    -- Multiple statements
    SELECT @Count = COUNT(*) FROM Loans WHERE MemberId = @MemberId;
    INSERT INTO @Results VALUES ('TotalLoans', @Count);

    SELECT @Count = COUNT(*) FROM Loans WHERE MemberId = @MemberId AND Status = 2;
    INSERT INTO @Results VALUES ('OverdueLoans', @Count);

    RETURN;
END
```

**Drawbacks:**
- Treated as a black box by the query optimizer
- Cannot use indexes efficiently
- Table variables have limitations (no statistics, no parallelism)
- Slower than inline TVFs

**When to Use:**
- Complex procedural logic required
- Multiple intermediate steps
- Cannot express logic in a single query

**Performance Tip:** If possible, rewrite as an inline TVF. Most multi-statement TVFs can be rewritten as a single complex SELECT.

### Our Implementation: fn_GetMemberStatistics

In migration `V011__member_stats_tvf.sql`, we create an **inline TVF** to get comprehensive member statistics:

```sql
CREATE FUNCTION dbo.fn_GetMemberStatistics
(
    @MemberId INT
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        @MemberId AS MemberId,
        COUNT(L.Id) AS TotalBooksLoaned,
        SUM(CASE WHEN L.Status = 0 THEN 1 ELSE 0 END) AS ActiveLoans,
        SUM(CASE WHEN L.Status = 2 THEN 1 ELSE 0 END) AS OverdueLoans,
        SUM(CASE WHEN L.Status = 3 THEN 1 ELSE 0 END) AS ReturnedLateCount,
        ISNULL(SUM(L.LateFee), 0.00) AS TotalLateFees,
        ISNULL(SUM(CASE WHEN L.IsFeePaid = 0 THEN L.LateFee ELSE 0 END), 0.00) AS UnpaidLateFees,
        AVG(CASE WHEN L.ReturnedAt IS NOT NULL
            THEN DATEDIFF(DAY, L.BorrowedAt, L.ReturnedAt)
            ELSE NULL END) AS AvgLoanDurationDays,
        MAX(L.BorrowedAt) AS LastBorrowDate,
        ISNULL(SUM(L.RenewalCount), 0) AS TotalRenewals,
        SUM(CASE WHEN L.Status IN (4, 5) THEN 1 ELSE 0 END) AS LostOrDamagedCount
    FROM Members M
    LEFT JOIN Loans L ON M.Id = L.MemberId
    WHERE M.Id = @MemberId
    GROUP BY M.Id
);
```

**Design Decisions:**
- **Inline TVF**: Single SELECT for maximum performance
- **LEFT JOIN**: Returns results even if member has no loans (zeros instead of no rows)
- **Conditional Aggregation**: `SUM(CASE WHEN ... THEN 1 ELSE 0 END)` for counting specific statuses
- **ISNULL for Sums**: Ensures 0.00 instead of NULL for monetary values
- **AVG with CASE**: Only includes returned loans in average duration calculation
- **Comprehensive Metrics**: All statistics in one query instead of multiple round trips

### Querying TVFs from C#

TVFs are queried just like tables using standard `SELECT`:

```csharp
public async Task<MemberStatistics?> GetStatisticsAsync(
    int memberId,
    SqlTransaction transaction,
    CancellationToken cancellationToken = default)
{
    // Query the TVF just like a table
    const string sql = @"
        SELECT
            MemberId,
            TotalBooksLoaned,
            ActiveLoans,
            OverdueLoans,
            ReturnedLateCount,
            TotalLateFees,
            UnpaidLateFees,
            AvgLoanDurationDays,
            LastBorrowDate,
            TotalRenewals,
            LostOrDamagedCount
        FROM dbo.fn_GetMemberStatistics(@MemberId);";

    var connection = transaction.Connection;

    await using var command = new SqlCommand(sql, connection, transaction);
    command.Parameters.Add("@MemberId", SqlDbType.Int).Value = memberId;

    await using var reader = await command.ExecuteReaderAsync(cancellationToken);

    if (await reader.ReadAsync(cancellationToken))
    {
        return MapReaderToMemberStatistics(reader);
    }

    return null;
}
```

**Key Points:**
- **FROM clause**: Treat the TVF like a table
- **Parameters in Parentheses**: Pass parameters directly in the function call
- **SqlDataReader**: Read results like any query
- **No ExecuteScalar**: Use ExecuteReaderAsync, not ExecuteScalarAsync (TVF returns a table, not a single value)

### TVFs vs. Views

| Feature | View | TVF |
|---------|------|-----|
| **Parameters** | ❌ No | ✅ Yes |
| **Filtering** | Only via WHERE after calling | Parameters + WHERE |
| **Reusability** | Static result set | Dynamic based on params |
| **Performance** | Fast (inline) | Fast (inline TVF) |
| **Use Case** | Fixed queries | Parameterized queries |

**Example - View:**
```sql
-- Cannot filter by specific member, must apply WHERE later
CREATE VIEW vw_AllMemberStats AS
SELECT MemberId, COUNT(*) AS TotalLoans
FROM Loans
GROUP BY MemberId;

-- Use it
SELECT * FROM vw_AllMemberStats WHERE MemberId = 123;
```

**Example - TVF:**
```sql
-- Accepts parameter, only processes data for one member
CREATE FUNCTION fn_GetMemberStats(@MemberId INT)
RETURNS TABLE AS RETURN
(
    SELECT @MemberId AS MemberId, COUNT(*) AS TotalLoans
    FROM Loans
    WHERE MemberId = @MemberId
);

-- Use it
SELECT * FROM fn_GetMemberStats(123);
```

The TVF is more efficient because it filters before aggregating, reducing the data processed.

### TVFs vs. Stored Procedures

| Feature | TVF | Stored Procedure |
|---------|-----|------------------|
| **Usage** | In FROM clause | EXEC/EXECUTE |
| **Returns** | Single table | Multiple result sets, output params |
| **Composability** | ✅ Can JOIN, filter | ❌ Cannot compose |
| **Modifications** | ❌ Read-only | ✅ Can modify data |
| **Performance** | Fast (inline) | Depends on implementation |

**When to Use TVF:**
- Need to join results with other tables
- Want to apply additional WHERE/ORDER BY
- Read-only data retrieval
- Want to treat result as a table

**When to Use Stored Procedure:**
- Need multiple result sets
- Need OUTPUT parameters
- Modifying data (INSERT/UPDATE/DELETE)
- Complex procedural logic

## 📊 Advanced TVF Techniques

### Joining TVFs with Other Tables

```sql
-- Get member info with their statistics
SELECT
    M.MembershipNumber,
    M.FirstName + ' ' + M.LastName AS FullName,
    S.TotalBooksLoaned,
    S.OverdueLoans,
    S.UnpaidLateFees
FROM Members M
CROSS APPLY dbo.fn_GetMemberStatistics(M.Id) S
WHERE S.OverdueLoans > 0
ORDER BY S.UnpaidLateFees DESC;
```

**CROSS APPLY** is like INNER JOIN for TVFs - it calls the function for each row and joins the results.

**OUTER APPLY** is like LEFT JOIN - returns NULLs if TVF returns no rows.

### Filtering TVF Results

```sql
-- Only show statistics for members with high late fees
SELECT *
FROM dbo.fn_GetMemberStatistics(123)
WHERE UnpaidLateFees > 10.00;
```

### Using TVFs in Subqueries

```sql
-- Find members who have borrowed more than average
SELECT MemberId, TotalBooksLoaned
FROM dbo.fn_GetMemberStatistics(123)
WHERE TotalBooksLoaned > (
    SELECT AVG(TotalBooksLoaned)
    FROM vw_AllMemberStats
);
```

## ⚠️ Common Pitfalls

### ❌ Don't: Use Multi-Statement TVF When Inline Works

```sql
-- BAD: Multi-statement for simple query
CREATE FUNCTION fn_GetActiveLoans(@MemberId INT)
RETURNS @Loans TABLE (Id INT, BookTitle VARCHAR(200))
AS
BEGIN
    INSERT INTO @Loans
    SELECT L.Id, B.Title
    FROM Loans L
    JOIN Books B ON L.BookId = B.Id
    WHERE L.MemberId = @MemberId;

    RETURN;
END
```

### ✅ Do: Use Inline TVF

```sql
-- GOOD: Same logic as inline TVF
CREATE FUNCTION fn_GetActiveLoans(@MemberId INT)
RETURNS TABLE
AS
RETURN
(
    SELECT L.Id, B.Title AS BookTitle
    FROM Loans L
    JOIN Books B ON L.BookId = B.Id
    WHERE L.MemberId = @MemberId
);
```

**Why:** Inline TVF can use indexes, generate better execution plans, and run 10-100x faster.

### ❌ Don't: Select All Columns From TVF if You Don't Need Them

```sql
-- BAD: Requesting all statistics when only need one metric
SELECT TotalBooksLoaned
FROM dbo.fn_GetMemberStatistics(123);
```

**Impact:** SQL Server still computes all aggregations (SUM, AVG, MAX).

**Better:** If you frequently need only one metric, create a separate scalar function for that metric.

### ❌ Don't: Call TVF in a Loop (N+1 Problem)

```csharp
// BAD: Calling TVF once per member
foreach (var memberId in memberIds)
{
    var stats = await GetStatisticsAsync(memberId, tx);
    ProcessStats(stats);
}
```

### ✅ Do: Use CROSS APPLY for Bulk Operations

```sql
-- GOOD: Single query for all members
SELECT
    M.Id,
    S.TotalBooksLoaned,
    S.ActiveLoans
FROM Members M
CROSS APPLY dbo.fn_GetMemberStatistics(M.Id) S
WHERE M.Id IN (1, 2, 3, 4, 5);
```

## ✅ Best Practices

1. **Prefer Inline TVFs** unless you absolutely need procedural logic
2. **Use Meaningful Column Names** - return descriptive column names, not generic names
3. **Handle NULL Parameters** - use ISNULL/COALESCE for optional parameters
4. **Return Consistent Columns** - always return the same columns regardless of input
5. **Document Parameters** - use comments to explain what each parameter does
6. **Consider Indexes** - ensure underlying tables have appropriate indexes
7. **Test Performance** - compare execution plans with inline queries
8. **Use CROSS/OUTER APPLY** when joining with other tables

## 🧪 Testing This Feature

Our integration tests (`MemberStatisticsTests.cs`) verify:

1. **Zero State**: Member with no loans returns zeros, not nulls
2. **Active Loans**: Counts active loans correctly
3. **Overdue Loans**: Distinguishes between active and overdue
4. **Returned Loans**: Calculates average duration only for returned books
5. **Late Fees**: Sums total and unpaid fees correctly
6. **Complex History**: Aggregates diverse loan history accurately

Run tests with:
```bash
dotnet test --filter "FullyQualifiedName~MemberStatisticsTests"
```

## 🔗 Learn More

### Official Microsoft Documentation
- [CREATE FUNCTION (Transact-SQL)](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-function-transact-sql)
- [Inline Table-Valued Functions](https://learn.microsoft.com/en-us/sql/relational-databases/user-defined-functions/create-user-defined-functions-database-engine)
- [APPLY Operator](https://learn.microsoft.com/en-us/sql/t-sql/queries/from-transact-sql#using-apply)

### Performance & Best Practices
- [Inline vs. Multi-Statement TVFs Performance](https://www.sqlshack.com/sql-server-table-valued-functions-performance-comparison/)
- [When to Use TVFs](https://www.red-gate.com/simple-talk/databases/sql-server/t-sql-programming-sql-server/sql-server-user-defined-functions/)

### Related Concepts
- [Scalar Functions](./22-scalar-functions.md) - Return single values
- [Views](./05-database-schema.md#views) - Static result sets
- [Stored Procedures](./23-stored-procedures.md) - Procedural logic

## ❓ Discussion Questions

1. **When would you choose a TVF over a view?**
   - Consider scenarios where parameterization provides value.

2. **Why are inline TVFs faster than multi-statement TVFs?**
   - Think about how the query optimizer handles each type.

3. **Can you rewrite a view as an inline TVF? What would you gain?**
   - Consider flexibility vs. simplicity trade-offs.

4. **What are the trade-offs between TVFs and stored procedures?**
   - Think about composability, return types, and use cases.

5. **How would you optimize a slow multi-statement TVF?**
   - Can it be rewritten as inline? Are there indexing opportunities?

## 💡 Try It Yourself

### Exercise 1: Create a Simple TVF
Create a TVF that returns all books in a specific category:

```sql
CREATE FUNCTION dbo.fn_GetBooksByCategory(@CategoryId INT)
RETURNS TABLE
AS
RETURN
(
    -- Your implementation here
);
```

### Exercise 2: Join with TVF
Write a query that finds all members who have more than 3 active loans using the member statistics TVF.

### Exercise 3: Performance Comparison
Create both an inline and multi-statement version of a TVF. Compare their execution plans using `SET STATISTICS TIME ON` and `SET STATISTICS IO ON`.

### Exercise 4: CROSS APPLY
Write a query using CROSS APPLY to get statistics for all members who joined in the last year, ordered by total books loaned.

---

**Key Takeaway:** Inline TVFs combine the parameterization of functions with the performance of views, making them ideal for reusable, parameterized queries. Always prefer inline TVFs over multi-statement TVFs unless procedural logic is absolutely required.
