# 28 - JSON Support in SQL Server

## 📖 What You'll Learn

- Storing JSON data in SQL Server columns (SQL Server 2016+)
- Using `JSON_VALUE()` to extract scalar values from JSON
- Using `JSON_QUERY()` to extract objects and arrays
- Using `OPENJSON()` to expand JSON arrays into rows
- Validating JSON data with `ISJSON()` and CHECK constraints
- Modifying JSON data with `JSON_MODIFY()`
- Querying JSON properties in WHERE clauses and JOINs

## 🎯 Why This Matters

JSON support in SQL Server provides **schema flexibility** without sacrificing the power of relational databases:

- **Flexible Metadata**: Store varying attributes without ALTER TABLE for each new field
- **API Integration**: Native JSON support simplifies data exchange with web services and applications
- **Hybrid Approach**: Combine structured relational data with semi-structured JSON data
- **Real-World Use Cases**: Product catalogs with varying attributes, user preferences, audit logs, configuration settings

**Trade-offs to Consider:**
- JSON queries are generally slower than indexed columns
- Use JSON for truly variable data, not for core business entities
- Balance between schema flexibility and query performance

## 🔍 Key Concepts

### JSON Functions Comparison

| Function | Purpose | Returns | Use Case |
|----------|---------|---------|----------|
| **ISJSON()** | Validates JSON | 1 (valid) or 0 (invalid) | CHECK constraints, validation |
| **JSON_VALUE()** | Extracts scalar value | NVARCHAR(4000) or NULL | Get genre, rating, single tags |
| **JSON_QUERY()** | Extracts object/array | JSON string or NULL | Get nested objects, arrays |
| **OPENJSON()** | Expands JSON to rows | Table (key, value, type) | Expand tags array, search within arrays |
| **JSON_MODIFY()** | Updates JSON | Modified JSON string | Add/update/delete JSON properties |

### 1. Storing JSON in a Column

**Column Definition:**
```sql
ALTER TABLE Books
ADD Metadata NVARCHAR(MAX) NULL;

-- Add validation constraint
ALTER TABLE Books
ADD CONSTRAINT CK_Books_Metadata_ValidJson
    CHECK (Metadata IS NULL OR ISJSON(Metadata) = 1);
```

**Why NVARCHAR(MAX)?**
- JSON is stored as text (not a special binary type)
- `NVARCHAR(MAX)` supports up to 2GB
- Unicode support for international characters
- SQL Server optimizes storage automatically

**Advantages:**
- Schema flexibility without migrations
- Easy integration with JSON APIs
- Can store different structures per row

**Disadvantages:**
- No indexing on JSON properties directly (use computed columns)
- Slower queries compared to indexed columns
- Type checking happens at query time, not storage time

### 2. JSON_VALUE() - Extract Scalar Values

**Purpose:** Extract a single scalar value (string, number, boolean) from JSON

**Syntax:**
```sql
JSON_VALUE(column, '$.path')
```

**Example - Get book genre:**
```sql
SELECT
    Title,
    JSON_VALUE(Metadata, '$.genre') AS Genre,
    JSON_VALUE(Metadata, '$.rating') AS Rating
FROM Books
WHERE Metadata IS NOT NULL;
```

**Path Syntax:**
- `$.property` - Root level property
- `$.object.property` - Nested property
- `$.array[0]` - Array element by index
- `$.array[0].property` - Property of array element

**Important:** Returns NULL if:
- Path doesn't exist
- Value is an object or array (use JSON_QUERY instead)
- JSON is invalid

### 3. JSON_QUERY() - Extract Objects and Arrays

**Purpose:** Extract complex values (objects, arrays) as JSON strings

**Syntax:**
```sql
JSON_QUERY(column, '$.path')
```

**Example - Get tags array:**
```sql
SELECT
    Title,
    JSON_QUERY(Metadata, '$.tags') AS TagsJson,
    JSON_QUERY(Metadata, '$.customFields') AS CustomFieldsJson
FROM Books
WHERE Metadata IS NOT NULL;
```

