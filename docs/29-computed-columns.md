# 29 - Computed Columns

## 📖 What You'll Learn

- Creating computed columns to derive values from existing columns
- Difference between persisted and non-persisted computed columns
- Indexing persisted computed columns for performance
- Using computed columns in queries (WHERE, ORDER BY, GROUP BY)
- Deterministic vs non-deterministic functions in computed columns
- Best practices for computed columns in database design

## 🎯 Why This Matters

Computed columns eliminate redundancy and ensure data consistency:

- **No Redundant Storage**: Values derived automatically from source data
- **Always Consistent**: Impossible to have mismatched computed values
- **Eliminates Application Logic**: Database handles calculations, not every app
- **Performance**: Persisted computed columns can be indexed like regular columns
- **Maintainability**: Change calculation once in database, not in multiple apps

**Real-World Use Cases:**
- Full names from first/last names
- Age from date of birth
- Tax amounts from subtotals
- Status flags from dates/states
- Formatted codes from multiple fields

## 🔍 Key Concepts

### Computed Columns Comparison

| Type | Stored on Disk | Can Be Indexed | Recalculated | Use Case |
|------|----------------|----------------|--------------|----------|
| **Non-Persisted** | No | No | On every query | Frequently changing values (age, days overdue) |
| **Persisted** | Yes | Yes | On UPDATE of source columns | Static derived values needing indexes (full name, decade) |

### 1. Non-Persisted Computed Columns

**Definition:** Calculated on-the-fly when queried, not stored on disk.

**Syntax:**
```sql
ALTER TABLE TableName
ADD ColumnName AS (expression);
```

**Example - Age from Date of Birth:**
```sql
ALTER TABLE Members
ADD Age AS DATEDIFF(YEAR, DateOfBirth, GETDATE());
```

**Characteristics:**
- ✅ No storage space required
- ✅ Always up-to-date (recalculated on every query)
- ❌ Cannot be indexed
- ❌ Slight performance cost on queries

**When to Use:**
- Values that change frequently (age, time-based calculations)
- Simple calculations with minimal performance impact
- When storage is a concern

### 2. Persisted Computed Columns

**Definition:** Calculated when source columns change, stored physically on disk.

**Syntax:**
```sql
ALTER TABLE TableName
ADD ColumnName AS (expression) PERSISTED;
```

**Example - Full Name:**
```sql
ALTER TABLE Authors
ADD FullName AS (FirstName + ' ' + LastName) PERSISTED;
```

**Characteristics:**
- ✅ Can be indexed for fast lookups
- ✅ No query-time calculation overhead
- ❌ Requires storage space
- ❌ Updates when source columns change (slight write overhead)

**When to Use:**
- Values used frequently in WHERE clauses or JOINs
- Complex calculations you want to index
- Values that don't change often

### 3. Indexing Computed Columns

**Only persisted columns can be indexed:**

```sql
-- Create persisted computed column
ALTER TABLE Authors
ADD FullName AS (FirstName + ' ' + LastName) PERSISTED;

-- Create index on it
CREATE NONCLUSTERED INDEX IX_Authors_FullName
ON Authors(FullName);
```

**Benefits:**
- Fast searches: `WHERE FullName LIKE 'John%'`
- Efficient sorting: `ORDER BY FullName`
- Join optimization: `JOIN ON a.FullName = b.FullName`

**Example Query Performance:**
```sql
-- WITHOUT INDEX: Table scan through all authors
SELECT * FROM Authors WHERE FirstName + ' ' + LastName = 'Isaac Asimov';

-- WITH INDEX on persisted FullName: Index seek
SELECT * FROM Authors WHERE FullName = 'Isaac Asimov';  -- Much faster!
```

### 4. Deterministic vs Non-Deterministic Functions

**Deterministic Functions** (allowed in persisted computed columns):
- Always return same result for same inputs
- Examples: `YEAR()`, `DATEDIFF()`, `SUBSTRING()`, `LOWER()`, `+`, `-`, `*`, `/`

