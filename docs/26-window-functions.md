# 23 - Window Functions for Analytics

## 📖 What You'll Learn

- Window functions (ROW_NUMBER, RANK, DENSE_RANK, LAG, LEAD)
- OVER clause with PARTITION BY and ORDER BY
- Moving averages and trend analysis
- When to use window functions vs GROUP BY

## 🎯 Why This Matters

Window functions enable powerful analytics:
- **Ranking**: Top N queries per category without subqueries
- **Trend Analysis**: Compare current vs previous/next periods
- **Running Calculations**: Moving averages, cumulative sums
- **Performance**: Single query instead of multiple round trips

Unlike GROUP BY, window functions **don't collapse rows** - you get calculations alongside raw data.

## 🔍 Key Concepts

### What Are Window Functions?

Window functions perform calculations across a set of rows (a "window") related to the current row:

```sql
SELECT
    Title,
    CategoryName,
    COUNT(*) AS LoanCount,
    -- Window function: rank within each category
    RANK() OVER (
        PARTITION BY CategoryId    -- Separate window per category
        ORDER BY COUNT(*) DESC      -- Order within window
    ) AS CategoryRank
FROM Books B
JOIN Loans L ON B.Id = L.BookId
GROUP BY B.Id, B.Title, B.CategoryId, CategoryName;
```

