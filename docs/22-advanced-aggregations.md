# 27 - Advanced Aggregations (GROUPING SETS, ROLLUP, CUBE)

## 📖 What You'll Learn

- `GROUPING SETS` - Multiple aggregation levels in a single query
- `ROLLUP` - Hierarchical aggregation with subtotals and grand totals
- `CUBE` - All possible grouping combinations (2^n)
- `GROUPING()` function - Identifies aggregated columns
- `GROUPING_ID()` function - Combined grouping indicator
- Multi-dimensional reporting techniques

## 🎯 Why This Matters

Business intelligence and reporting require aggregating data at multiple levels:
- **GROUPING SETS**: Efficiently compute multiple GROUP BY operations in one query
- **ROLLUP**: Create hierarchical reports with subtotals (e.g., Category → Year → Month → Grand Total)
- **CUBE**: Analyze all dimensional combinations (e.g., by Category, by Status, by Category+Status, etc.)

These features eliminate the need for multiple queries or UNION ALL, improving both performance and maintainability.

## 🔍 Key Concepts

### Comparison of GROUP BY Extensions

| Feature | Purpose | Combinations | Use Case |
|---------|---------|--------------|----------|
| **GROUPING SETS** | Specific aggregation levels | Custom selection | Dashboard with specific metrics |
| **ROLLUP** | Hierarchical subtotals | (A,B,C) → (A,B) → (A) → () | Regional sales → Country → Total |
| **CUBE** | All combinations | 2^n combinations | Cross-dimensional analysis |

### 1. GROUPING SETS

**Purpose:** Define specific grouping levels in a single query without UNION ALL.

**Syntax:**
```sql
SELECT Category, Year, COUNT(*) AS Total
FROM Sales
GROUP BY GROUPING SETS (
    (Category),           -- Category level
    (Year),              -- Year level
    (Category, Year),    -- Detail level
    ()                   -- Grand total
);
```

**Advantages:**
- More efficient than UNION ALL of separate GROUP BY queries
- Single scan of data
- Can specify exactly which groupings you need

### 2. ROLLUP

**Purpose:** Create hierarchical aggregations from right to left.

**Syntax:**
```sql
GROUP BY ROLLUP (Category, Year, Month)
```

**Generates:**
1. `(Category, Year, Month)` - Detail level
2. `(Category, Year)` - Year subtotals
3. `(Category)` - Category subtotals
4. `()` - Grand total

**Use Case:** Perfect for hierarchical reports where you need subtotals at each level.

### 3. CUBE

**Purpose:** Generate all possible grouping combinations.

**Syntax:**
```sql
GROUP BY CUBE (Category, Year, Status)
```

**Generates:** 2^3 = 8 combinations:
- `(Category, Year, Status)` - Full detail
- `(Category, Year)`, `(Category, Status)`, `(Year, Status)` - 2-way combinations
- `(Category)`, `(Year)`, `(Status)` - 1-way combinations
- `()` - Grand total

**Use Case:** OLAP-style analysis where you need to slice and dice across all dimensions.

### 4. GROUPING() and GROUPING_ID()

**GROUPING(column):** Returns 1 if the column is aggregated (NULL), 0 otherwise.

```sql
SELECT
    Category,
    GROUPING(Category) AS IsCategoryAggregated,
    COUNT(*) AS Total
FROM Sales
GROUP BY ROLLUP(Category);
```

**GROUPING_ID(col1, col2, ...):** Combines GROUPING() values into a single integer.

```sql
GROUPING_ID(Category, Year) = 0  -- 00 binary: Detail level
GROUPING_ID(Category, Year) = 1  -- 01 binary: Category subtotal
GROUPING_ID(Category, Year) = 3  -- 11 binary: Grand total
```

## 🎯 Our Implementation: Library Dashboard

Migration V017 creates four stored procedures demonstrating each technique:

### sp_GetLibraryStatsGroupingSets

```sql
GROUP BY GROUPING SETS (
    (C.Name),                                          -- Category only
    (YEAR(L.BorrowedAt), MONTH(L.BorrowedAt)),        -- Time only
    (C.Name, YEAR(L.BorrowedAt), MONTH(L.BorrowedAt)), -- Detail
    ()                                                 -- Grand total
)
```