**Non-Deterministic Functions** (NOT allowed in persisted columns):
- Can return different results for same inputs
- Examples: `GETDATE()`, `NEWID()`, `RAND()`, `CURRENT_TIMESTAMP`

**Example:**

```sql
-- ✅ ALLOWED (deterministic - always same year for same date)
ALTER TABLE Books
ADD YearPublished AS YEAR(PublishedDate) PERSISTED;

-- ❌ ERROR (non-deterministic - age changes over time)
ALTER TABLE Members
ADD Age AS DATEDIFF(YEAR, DateOfBirth, GETDATE()) PERSISTED;
-- Error: Cannot create persisted computed column because GETDATE() is non-deterministic

-- ✅ SOLUTION: Use non-persisted
ALTER TABLE Members
ADD Age AS DATEDIFF(YEAR, DateOfBirth, GETDATE());  -- No PERSISTED keyword
```

## 🎯 Our Implementation: Practical Computed Columns

Migration V019 adds computed columns across multiple tables:

### 1. Authors.FullName (Persisted + Indexed)

```sql
ALTER TABLE Authors
ADD FullName AS (FirstName + ' ' + LastName) PERSISTED;

CREATE NONCLUSTERED INDEX IX_Authors_FullName
ON Authors(FullName);
```

**Benefits:**
- Fast author searches by full name
- Consistent formatting across all apps
- No need to concatenate in application code

**Usage:**
```sql
-- Search by full name (uses index)
SELECT * FROM Authors
WHERE FullName LIKE 'Isaac%';

-- Sort by full name
SELECT FullName, Email
FROM Authors
ORDER BY FullName;
```

### 2. Books.YearPublished (Non-Persisted)

```sql
ALTER TABLE Books
ADD YearPublished AS YEAR(PublishedDate);
```

**Benefits:**
- Easy filtering/grouping by year
- No need to extract year in queries

**Usage:**
```sql
-- Books published in 1950s
SELECT Title, YearPublished
FROM Books
WHERE YearPublished BETWEEN 1950 AND 1959;

-- Group by year
SELECT YearPublished, COUNT(*) AS BookCount
FROM Books
GROUP BY YearPublished;
```

### 3. Books.PublishedDecade (Persisted + Indexed)

```sql
ALTER TABLE Books
ADD PublishedDecade AS (
    CASE
        WHEN PublishedDate IS NULL THEN NULL
        ELSE (YEAR(PublishedDate) / 10) * 10
    END
) PERSISTED;

CREATE NONCLUSTERED INDEX IX_Books_PublishedDecade
ON Books(PublishedDecade);
```

**Benefits:**
- Fast grouping by decade for reports
- Indexed for efficient filtering

**Usage:**
```sql
-- Books per decade
SELECT PublishedDecade, COUNT(*) AS Count
FROM Books
WHERE PublishedDecade IS NOT NULL
GROUP BY PublishedDecade
ORDER BY PublishedDecade DESC;
```

### 4. Members.Age (Non-Persisted)

```sql
ALTER TABLE Members
ADD Age AS DATEDIFF(YEAR, DateOfBirth, GETDATE());
```

**Benefits:**
- Always current age (updates automatically)
- Simple age-based queries

**Usage:**
```sql
-- Members over 18
SELECT FirstName, LastName, Age
FROM Members
WHERE Age >= 18;

-- Age distribution
SELECT
    CASE
        WHEN Age < 18 THEN 'Under 18'
        WHEN Age < 65 THEN 'Adult'
        ELSE 'Senior'
    END AS AgeGroup,
    COUNT(*) AS Count
FROM Members
GROUP BY CASE WHEN Age < 18 THEN 'Under 18' WHEN Age < 65 THEN 'Adult' ELSE 'Senior' END;
```

### 5. Loans.DaysOverdue (Non-Persisted)

```sql
ALTER TABLE Loans
ADD DaysOverdue AS (
    CASE
        WHEN ReturnedAt IS NULL AND GETDATE() > DueDate
        THEN DATEDIFF(DAY, DueDate, GETDATE())
        ELSE 0
    END
);
```

**Benefits:**
- Automatically calculates overdue days
- Always current (updates every day)
- No application logic needed