**Returns:**
```
Title                    | TagsJson                           | CustomFieldsJson
------------------------ | ---------------------------------- | ------------------
Foundation               | ["sci-fi","space","adventure"]     | NULL
The Hobbit               | ["fantasy","magic","adventure"]    | {"edition":"2nd"}
```

**Difference from JSON_VALUE:**
- JSON_VALUE: `["a","b","c"]` → NULL (can't return arrays)
- JSON_QUERY: `["a","b","c"]` → `["a","b","c"]` (returns JSON string)

### 4. OPENJSON() - Expand Arrays to Rows

**Purpose:** Convert JSON arrays into relational rows for querying

**Syntax:**
```sql
OPENJSON(column, '$.arrayPath')
```

**Example - Find all books with a specific tag:**
```sql
SELECT DISTINCT
    b.Title,
    tags.[value] AS Tag
FROM Books b
CROSS APPLY OPENJSON(b.Metadata, '$.tags') AS tags
WHERE tags.[value] = 'sci-fi';
```

**Returns:**
```
Title       | Tag
----------- | --------
Foundation  | sci-fi
Neuromancer | sci-fi
```

**How it works:**
1. `OPENJSON` expands JSON array to a table
2. `CROSS APPLY` joins each book with its expanded tags
3. WHERE filters for specific tag values

**OPENJSON with schema:**
```sql
SELECT *
FROM OPENJSON(@json)
WITH (
    genre NVARCHAR(50) '$.genre',
    rating DECIMAL(3,1) '$.rating',
    tags NVARCHAR(MAX) '$.tags' AS JSON
);
```

## 🎯 Our Implementation: Flexible Book Metadata

Migration V018 adds JSON support to the Books table:

### 1. Metadata Column with Validation

```sql
ALTER TABLE Books
ADD Metadata NVARCHAR(MAX) NULL;

ALTER TABLE Books
ADD CONSTRAINT CK_Books_Metadata_ValidJson
    CHECK (Metadata IS NULL OR ISJSON(Metadata) = 1);
```

**Key Feature:** CHECK constraint prevents invalid JSON from being stored

### 2. BookMetadata C# DTO

```csharp
public class BookMetadata
{
    [JsonPropertyName("genre")]
    public string? Genre { get; init; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; init; }

    [JsonPropertyName("series")]
    public string? Series { get; init; }

    [JsonPropertyName("seriesNumber")]
    public int? SeriesNumber { get; init; }

    public string? ToJson() { /* ... */ }
    public static BookMetadata? FromJson(string? json) { /* ... */ }
}
```

**Pattern:** Strongly-typed C# access to loosely-typed JSON storage

### 3. Stored Procedure - Search by Metadata

```sql
CREATE PROCEDURE dbo.sp_GetBooksByMetadataValue
    @JsonPath NVARCHAR(100),
    @SearchValue NVARCHAR(200)
AS
BEGIN
    SELECT Id, ISBN, Title, Metadata,
           JSON_VALUE(Metadata, @JsonPath) AS ExtractedValue
    FROM Books
    WHERE JSON_VALUE(Metadata, @JsonPath) = @SearchValue
        AND IsDeleted = 0;
END
```

**Usage from C#:**
```csharp
var sciFiBooks = await repository.SearchByMetadataValueAsync(
    "$.genre",
    "Science Fiction",
    transaction);
```

### 4. View with Parsed Metadata

```sql
CREATE VIEW dbo.vw_BooksWithMetadata AS
    SELECT
        Id, Title, Publisher, Metadata,
        JSON_VALUE(Metadata, '$.genre') AS Genre,
        JSON_VALUE(Metadata, '$.series') AS Series,
        JSON_QUERY(Metadata, '$.tags') AS TagsJson
    FROM Books
    WHERE Metadata IS NOT NULL AND IsDeleted = 0;
```

### 5. Function - Find Books by Tag

```sql
CREATE FUNCTION dbo.fn_GetBooksByTag(@Tag NVARCHAR(50))
RETURNS TABLE
AS RETURN
(
    SELECT DISTINCT b.Id, b.Title, tags.[value] AS Tag
    FROM Books b
    CROSS APPLY OPENJSON(b.Metadata, '$.tags') AS tags
    WHERE tags.[value] = @Tag AND b.IsDeleted = 0
);
```

## ⚠️ Common Pitfalls

### ❌ Don't: Use JSON for everything

```sql
-- BAD: Core business data in JSON
CREATE TABLE Orders (
    Id INT PRIMARY KEY,
    OrderData NVARCHAR(MAX)  -- Contains customer, items, total, etc.
);
```

**Why:** Core queryable data should be in columns for:
- Performance (indexed access)
- Referential integrity (foreign keys)
- Data type enforcement
- Clear schema documentation

**Fix:** Use JSON for truly variable data:

```sql
-- GOOD: Structured core data + flexible metadata
CREATE TABLE Orders (
    Id INT PRIMARY KEY,
    CustomerId INT FOREIGN KEY REFERENCES Customers(Id),
    Total DECIMAL(10,2),
    OrderDate DATETIME2,
    Metadata NVARCHAR(MAX)  -- Gift message, special instructions, etc.
);
```

### ❌ Don't: Forget NULL handling

```sql
-- BAD: Doesn't check if Metadata exists
SELECT Title
FROM Books
WHERE JSON_VALUE(Metadata, '$.rating') > 4.0;  -- NULLs excluded silently!
```

**Fix:** Explicit NULL checks:

```sql
-- GOOD: Handle NULLs explicitly
SELECT Title
FROM Books
WHERE Metadata IS NOT NULL
    AND CAST(JSON_VALUE(Metadata, '$.rating') AS DECIMAL(3,1)) > 4.0;
```

### ❌ Don't: Store JSON without validation

```sql
-- BAD: No validation, accepts invalid JSON
UPDATE Books
SET Metadata = '{invalid json'
WHERE Id = 1;  -- Succeeds but breaks queries!
```

**Fix:** Always use CHECK constraint:

```sql
-- GOOD: Validation constraint prevents invalid JSON
ALTER TABLE Books
ADD CONSTRAINT CK_Books_Metadata_ValidJson
    CHECK (Metadata IS NULL OR ISJSON(Metadata) = 1);
```

## ✅ Best Practices

### 1. Index JSON Properties with Computed Columns

```sql
-- Create persisted computed column
ALTER TABLE Books
ADD Genre AS JSON_VALUE(Metadata, '$.genre') PERSISTED;

-- Index the computed column
CREATE INDEX IX_Books_Genre ON Books(Genre)
WHERE Genre IS NOT NULL;

-- Fast queries!
SELECT * FROM Books WHERE Genre = 'Science Fiction';
```

### 2. Use Explicit Type Casting

```sql
-- GOOD: Explicit casting for numeric comparisons
SELECT Title
FROM Books
WHERE CAST(JSON_VALUE(Metadata, '$.rating') AS DECIMAL(3,1)) >= 4.5;

-- GOOD: Explicit casting for dates
SELECT Title
FROM Books
WHERE CAST(JSON_VALUE(Metadata, '$.publishedYear') AS INT) = 2023;
```

### 3. Combine JSON with Relational Features

```sql
-- GOOD: Join JSON data with relational data
SELECT
    c.Name AS Category,
    JSON_VALUE(b.Metadata, '$.genre') AS Genre,
    COUNT(*) AS BookCount
FROM Books b
INNER JOIN Categories c ON b.CategoryId = c.Id
WHERE b.Metadata IS NOT NULL
GROUP BY c.Name, JSON_VALUE(b.Metadata, '$.genre');
```

### 4. Use JSON_MODIFY for Updates

```sql
-- Add or update a property
UPDATE Books
SET Metadata = JSON_MODIFY(Metadata, '$.rating', 4.5)
WHERE Id = 1;

-- Add to array
UPDATE Books
SET Metadata = JSON_MODIFY(Metadata, 'append $.tags', 'bestseller')
WHERE Id = 1;

-- Delete a property
UPDATE Books
SET Metadata = JSON_MODIFY(Metadata, '$.tempField', NULL)
WHERE Id = 1;
```

## 🧪 Testing This Feature

Our tests (`JsonSupportTests.cs`) verify:
1. ✅ JSON serialization/deserialization with `BookMetadata`
2. ✅ Storing and retrieving JSON metadata
3. ✅ Searching by JSON properties with `JSON_VALUE()`
4. ✅ Finding books by tag with `OPENJSON()`
5. ✅ Updating JSON metadata
6. ✅ Handling NULL metadata gracefully
7. ✅ Complex JSON structures with nested objects and arrays

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~JsonSupportTests"
```

## 🔍 C# Implementation

```csharp
// Create book with metadata
var metadata = BookMetadata.Create(
    genre: "Science Fiction",
    tags: new List<string> { "sci-fi", "space", "adventure" },
    series: "Foundation",
    seriesNumber: 1,
    rating: 4.5m
);

var book = new Book("1234567890123", "Foundation", categoryId, 5);
book.UpdateMetadata(metadata);

await repository.CreateAsync(book, transaction);

// Search by genre
var sciFiBooks = await repository.SearchByMetadataValueAsync(
    "$.genre",
    "Science Fiction",
    transaction
);

// Find books by tag
var classicBooks = await repository.GetBooksByTagAsync("classic", transaction);

// Access metadata
Console.WriteLine($"Genre: {book.Metadata?.Genre}");
Console.WriteLine($"Series: {book.Metadata?.Series} #{book.Metadata?.SeriesNumber}");
Console.WriteLine($"Rating: {book.Metadata?.Rating:F1}");
```

**Key Pattern:** JSON stored in database, strongly-typed access in C#

## 🔗 Learn More

- [JSON Data in SQL Server](https://docs.microsoft.com/en-us/sql/relational-databases/json/json-data-sql-server) - Microsoft Docs
- [JSON Functions](https://docs.microsoft.com/en-us/sql/t-sql/functions/json-functions-transact-sql) - Complete function reference
- [JSON Path Expressions](https://docs.microsoft.com/en-us/sql/relational-databases/json/json-path-expressions-sql-server) - Path syntax guide
- [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to) - C# JSON serialization
- [When to Use JSON in SQL Server](https://www.brentozar.com/archive/2016/03/introduction-json-sql-server/) - Best practices guide

## ❓ Discussion Questions

1. When should you use JSON columns instead of adding new table columns? What are the trade-offs?
2. How does JSON support affect database normalization? Is it a violation of normal forms?
3. What are the performance implications of querying JSON data vs. indexed columns?
4. How would you migrate from JSON to structured columns if a property becomes commonly queried?
5. Can you implement foreign key constraints on JSON properties? Why or why not?
6. How does JSON support in SQL Server compare to document databases like MongoDB?

## 💡 Try It Yourself

### Exercise 1: Add Review Metadata

Add a `reviews` array to book metadata with reviewer name and score:

```json
{
  "genre": "Science Fiction",
  "reviews": [
    {"reviewer": "John Doe", "score": 5},
    {"reviewer": "Jane Smith", "score": 4}
  ]
}
```

Write a query to find average review score per book.

### Exercise 2: Create a Book Recommendation View

Create a view that recommends books based on:
- Genre similarity
- Tags overlap
- Ratings above 4.0

Use JSON functions to extract and compare metadata.

### Exercise 3: Performance Comparison

1. Create a persisted computed column for `Genre`
2. Add an index on it
3. Compare query performance:
   - `WHERE JSON_VALUE(Metadata, '$.genre') = 'Fantasy'`
   - `WHERE Genre = 'Fantasy'` (using indexed computed column)

Use `SET STATISTICS TIME ON` to measure.

---

**Key Takeaway:** JSON support in SQL Server provides schema flexibility for variable data while maintaining relational database benefits. Use it strategically for truly flexible attributes, not as a replacement for proper schema design. Combine with computed columns and indexes for performance-critical queries.