**Returns:**
- Total loans by category (all time)
- Total loans by month (all categories)
- Total loans by category and month (detail)
- Overall grand total

**Key Feature:** GROUPING() indicators show which dimensions are aggregated.

### sp_GetLibraryStatsRollup

```sql
GROUP BY ROLLUP (C.Name, YEAR(L.BorrowedAt), MONTH(L.BorrowedAt))
```

**Returns hierarchy:**
1. Category-Year-Month (detail)
2. Category-Year (year subtotals)
3. Category (category subtotals)
4. Grand Total

**Key Feature:** GROUPING_ID() provides a numeric indicator:
- `0` = Detail
- `1` = Category-Year subtotal
- `3` = Category subtotal
- `7` = Grand total

### sp_GetLibraryStatsCube

```sql
GROUP BY CUBE (C.Name, YEAR(L.BorrowedAt), L.Status)
```

**Returns 8 combinations:**
- All categories, all years, all statuses
- By category only, by year only, by status only
- By category+year, category+status, year+status
- Grand total

**Use Case:** Interactive dashboards where users can pivot across any dimension.

### sp_GetDashboardSummary

```sql
GROUP BY ROLLUP (C.Name, YEAR(L.BorrowedAt))
```

**Returns:**
- Loan statistics by category and year
- Category subtotals across all years
- Grand total across all categories and years

**Includes:** Active, returned, and overdue loan breakdowns for each level.

## ⚠️ Common Pitfalls

### ❌ Don't: Confuse NULL grouping columns with actual NULL data

```sql
-- BAD: Can't distinguish NULL from aggregation
SELECT Category, COUNT(*)
FROM Sales
GROUP BY ROLLUP(Category);
```

**Why:** If `Category` is actually NULL in data, you can't tell if the row is a grand total or a NULL category.

**Fix:** Use GROUPING() to identify aggregated rows:

```sql
-- GOOD: Clear distinction
SELECT
    CASE WHEN GROUPING(Category) = 1
         THEN '(All Categories)'
         ELSE ISNULL(Category, '(Unknown)')
    END AS Category,
    COUNT(*)
FROM Sales
GROUP BY ROLLUP(Category);
```

### ❌ Don't: Use CUBE with high-cardinality columns

```sql
-- BAD: Creates millions of rows
GROUP BY CUBE (CustomerId, ProductId, StoreId, Date)
-- 2^4 = 16 combinations × (many customers × many products × many stores × many dates)
```

**Why:** CUBE generates 2^n combinations. With many unique values per column, this explodes.

**Fix:** Use GROUPING SETS to specify only needed combinations.

### ❌ Don't: Forget to filter on GROUPING_ID for specific aggregation levels

```sql
-- BAD: Returns all levels mixed together
SELECT Category, Year, COUNT(*)
FROM Sales
GROUP BY ROLLUP(Category, Year);
-- Hard to separate grand total from subtotals in application
```

**Fix:** Filter in WHERE or use GROUPING_ID() in application logic:

```sql
-- GOOD: Get only grand total
SELECT Category, Year, COUNT(*)
FROM Sales
GROUP BY ROLLUP(Category, Year)
HAVING GROUPING_ID(Category, Year) = 3;  -- Grand total only
```

## ✅ Best Practices

### 1. Use GROUPING() for Display Logic

```sql
-- GOOD: Human-readable aggregation labels
SELECT
    CASE WHEN GROUPING(Category) = 1 THEN '(ALL)' ELSE Category END AS Category,
    CASE WHEN GROUPING(Year) = 1 THEN '(ALL)' ELSE CAST(Year AS VARCHAR) END AS Year,
    COUNT(*) AS TotalSales
FROM Sales
GROUP BY ROLLUP(Category, Year);
```

### 2. Use GROUPING_ID() for Programmatic Level Detection

```sql
-- GOOD: Identify aggregation level
SELECT
    Category,
    Year,
    COUNT(*) AS Total,
    GROUPING_ID(Category, Year) AS LevelId,
    CASE GROUPING_ID(Category, Year)
        WHEN 0 THEN 'Detail'
        WHEN 1 THEN 'Category Total'
        WHEN 2 THEN 'Year Total'
        WHEN 3 THEN 'Grand Total'
    END AS LevelName
FROM Sales
GROUP BY ROLLUP(Category, Year);
```