**Usage:**
```sql
-- Find overdue loans
SELECT
    l.Id,
    b.Title,
    m.FirstName + ' ' + m.LastName AS Member,
    l.DueDate,
    l.DaysOverdue
FROM Loans l
INNER JOIN Books b ON l.BookId = b.Id
INNER JOIN Members m ON l.MemberId = m.Id
WHERE l.DaysOverdue > 0
ORDER BY l.DaysOverdue DESC;
```

## ⚠️ Common Pitfalls

### ❌ Don't: Try to persist non-deterministic functions

```sql
-- BAD: GETDATE() is non-deterministic
ALTER TABLE Loans
ADD CurrentStatus AS (
    CASE WHEN GETDATE() > DueDate THEN 'Overdue' ELSE 'Active' END
) PERSISTED;
-- Error: Cannot persist non-deterministic computed column
```

**Fix:** Remove PERSISTED keyword:

```sql
-- GOOD: Non-persisted allows GETDATE()
ALTER TABLE Loans
ADD CurrentStatus AS (
    CASE WHEN GETDATE() > DueDate THEN 'Overdue' ELSE 'Active' END
);
```

### ❌ Don't: Index non-persisted columns

```sql
-- BAD: Cannot index non-persisted column
ALTER TABLE Members
ADD Age AS DATEDIFF(YEAR, DateOfBirth, GETDATE());

CREATE INDEX IX_Members_Age ON Members(Age);
-- Error: Cannot create index on non-persisted computed column
```

**Fix:** Make it persisted first (if deterministic):

```sql
-- Not possible for Age (uses GETDATE())
-- Solution: Create a persisted column with a fixed date or use a different approach
```

### ❌ Don't: Create circular dependencies

```sql
-- BAD: Column depends on itself
ALTER TABLE Products
ADD DiscountedPrice AS (Price - Discount) PERSISTED;

ALTER TABLE Products
ADD Discount AS (DiscountedPrice * 0.1) PERSISTED;
-- Error: Circular dependency
```

**Fix:** Ensure dependency flows one direction only.

## ✅ Best Practices

### 1. Choose Persisted vs Non-Persisted Wisely

```sql
-- GOOD: Persisted for static derived values
ALTER TABLE Orders
ADD TaxAmount AS (Subtotal * TaxRate) PERSISTED;

-- GOOD: Non-persisted for time-sensitive values
ALTER TABLE Subscriptions
ADD DaysRemaining AS DATEDIFF(DAY, GETDATE(), ExpiryDate);
```

### 2. Index Persisted Columns Used in WHERE Clauses

```sql
-- Create persisted computed column
ALTER TABLE Products
ADD SearchCode AS (UPPER(Category + '-' + CAST(ProductId AS VARCHAR))) PERSISTED;

-- Index it for fast searches
CREATE INDEX IX_Products_SearchCode ON Products(SearchCode);

-- Fast queries
SELECT * FROM Products WHERE SearchCode = 'ELECTRONICS-12345';
```

### 3. Handle NULLs Explicitly

```sql
-- GOOD: Explicit NULL handling
ALTER TABLE Books
ADD PublishedDecade AS (
    CASE
        WHEN PublishedDate IS NULL THEN NULL
        ELSE (YEAR(PublishedDate) / 10) * 10
    END
) PERSISTED;

-- BAD: Might produce unexpected results
ALTER TABLE Books
ADD PublishedDecade AS ((YEAR(PublishedDate) / 10) * 10) PERSISTED;
-- Returns NULL if PublishedDate is NULL (might be okay, but explicit is better)
```

### 4. Keep Expressions Simple

```sql
-- GOOD: Simple, readable
ALTER TABLE Authors
ADD FullName AS (FirstName + ' ' + LastName) PERSISTED;

-- BAD: Too complex (consider a view or function instead)
ALTER TABLE Orders
ADD ComplexCalculation AS (
    CASE
        WHEN (Subtotal > 100 AND CustomerType = 'Premium' AND YEAR(OrderDate) = YEAR(GETDATE()))
        THEN Subtotal * 0.9
        WHEN (Subtotal > 50 AND CustomerType = 'Standard')
        THEN Subtotal * 0.95
        ELSE Subtotal
    END
) PERSISTED;  -- Consider a function or application logic instead
```

