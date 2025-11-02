# 25 - PIVOT and UNPIVOT for Cross-Tab Reporting

## 📖 What You'll Learn

- PIVOT operator to transform rows into columns (cross-tab reports)
- UNPIVOT operator to normalize pivoted data back to rows
- Dynamic PIVOT for unknown column sets
- Aggregation with PIVOT
- Querying pivoted data from C#

## 🎯 Why This Matters

Cross-tabulation (pivot tables) is essential for business reporting:
- **Monthly Sales by Category**: Categories as columns, months as rows
- **Employee Performance**: Metrics as columns, employees as rows
- **Library Statistics**: Categories as columns, time periods as rows
- **Dashboard Reports**: Multi-dimensional data display

Traditional GROUP BY produces rows for each category-month combination. PIVOT reshapes data to show categories as separate columns, creating Excel-style pivot tables.

## 🔍 Key Concepts

### What is PIVOT?

**PIVOT** transforms unique values from one column into multiple columns, aggregating data in the process:

```sql
-- WITHOUT PIVOT: Multiple rows
YearMonth  | CategoryName | LoanCount
-----------|--------------|----------
2024-01    | Fiction      | 10
2024-01    | Technology   | 5
2024-01    | Science      | 3

-- WITH PIVOT: Single row with columns for each category
YearMonth  | Fiction | Technology | Science | TotalLoans
-----------|---------|------------|---------|------------
2024-01    | 10      | 5          | 3       | 18
```

### PIVOT Syntax

```sql
SELECT [columns], [pivoted_columns]
FROM
(
    -- Source query: Must have exactly the columns needed
    SELECT ColumnToAggregate, ColumnForRows, ColumnForColumns
    FROM SourceTable
) AS SourceData
PIVOT
(
    AggregateFunction(ColumnToAggregate)
    FOR ColumnForColumns IN ([Value1], [Value2], [Value3])
) AS PivotTable
```

**Key Components:**
1. **Source Query**: Provides the data to pivot (must be exactly the columns needed)
2. **Aggregate Function**: How to combine values (COUNT, SUM, AVG, MAX, MIN)
3. **FOR Column**: Which column's values become new columns
4. **IN Clause**: Explicit list of values to pivot (or dynamic SQL)
5. **Pivot Table Alias**: Required AS alias

### Our Implementation: Monthly Loan Statistics

Migration V015 creates `vw_MonthlyLoansByCategory`:

```sql
CREATE VIEW dbo.vw_MonthlyLoansByCategory
AS
SELECT
    [Year],
    [Month],
    YearMonth,
    [Fiction],
    [Non-Fiction],
    [Science],
    [History],
    [Technology],
    [Biography],
    [Children],
    TotalLoans
FROM
(
    -- Source: Get loans with category names
    SELECT
        YEAR(L.BorrowedAt) AS [Year],
        MONTH(L.BorrowedAt) AS [Month],
        FORMAT(L.BorrowedAt, 'yyyy-MM') AS YearMonth,
        C.Name AS CategoryName,
        L.Id AS LoanId
    FROM Loans L
    INNER JOIN Books B ON L.BookId = B.Id
    INNER JOIN Categories C ON B.CategoryId = C.Id
) AS SourceData
PIVOT
(
    COUNT(LoanId)
    FOR CategoryName IN (
        [Fiction],
        [Non-Fiction],
        [Science],
        [History],
        [Technology],
        [Biography],
        [Children]
    )
) AS PivotTable
```

**How it works:**
1. Source query gets Year, Month, YearMonth, CategoryName, LoanId for each loan
2. PIVOT counts LoanIds (one per loan) for each category
3. CategoryName values become column names
4. Result: One row per month with loan counts in category columns
5. CROSS APPLY adds TotalLoans as sum of all category columns

### What is UNPIVOT?

**UNPIVOT** is the reverse operation - it transforms columns back into rows:

```sql
-- PIVOTED DATA: Columns for each category
YearMonth  | Fiction | Technology | Science
-----------|---------|------------|--------
2024-01    | 10      | 5          | 3

-- UNPIVOTED DATA: Normalized rows
YearMonth  | CategoryName | LoanCount
-----------|--------------|----------
2024-01    | Fiction      | 10
2024-01    | Technology   | 5
2024-01    | Science      | 3
```

### UNPIVOT Syntax

```sql
SELECT [columns], [unpivoted_column], [value_column]
FROM
(
    -- Source query with columns to unpivot
    SELECT YearMonth, Fiction, Technology, Science
    FROM PivotedTable
) AS PivotedData
UNPIVOT
(
    ValueColumn FOR UnpivotColumn IN (Fiction, Technology, Science)
) AS UnpivotTable
```

### Our UNPIVOT Implementation

Migration V015 creates `vw_UnpivotedLoanStats`:

```sql
CREATE VIEW dbo.vw_UnpivotedLoanStats
AS
SELECT
    YearMonth,
    CategoryName,
    LoanCount
FROM
(
    SELECT
        YearMonth,
        [Fiction],
        [Non-Fiction],
        [Science],
        [History],
        [Technology],
        [Biography],
        [Children]
    FROM dbo.vw_MonthlyLoansByCategory
) AS PivotedData
UNPIVOT
(
    LoanCount FOR CategoryName IN (
        [Fiction],
        [Non-Fiction],
        [Science],
        [History],
        [Technology],
        [Biography],
        [Children]
    )
) AS UnpivotTable
WHERE LoanCount > 0;  -- Filter out zero-count categories
```

**Key Features:**
- Takes pivoted data from `vw_MonthlyLoansByCategory`
- Converts each category column back to a row
- Filters out categories with zero loans (NULL becomes 0 in PIVOT)
- Result is normalized month-category-count data

### Dynamic PIVOT

Static PIVOT requires hardcoded column names. For dynamic categories, use dynamic SQL:

```sql
DECLARE @columns NVARCHAR(MAX);
DECLARE @query NVARCHAR(MAX);

-- Build column list from data
SELECT @columns = STRING_AGG(QUOTENAME(Name), ', ')
FROM (SELECT DISTINCT Name FROM Categories) AS CategoryNames;

-- Build dynamic PIVOT query
SET @query = '
SELECT Year, Month, ' + @columns + '
FROM (SELECT Year, Month, CategoryName, LoanId FROM ...) AS Source
PIVOT (COUNT(LoanId) FOR CategoryName IN (' + @columns + ')) AS PivotTable';

-- Execute dynamic SQL
EXEC sp_executesql @query;
```

**When to use Dynamic PIVOT:**
- Category names change over time
- Column list not known at design time
- Report needs to adapt to data

**Drawbacks:**
- Cannot be used in views (requires dynamic SQL)
- More complex, harder to debug
- Potential SQL injection risk if not careful

## 🎯 Practical Use Cases

### 1. Monthly Dashboard Report

```csharp
var pivots = await reportRepository.GetMonthlyLoansPivotAsync(2024, tx);
foreach (var month in pivots)
{
    Console.WriteLine($"{month.YearMonth}:");
    foreach (var category in month.CategoryLoans)
    {
        Console.WriteLine($"  {category.Key}: {category.Value} loans");
    }
    Console.WriteLine($"  TOTAL: {month.TotalLoans}");
}
```

### 2. Category Performance Analysis

```sql
-- Which categories are growing?
SELECT
    YearMonth,
    Fiction,
    Technology,
    Technology - LAG(Technology, 1) OVER (ORDER BY YearMonth) AS TechGrowth
FROM dbo.vw_MonthlyLoansByCategory
ORDER BY YearMonth DESC;
```

### 3. Export to Excel Format

Pivoted data is perfect for Excel exports - categories as columns match spreadsheet layout.

### 4. Reverse Engineering: UNPIVOT

```sql
-- Need to join unpivoted data with other tables
SELECT
    U.YearMonth,
    U.CategoryName,
    U.LoanCount,
    C.Description AS CategoryDescription
FROM dbo.vw_UnpivotedLoanStats U
INNER JOIN Categories C ON U.CategoryName = C.Name
WHERE U.LoanCount > 10;
```

## ⚠️ Common Pitfalls

### ❌ Don't: Include Extra Columns in Source Query

```sql
-- WRONG: Extra columns cause incorrect grouping
SELECT *  -- Too many columns!
FROM (
    SELECT BookId, CategoryName, LoanId FROM Loans  -- BookId causes issues
) AS Source
PIVOT (COUNT(LoanId) FOR CategoryName IN ([Fiction], [Science])) AS P
```

Source query should have ONLY:
- Columns for rows (Year, Month)
- Column to aggregate (LoanId)
- Column to pivot (CategoryName)

### ❌ Don't: Forget to Handle NULLs

```sql
-- WRONG: NULL becomes 0 in PIVOT, but might be misleading
SELECT Fiction, Science FROM vw_MonthlyLoansByCategory

-- CORRECT: Explicitly handle NULLs
SELECT
    ISNULL(Fiction, 0) AS Fiction,
    ISNULL(Science, 0) AS Science
FROM vw_MonthlyLoansByCategory
```

### ❌ Don't: Use UNPIVOT on Heterogeneous Columns

```sql
-- BAD: Columns have different data types or meanings
UNPIVOT (Value FOR Metric IN (LoanCount, AvgRating, Description))
-- LoanCount is INT, AvgRating is DECIMAL, Description is VARCHAR - ERROR!
```

All unpivoted columns must have compatible types.

### ❌ Don't: Assume PIVOT Preserves All Rows

```sql
-- PIVOT aggregates - rows are combined
-- If YearMonth has no loans, it won't appear in output!
```

Use LEFT JOIN with calendar table to preserve all time periods.

## ✅ Best Practices