### 3. Choose the Right Extension

```sql
-- GROUPING SETS: When you need specific combinations
GROUP BY GROUPING SETS ((Category), (Year), (Category, Year))

-- ROLLUP: When you have a natural hierarchy
GROUP BY ROLLUP (Region, Country, State, City)

-- CUBE: When analyzing all dimensional relationships
GROUP BY CUBE (ProductCategory, CustomerSegment, SalesChannel)
```

### 4. Optimize with Partial Aggregations

```sql
-- GOOD: ROLLUP on subset of columns
SELECT Region, Country, Year, SUM(Sales)
FROM Sales
GROUP BY Region, Country, ROLLUP(Year);
-- Creates: (Region, Country, Year) and (Region, Country)
-- But NOT (Region) or ()
```

## 🧪 Testing This Feature

Our tests (`AdvancedAggregationsTests.cs`) verify:
1. **GROUPING SETS** returns all specified aggregation levels
2. **GROUPING()** indicators correctly identify aggregated columns
3. **ROLLUP** creates hierarchical subtotals
4. **GROUPING_ID()** values match expected levels
5. **CUBE** generates 2^n combinations
6. **Dashboard Summary** calculates percentages correctly
7. All methods return consistent data across aggregation levels

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~AdvancedAggregationsTests"
```

## 🔍 C# Implementation

```csharp
// Execute stored procedure and map results
var results = await reportRepository.GetLibraryStatsGroupingSetsAsync(tx);

// Identify aggregation level using GROUPING() indicators
foreach (var result in results)
{
    if (result.IsGrandTotal)
        Console.WriteLine($"GRAND TOTAL: {result.TotalLoans} loans");
    else if (result.IsCategoryAggregated == 0 && result.IsYearAggregated == 1)
        Console.WriteLine($"Category '{result.CategoryName}': {result.TotalLoans} loans (all time)");
    else if (result.IsDetail)
        Console.WriteLine($"{result.CategoryName} / {result.LoanYear}-{result.LoanMonth:D2}: {result.TotalLoans} loans");
}
```

**Key Pattern:** Use computed properties (`IsGrandTotal`, `IsDetail`) based on GROUPING() values for clean business logic.

## 🔗 Learn More

- [GROUP BY Extensions (Microsoft Docs)](https://learn.microsoft.com/en-us/sql/t-sql/queries/select-group-by-transact-sql)
- [GROUPING SETS](https://learn.microsoft.com/en-us/sql/t-sql/queries/select-group-by-transact-sql#grouping-sets--)
- [ROLLUP and CUBE](https://learn.microsoft.com/en-us/sql/t-sql/queries/select-group-by-transact-sql#rollup)
- [GROUPING() Function](https://learn.microsoft.com/en-us/sql/t-sql/functions/grouping-transact-sql)

## ❓ Discussion Questions

1. When would you use GROUPING SETS instead of ROLLUP?
2. How does CUBE's exponential growth (2^n) affect query performance?
3. Why is GROUPING() necessary when working with aggregations?
4. How would you implement a drill-down interface using GROUPING_ID()?

## 💡 Try It Yourself

### Exercise 1: Add CUBE Analysis

Create a new stored procedure using CUBE to analyze loans by:
- Category
- Member type (inferred from membership date)
- Loan duration bucket (< 7 days, 7-14 days, > 14 days)

### Exercise 2: Custom GROUPING SETS

Modify `sp_GetLibraryStatsGroupingSets` to include only these groupings:
- By category and status
- By year
- Grand total

### Exercise 3: Performance Comparison

Compare execution time and I/O:
1. Three separate GROUP BY queries with UNION ALL
2. Single query with GROUPING SETS
3. Single query with CUBE (filtered to same levels)

Which is faster? Why?

### Exercise 4: Interactive Dashboard

Build a simple console menu that:
- Displays grand total
- Allows drilling down by category
- Allows drilling down by year within category
- Uses GROUPING_ID() to identify current level

---

**Key Takeaway:** GROUPING SETS, ROLLUP, and CUBE provide powerful multi-dimensional aggregation capabilities. Use GROUPING() and GROUPING_ID() to distinguish aggregated NULL values from actual data, enabling clean business logic and interactive reporting interfaces.