## 🧪 Testing This Feature

Our tests (`ComputedColumnsTests.cs`) verify:
1. ✅ Computed values calculate correctly from source columns
2. ✅ Persisted computed columns can be indexed
3. ✅ Computed columns can be used in WHERE, ORDER BY, GROUP BY
4. ✅ Non-persisted columns recalculate automatically
5. ✅ Persisted columns update when source columns change
6. ✅ NULL handling works correctly
7. ✅ Time-based computations (age, days overdue) work as expected

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~ComputedColumnsTests"
```

## 🔍 C# Access Pattern

Computed columns appear as regular columns in queries:

```csharp
// Query includes computed columns
const string sql = @"
    SELECT
        Id,
        FirstName,
        LastName,
        FullName,      -- Computed column
        DateOfBirth,
        Age            -- Computed column
    FROM Members
    WHERE Age >= @MinAge
    ORDER BY FullName;";

await using var command = new SqlCommand(sql, connection, transaction);
command.Parameters.AddWithValue("@MinAge", 18);

await using var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    var fullName = reader.GetString(3);  // Computed FullName
    var age = reader.GetInt32(5);        // Computed Age

    Console.WriteLine($"{fullName}, Age: {age}");
}
```

**Key Point:** From the C# perspective, computed columns look exactly like regular columns - the computation is transparent.

## 🔗 Learn More

- [Computed Columns](https://docs.microsoft.com/en-us/sql/relational-databases/tables/specify-computed-columns-in-a-table) - Microsoft Docs
- [Persisted Computed Columns](https://docs.microsoft.com/en-us/sql/t-sql/statements/alter-table-computed-column-definition-transact-sql) - Persisted vs non-persisted
- [Indexes on Computed Columns](https://docs.microsoft.com/en-us/sql/relational-databases/indexes/indexes-on-computed-columns) - Performance guide
- [Deterministic Functions](https://docs.microsoft.com/en-us/sql/relational-databases/user-defined-functions/deterministic-and-nondeterministic-functions) - Function reference

## ❓ Discussion Questions

1. When would you choose a computed column over storing the value explicitly? What are the trade-offs?
2. Why can't non-deterministic functions like `GETDATE()` be used in persisted computed columns?
3. How do computed columns affect INSERT/UPDATE performance? When might this be a concern?
4. Could you achieve the same result with a view instead of computed columns? What are the differences?
5. How do computed columns interact with NULL values? What's the best way to handle NULLs?
6. When would you use a computed column vs. a calculated field in the application layer?

## 💡 Try It Yourself

### Exercise 1: Create BMI Computed Column

Add a BMI (Body Mass Index) computed column to a hypothetical Health table:

```sql
CREATE TABLE HealthRecords (
    Id INT PRIMARY KEY,
    PatientId INT,
    HeightCm DECIMAL(5,2),
    WeightKg DECIMAL(5,2)
);

-- Add BMI computed column
-- BMI = Weight(kg) / (Height(m))^2
```

Should it be persisted? Why or why not?

### Exercise 2: Searchable Product Code

Create a computed column that combines category code and product ID for easy searching:

```sql
CREATE TABLE Products (
    Id INT PRIMARY KEY,
    CategoryCode CHAR(3),
    ProductNumber INT
);

-- Create: SearchCode as 'XXX-00000' format
-- Make it searchable with an index
```

### Exercise 3: Performance Test

Compare query performance:
1. Query with computed column in WHERE clause
2. Same query with calculation in WHERE clause
3. Query using indexed persisted computed column

Use `SET STATISTICS TIME ON` to measure.

---

**Key Takeaway:** Computed columns eliminate redundancy and ensure data consistency by deriving values automatically from source columns. Use persisted computed columns with indexes for frequently-queried derived values, and non-persisted columns for time-sensitive calculations that change frequently.