**Key Components:**
- `OVER` clause defines the window
- `PARTITION BY` creates separate windows (like GROUP BY, but doesn't collapse)
- `ORDER BY` determines calculation order within window
- Function operates on each window independently

### Ranking Functions Comparison

| Function | Ties Handling | Example Output | Use Case |
|----------|---------------|----------------|----------|
| **ROW_NUMBER()** | Unique numbers | 1, 2, 3, 4, 5 | Need unique IDs per group |
| **RANK()** | Same rank, gaps | 1, 2, 2, 4, 5 | Traditional competition ranking |
| **DENSE_RANK()** | Same rank, no gaps | 1, 2, 2, 3, 4 | Academic grading |

**Our Implementation - vw_PopularBooks:**

```sql
SELECT
    B.Title,
    C.Name AS CategoryName,
    COUNT(L.Id) AS TotalLoans,

    ROW_NUMBER() OVER (
        PARTITION BY C.Id
        ORDER BY COUNT(L.Id) DESC, B.Title ASC
    ) AS RowNumber,  -- 1,2,3,4... (unique even with ties)

    RANK() OVER (
        PARTITION BY C.Id
        ORDER BY COUNT(L.Id) DESC
    ) AS Rank,  -- 1,2,2,4... (gaps after ties)

    DENSE_RANK() OVER (
        PARTITION BY C.Id
        ORDER BY COUNT(L.Id) DESC
    ) AS DenseRank  -- 1,2,2,3... (no gaps)
FROM Books B
JOIN Categories C ON B.CategoryId = C.Id
LEFT JOIN Loans L ON B.Id = L.BookId
GROUP BY B.Id, B.Title, C.Id, C.Name;
```

### LAG and LEAD for Trend Analysis

`LAG` accesses previous row, `LEAD` accesses next row:

```sql
SELECT
    YearMonth,
    LoanCount,

    -- Previous month (offset=1, default NULL)
    LAG(LoanCount, 1) OVER (
        PARTITION BY CategoryId
        ORDER BY Year, Month
    ) AS PrevMonthLoans,

    -- Next month
    LEAD(LoanCount, 1) OVER (
        PARTITION BY CategoryId
        ORDER BY Year, Month
    ) AS NextMonthLoans,

    -- Calculate growth percentage
    CASE
        WHEN LAG(LoanCount, 1) OVER (...) > 0
        THEN ((LoanCount - LAG(LoanCount, 1) OVER (...)) * 100.0
              / LAG(LoanCount, 1) OVER (...))
        ELSE NULL
    END AS GrowthPercentage
FROM MonthlyLoans;
```

**Our Implementation - vw_MonthlyLoanTrends:**
- Tracks loans per category per month
- Uses LAG for month-over-month growth
- Uses LEAD to preview next month
- Calculates 3-month moving average

### Moving Averages

Calculate rolling statistics with `ROWS BETWEEN`:

```sql
AVG(LoanCount) OVER (
    PARTITION BY CategoryId
    ORDER BY Year, Month
    ROWS BETWEEN 2 PRECEDING AND CURRENT ROW  -- Last 3 months
) AS ThreeMonthMovingAvg
```

**Frame specifications:**
- `ROWS BETWEEN 2 PRECEDING AND CURRENT ROW` - Last 3 rows
- `ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW` - From start to current
- `ROWS BETWEEN 1 PRECEDING AND 1 FOLLOWING` - Previous, current, next

## 📊 Querying Views from C#

Window function views are queried like any table:

```csharp
public async Task<List<PopularBook>> GetPopularBooksAsync(
    int? topN,
    int? categoryId,
    SqlTransaction transaction,
    CancellationToken cancellationToken = default)
{
    var sql = @"
        SELECT
            BookId, Title, CategoryName, TotalLoans,
            RowNumber, Rank, DenseRank, GlobalRowNumber
        FROM dbo.vw_PopularBooks
        WHERE 1=1";

    if (topN.HasValue)
        sql += " AND RowNumber <= @TopN";  // Filter by rank

    if (categoryId.HasValue)
        sql += " AND CategoryId = @CategoryId";

    sql += " ORDER BY CategoryName, RowNumber;";

    // Execute query, map to DTOs...
}
```

## 🎯 Practical Use Cases

### 1. Top N Per Group Without Subqueries

❌ **Before (Slow):**
```sql
SELECT * FROM Books B
WHERE B.Id IN (
    SELECT TOP 5 Id FROM Books
    WHERE CategoryId = B.CategoryId
    ORDER BY (SELECT COUNT(*) FROM Loans WHERE BookId = Id) DESC
);
```

✅ **After (Fast):**
```sql
SELECT * FROM vw_PopularBooks
WHERE RowNumber <= 5;
```

### 2. Trend Detection

```sql
-- Find categories with strong growth (>20%)
SELECT DISTINCT CategoryName
FROM vw_MonthlyLoanTrends
WHERE GrowthPercentage > 20
  AND Year = 2024 AND Month = 10;
```

### 3. Outlier Detection

```sql
-- Books performing above trend
SELECT *
FROM vw_MonthlyLoanTrends
WHERE LoanCount > ThreeMonthMovingAvg * 1.5;
```

## ⚠️ Common Pitfalls

### ❌ Don't: Reuse Window Definitions

```sql
-- BAD: Repeating same OVER clause
SELECT
    Title,
    RANK() OVER (PARTITION BY CategoryId ORDER BY TotalLoans DESC),
    DENSE_RANK() OVER (PARTITION BY CategoryId ORDER BY TotalLoans DESC),
    ROW_NUMBER() OVER (PARTITION BY CategoryId ORDER BY TotalLoans DESC)
...
```

### ✅ Do: Use WINDOW Clause (SQL Server 2022+)

```sql
-- GOOD: Define window once
SELECT
    Title,
    RANK() OVER w,
    DENSE_RANK() OVER w,
    ROW_NUMBER() OVER w
FROM Books
WINDOW w AS (PARTITION BY CategoryId ORDER BY TotalLoans DESC);
```

### ❌ Don't: Use Window Functions in WHERE

```sql
-- ERROR: Can't use window functions in WHERE
SELECT * FROM Books
WHERE ROW_NUMBER() OVER (ORDER BY Title) <= 10;
```

### ✅ Do: Use Subquery or CTE

```sql
-- CORRECT: Wrap in CTE
WITH Ranked AS (
    SELECT *, ROW_NUMBER() OVER (ORDER BY Title) AS rn
    FROM Books
)
SELECT * FROM Ranked WHERE rn <= 10;
```

## ✅ Best Practices

1. **Create Views** - Encapsulate complex window logic in views
2. **Index Wisely** - Index PARTITION BY and ORDER BY columns
3. **Limit Results** - Use WHERE on rank columns to filter early
4. **Use CTEs** - Break complex queries into readable steps
5. **Test Performance** - Compare execution plans vs alternatives

## 🧪 Testing This Feature

Our tests (`WindowFunctionTests.cs`) verify:
1. **ROW_NUMBER uniqueness** - Even with ties
2. **RANK gap behavior** - Skips after ties
3. **DENSE_RANK continuity** - No gaps
4. **LAG/LEAD correctness** - Accesses correct rows
5. **Growth calculations** - Month-over-month percentages
6. **Moving averages** - 3-month rolling calculations

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~WindowFunctionTests"
```

## 🔗 Learn More

- [Window Functions (SQL Server)](https://learn.microsoft.com/en-us/sql/t-sql/queries/select-over-clause-transact-sql)
- [LAG](https://learn.microsoft.com/en-us/sql/t-sql/functions/lag-transact-sql) / [LEAD](https://learn.microsoft.com/en-us/sql/t-sql/functions/lead-transact-sql)
- [Ranking Functions](https://learn.microsoft.com/en-us/sql/t-sql/functions/ranking-functions-transact-sql)

## ❓ Discussion Questions

1. When would you choose `RANK()` vs `DENSE_RANK()`?
2. How do window functions differ from `GROUP BY`?
3. What are the performance implications of `PARTITION BY`?
4. When would you use `LAG/LEAD` instead of self-joins?

## 💡 Try It Yourself

### Exercise 1: Find Top 3 Books per Category
Write a query using `vw_PopularBooks`.

### Exercise 2: Calculate Quarter-over-Quarter Growth
Modify `vw_MonthlyLoanTrends` to compare quarters instead of months.

### Exercise 3: Running Total
Create a view with cumulative loan counts using `SUM() OVER`.

---

**Key Takeaway:** Window functions provide analytical power without collapsing rows, enabling complex rankings and trend analysis in single queries.