1. **Keep Source Query Minimal** - Only columns needed for PIVOT
2. **Use Static PIVOT in Views** - Faster, easier to query
3. **Use Dynamic PIVOT in Stored Procedures** - When flexibility needed
4. **Index Source Columns** - CategoryId, BorrowedAt for performance
5. **Document Column Meanings** - Clear names (Fiction, not C1)
6. **Filter UNPIVOT Results** - Remove zero/NULL values
7. **Consider Alternatives** - Sometimes CASE statements are clearer

## 🧪 Testing This Feature

Our tests (`PivotUnpivotTests.cs`) verify:
1. **PIVOT correctness** - Categories become columns with correct counts
2. **Empty data handling** - Returns empty result set
3. **Single category** - Only one column populated
4. **UNPIVOT normalization** - Columns converted back to rows
5. **Reversibility** - PIVOT → UNPIVOT returns original data

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~PivotUnpivotTests"
```

## 🔍 Querying from C#

### Querying PIVOT Results

The challenge with PIVOT is handling dynamic column names in C#:

```csharp
public async Task<List<MonthlyLoanPivot>> GetMonthlyLoansPivotAsync(
    int? year,
    SqlTransaction transaction,
    CancellationToken cancellationToken = default)
{
    var sql = @"
        SELECT [Year], [Month], YearMonth,
               [Fiction], [Non-Fiction], [Science], [History],
               [Technology], [Biography], [Children], TotalLoans
        FROM dbo.vw_MonthlyLoansByCategory
        WHERE (@Year IS NULL OR [Year] = @Year)
        ORDER BY [Year], [Month];";

    await using var command = new SqlCommand(sql, connection, transaction);
    command.Parameters.Add("@Year", SqlDbType.Int).Value =
        (object?)year ?? DBNull.Value;

    await using var reader = await command.ExecuteReaderAsync(cancellationToken);

    var pivots = new List<MonthlyLoanPivot>();
    while (await reader.ReadAsync(cancellationToken))
    {
        // Map category columns to Dictionary
        var categoryLoans = new Dictionary<string, int>
        {
            ["Fiction"] = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
            ["Non-Fiction"] = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
            ["Science"] = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
            // ... more categories
        };

        pivots.Add(MonthlyLoanPivot.FromDatabase(
            reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2),
            categoryLoans, reader.GetInt32(10)
        ));
    }

    return pivots;
}
```

**Key Techniques:**
- Use Dictionary<string, int> for dynamic category storage
- Map each pivoted column to dictionary entry
- Handle NULLs with IsDBNull checks

### Querying UNPIVOT Results

UNPIVOT results are straightforward - just normalized rows:

```csharp
public async Task<List<UnpivotedLoanStat>> GetUnpivotedLoanStatsAsync(
    SqlTransaction transaction,
    CancellationToken cancellationToken = default)
{
    const string sql = @"
        SELECT YearMonth, CategoryName, LoanCount
        FROM dbo.vw_UnpivotedLoanStats
        ORDER BY YearMonth, CategoryName;";

    await using var command = new SqlCommand(sql, connection, transaction);
    await using var reader = await command.ExecuteReaderAsync(cancellationToken);

    var stats = new List<UnpivotedLoanStat>();
    while (await reader.ReadAsync(cancellationToken))
    {
        stats.Add(UnpivotedLoanStat.FromDatabase(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetInt32(2)
        ));
    }

    return stats;
}
```

## 🔗 Learn More

- [PIVOT and UNPIVOT (SQL Server)](https://learn.microsoft.com/en-us/sql/t-sql/queries/from-using-pivot-and-unpivot)
- [Dynamic PIVOT Queries](https://www.sqlshack.com/dynamic-pivot-tables-in-sql-server/)
- [Cross Tabulation Reports](https://www.red-gate.com/simple-talk/databases/sql-server/learn/cross-tabulations-pivot-tables-sql-server/)

## ❓ Discussion Questions

1. When would you use PIVOT instead of multiple CASE statements?
2. How does PIVOT affect query performance compared to GROUP BY?
3. What are the limitations of PIVOT in views vs stored procedures?
4. When would UNPIVOT be necessary in real applications?

## 💡 Try It Yourself

### Exercise 1: Add New Category

Add a new category (e.g., "Mystery") and verify it appears in pivot results.

### Exercise 2: Yearly Pivot

Create a view that pivots loan counts by year instead of month, with months as columns.

### Exercise 3: Multiple Aggregates

Modify the PIVOT to show both COUNT and AVG loan duration per category.

### Exercise 4: Dynamic PIVOT Stored Procedure

Create a stored procedure that dynamically pivots any set of categories without hardcoding names.

---

**Key Takeaway:** PIVOT transforms rows into columns for cross-tab reports, while UNPIVOT normalizes pivoted data back to rows. Use static PIVOT in views for performance, dynamic PIVOT in stored procedures for flexibility.
