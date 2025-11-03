# Entity Framework Core: Complete Guide to ORM, LINQ, and Expression Trees

## Table of Contents

1. [Introduction](#introduction)
2. [What is LINQ?](#what-is-linq)
3. [How LINQ Works: Expression Trees](#how-linq-works-expression-trees)
4. [EF Core Architecture: Under the Hood](#ef-core-architecture-under-the-hood)
5. [Advanced Query Patterns](#advanced-query-patterns)
6. [Performance Optimization](#performance-optimization)
7. [ORM Problems and Trade-offs](#orm-problems-and-trade-offs)
8. [Anti-Patterns and Best Practices](#anti-patterns-and-best-practices)
9. [Comparison: EF Core vs ADO.NET vs SqlKata](#comparison-ef-core-vs-adonet-vs-sqlkata)

---

## Introduction

**Entity Framework Core (EF Core)** is a modern **Object-Relational Mapper (ORM)** for .NET that enables developers to work with databases using .NET objects. Instead of writing raw SQL, you write **LINQ queries** in C# that get translated to SQL at runtime.

### What is an ORM?

An **Object-Relational Mapper** bridges the gap between:
- **Object-Oriented Programming** (C# classes, objects, properties)
- **Relational Databases** (tables, rows, columns)

```
C# Domain Model              ORM              Relational Database
================            =====             ===================
public class Book     <-->  MAPPING    <-->  CREATE TABLE Books (
{                                                Id INT PRIMARY KEY,
    int Id                                       Title NVARCHAR(500),
    string Title                                 ISBN NVARCHAR(20)
    string ISBN                                )
}
```

### Why Use EF Core?

**Advantages:**
- ✅ Type-safe queries (compile-time checking)
- ✅ Automatic SQL generation
- ✅ Change tracking (knows what changed)
- ✅ Lazy/eager loading of related data
- ✅ Database migrations
- ✅ Cross-database support (SQL Server, PostgreSQL, SQLite, etc.)
- ✅ Reduces boilerplate code (no manual mapping)

**Disadvantages:**
- ❌ Performance overhead (compared to raw SQL)
- ❌ Can generate inefficient queries
- ❌ Steep learning curve for complex scenarios
- ❌ Less control over exact SQL
- ❌ Can lead to N+1 query problems
- ❌ Cartesian explosion with multiple includes

---

## What is LINQ?

**LINQ (Language Integrated Query)** is a set of features in C# that allows you to query data using a consistent syntax across different data sources (databases, collections, XML, etc.).

### LINQ Syntax: Two Flavors

#### 1. Query Syntax (SQL-like)
```csharp
// Query syntax (looks like SQL)
var activeBooks = from book in _context.Books
                  where book.IsActive == true
                  orderby book.Title
                  select book;
```

#### 2. Method Syntax (Fluent API)
```csharp
// Method syntax (more common in EF Core)
var activeBooks = _context.Books
    .Where(b => b.IsActive)
    .OrderBy(b => b.Title);
```

**Both compile to the same thing!** Method syntax is more popular because it's more flexible and composable.

### Why LINQ is Powerful

#### 1. Type Safety
```csharp
// COMPILE ERROR: Property doesn't exist
var books = _context.Books.Where(b => b.NonExistentProperty == "test");
//                                     ^^^^^^^^^^^^^^^^^^^^ CS0117 error at compile time

// Raw SQL: Error only at runtime
var sql = "SELECT * FROM Books WHERE NonExistentColumn = 'test'";
// No error until you execute the query!
```

#### 2. IntelliSense Support
```csharp
_context.Books
    .Where(b => b.  // IntelliSense shows all Book properties
//              ^
//              |
//              Title, ISBN, PublishedDate, etc.
```

#### 3. Composability
```csharp
// Build queries dynamically
IQueryable<Book> query = _context.Books;

if (activeOnly)
{
    query = query.Where(b => b.IsActive);
}

if (!string.IsNullOrEmpty(searchTerm))
{
    query = query.Where(b => EF.Functions.Like(b.Title, $"%{searchTerm}%"));
}

if (sortByTitle)
{
    query = query.OrderBy(b => b.Title);
}

// Query is NOT executed yet!
var results = await query.ToListAsync(); // <-- Executes here
```

#### 4. Cross-Platform Queries
```csharp
// Same LINQ syntax works on:
var inMemoryList = new List<Book> { /* ... */ };
var inMemoryResult = inMemoryList.Where(b => b.IsActive).ToList(); // LINQ to Objects

var dbResult = _context.Books.Where(b => b.IsActive).ToList(); // LINQ to SQL (via EF)
```

### CRITICAL: Queries Run on DB Side, NOT Client Side

This is the most important concept to understand about EF Core and LINQ:

```csharp
// ❌ BAD: Fetches ALL books from DB, then filters in C#
var allBooks = await _context.Books.ToListAsync(); // <-- Executes query
var activeBooks = allBooks.Where(b => b.IsActive).ToList(); // <-- Filters in memory

// SQL Generated:
// SELECT * FROM Books  <-- Returns 1 million rows to C# app!

// Then C# filters in memory (slow, uses tons of RAM)
```

```csharp
// ✅ GOOD: Filtering happens in database
var activeBooks = await _context.Books
    .Where(b => b.IsActive)
    .ToListAsync(); // <-- Executes query

// SQL Generated:
// SELECT * FROM Books WHERE IsActive = 1  <-- Returns only active books
```

**When does the query execute?**

```csharp
// NOT executed (returns IQueryable<Book>)
IQueryable<Book> query = _context.Books.Where(b => b.IsActive);

// NOT executed (still composing query)
query = query.OrderBy(b => b.Title);

// NOT executed (still composing)
query = query.Take(10);

// EXECUTED HERE (materializes results)
var results = await query.ToListAsync();
```

**Methods that execute the query immediately:**
- `ToList()` / `ToListAsync()`
- `First()` / `FirstAsync()`
- `Single()` / `SingleAsync()`
- `Count()` / `CountAsync()`
- `Any()` / `AnyAsync()`
- `ToArray()` / `ToDictionary()`
- Iterating with `foreach`

---

## How LINQ Works: Expression Trees

### What is an Expression Tree?

An **expression tree** is a data structure that represents C# code as a tree of nodes. Instead of executable code, it's a **description** of code that can be analyzed and translated.

```csharp
// Regular lambda expression (Func<Book, bool>)
Func<Book, bool> predicate = b => b.IsActive;

// Expression tree version (Expression<Func<Book, bool>>)
Expression<Func<Book, bool>> expression = b => b.IsActive;
//                                         ^^^^^^^^^^^^^^^^
//                                         This is NOT compiled to IL
//                                         It's a data structure describing the logic
```

### Visual Representation

```csharp
Expression<Func<Book, bool>> expr = b => b.IsActive && b.PublishedDate.Year > 2020;
```

This compiles to an expression tree:

```
                    BinaryExpression (&&)
                   /                      \
                  /                        \
        MemberAccess                 BinaryExpression (>)
        (b.IsActive)                /                   \
                                   /                     \
                          MemberAccess              ConstantExpression
                      (b.PublishedDate.Year)              (2020)
```

### Why Expression Trees Matter for EF Core

**EF Core uses expression trees to translate C# LINQ queries to SQL:**

```csharp
// Your C# code
var books = await _context.Books
    .Where(b => b.PublishedDate.Year > 2020)
    .ToListAsync();
```

**What EF Core does:**

1. **Receives expression tree**: `Expression<Func<Book, bool>>` describing `b.PublishedDate.Year > 2020`

2. **Walks the tree** using an `ExpressionVisitor`:
   - Finds `MemberAccess` node for `b.PublishedDate`
   - Finds `MemberAccess` node for `.Year`
   - Finds `BinaryExpression` node for `>`
   - Finds `ConstantExpression` node for `2020`

3. **Translates to SQL**:
   ```sql
   SELECT [b].[Id], [b].[Title], [b].[ISBN], [b].[PublishedDate], ...
   FROM [Books] AS [b]
   WHERE DATEPART(year, [b].[PublishedDate]) > 2020
   ```

4. **Executes SQL** on database server

5. **Materializes results** into `Book` objects

### Expression Tree Example: Building Manually

```csharp
// Let's build: b => b.IsActive

// 1. Create parameter (b)
ParameterExpression parameter = Expression.Parameter(typeof(Book), "b");

// 2. Create property access (b.IsActive)
MemberExpression property = Expression.Property(parameter, "IsActive");

// 3. Create lambda expression
Expression<Func<Book, bool>> lambda = Expression.Lambda<Func<Book, bool>>(
    property,     // body: b.IsActive
    parameter     // parameter: b
);

// 4. Use in EF Core query
var books = await _context.Books.Where(lambda).ToListAsync();

// Same as:
var books = await _context.Books.Where(b => b.IsActive).ToListAsync();
```

### Expression Visitor: How EF Core Walks the Tree

EF Core uses the **Visitor Pattern** to traverse expression trees:

```csharp
public class SqlTranslatingExpressionVisitor : ExpressionVisitor
{
    private StringBuilder _sql = new StringBuilder();

    protected override Expression VisitBinary(BinaryExpression node)
    {
        // Handle: b.Year > 2020
        Visit(node.Left);      // Visit left side (b.Year)

        _sql.Append(" ");
        _sql.Append(GetOperator(node.NodeType)); // ">"
        _sql.Append(" ");

        Visit(node.Right);     // Visit right side (2020)

        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // Handle: b.PublishedDate.Year
        if (node.Expression is MemberExpression parent)
        {
            Visit(parent); // b.PublishedDate
            _sql.Append($"DATEPART(year, {parent.Member.Name})");
        }
        else
        {
            _sql.Append($"[{node.Member.Name}]");
        }
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        // Handle: 2020
        _sql.Append(node.Value);
        return node;
    }

    private string GetOperator(ExpressionType type)
    {
        return type switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.LessThan => "<",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            _ => throw new NotSupportedException()
        };
    }
}
```

### Limitations of Expression Trees

**Not all C# code can be translated to SQL:**

```csharp
// ❌ RUNTIME ERROR: Cannot translate custom method to SQL
public bool IsRecent(DateTime date) => date.Year > 2020;

var books = await _context.Books
    .Where(b => IsRecent(b.PublishedDate)) // <-- Throws exception
    .ToListAsync();

// Error: "The LINQ expression 'IsRecent(b.PublishedDate)' could not be translated."
```

**Solution: Use EF.Functions

```csharp
var books = await _context.Books
    .Where(b => EF.Functions.DateDiffYear(b.PublishedDate, DateTime.UtcNow) < 3)
    .ToListAsync();
```

---

## EF Core Architecture: Under the Hood

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Your Application                         │
│  _context.Books.Where(b => b.IsActive).ToListAsync()           │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                         DbContext                                │
│  - DbSet<Book> Books                                            │
│  - ChangeTracker (tracks entity states)                         │
│  - Database (connection, transactions)                          │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                    LINQ Query Pipeline                           │
│  1. Expression Tree Received                                    │
│  2. Query Optimization                                          │
│  3. SQL Translation                                             │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                  Database Provider (SQL Server)                  │
│  - Translates to SQL Server dialect                             │
│  - Generates parameterized queries                              │
│  - Handles data type mapping                                    │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                      ADO.NET Layer                               │
│  - SqlConnection                                                │
│  - SqlCommand                                                   │
│  - SqlDataReader                                                │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                      SQL Server Database                         │
└─────────────────────────────────────────────────────────────────┘
```

### 1. DbContext: The Heart of EF Core

**DbContext responsibilities:**
- Manages database connection
- Tracks entity changes
- Provides DbSet<T> properties for querying
- Coordinates SaveChanges operations
- Manages transactions

```csharp
public class LibraryDbContext : DbContext
{
    // DbSet properties (entry points for queries)
    public DbSet<Book> Books { get; set; }
    public DbSet<Author> Authors { get; set; }

    // Configuration
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure entities, relationships, indexes, etc.
    }

    // Internal components (you don't usually access these directly)
    // - ChangeTracker: Tracks entity states
    // - Database: Connection/transaction management
    // - Model: Metadata about entities (cached)
}
```

**DbContext Lifecycle:**

```csharp
// 1. Create DbContext (usually via DI)
using var context = new LibraryDbContext(options);

// 2. Query data (ChangeTracker starts tracking)
var book = await context.Books.FirstAsync(b => b.Id == 1);

// 3. Modify entity
book.Title = "New Title"; // <-- ChangeTracker detects this

// 4. SaveChanges (generates UPDATE SQL)
await context.SaveChangesAsync();

// 5. Dispose (releases connection)
```

### 2. Query Translation Pipeline

When you write a LINQ query, EF Core goes through multiple stages:

#### Stage 1: Expression Tree Construction

```csharp
var query = _context.Books
    .Where(b => b.IsActive)
    .OrderBy(b => b.Title)
    .Take(10);
```

EF Core receives an `IQueryable<Book>` with an expression tree describing:
- Filter: `b => b.IsActive`
- Sort: `b => b.Title`
- Limit: `10`

#### Stage 2: Query Optimization

EF Core optimizes the query:
- Combines multiple `Where` clauses
- Removes redundant operations
- Applies global query filters

```csharp
// Your code:
var query = _context.Books
    .Where(b => b.IsActive)
    .Where(b => b.PublishedDate.Year > 2020); // Two Where clauses

// EF Core optimizes to:
// SELECT * FROM Books WHERE IsActive = 1 AND DATEPART(year, PublishedDate) > 2020
```

#### Stage 3: SQL Generation

The database provider (SQL Server, PostgreSQL, etc.) generates SQL:

```csharp
// Your LINQ:
var books = _context.Books
    .Where(b => b.IsActive)
    .OrderBy(b => b.Title)
    .Take(10);

// Generated SQL (SQL Server):
SELECT TOP(@__p_0) [b].[Id], [b].[Title], [b].[ISBN], [b].[PublishedDate], ...
FROM [Books] AS [b]
WHERE [b].[IsActive] = CAST(1 AS bit)
ORDER BY [b].[Title]

// Generated SQL (PostgreSQL) - different dialect:
SELECT b."Id", b."Title", b."ISBN", b."PublishedDate", ...
FROM "Books" AS b
WHERE b."IsActive" = TRUE
ORDER BY b."Title"
LIMIT @__p_0
```

#### Stage 4: Parameterization

EF Core **parameterizes** queries to prevent SQL injection and enable query plan reuse:

```csharp
var searchTerm = userInput; // Could be malicious!
var books = await _context.Books
    .Where(b => b.Title == searchTerm)
    .ToListAsync();

// Generated SQL (parameterized):
SELECT * FROM Books WHERE Title = @p0

// Parameter: @p0 = "User Input" (safely escaped)
```

#### Stage 5: Execution via ADO.NET

```csharp
// EF Core internally does something like:
using var command = connection.CreateCommand();
command.CommandText = "SELECT * FROM Books WHERE Title = @p0";
command.Parameters.Add("@p0", "User Input");

using var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    var book = new Book
    {
        Id = reader.GetInt32(0),
        Title = reader.GetString(1),
        // ... materialize all properties
    };
    results.Add(book);
}
```

### 3. Change Tracking: How EF Knows What Changed

**Entity States:**

```csharp
public enum EntityState
{
    Detached,   // Not tracked by context
    Unchanged,  // Tracked, no changes
    Added,      // New entity, INSERT on SaveChanges
    Modified,   // Existing entity with changes, UPDATE on SaveChanges
    Deleted     // Marked for deletion, DELETE on SaveChanges
}
```

**Change Tracking Example:**

```csharp
// 1. Load entity (State: Unchanged)
var book = await _context.Books.FirstAsync(b => b.Id == 1);
Console.WriteLine(_context.Entry(book).State); // Unchanged

// 2. Modify property (State: Modified)
book.Title = "New Title";
Console.WriteLine(_context.Entry(book).State); // Modified

// EF Core tracks the original value:
var titleProperty = _context.Entry(book).Property(b => b.Title);
Console.WriteLine(titleProperty.OriginalValue); // "Old Title"
Console.WriteLine(titleProperty.CurrentValue);  // "New Title"
Console.WriteLine(titleProperty.IsModified);    // True

// 3. SaveChanges generates UPDATE only for modified columns
await _context.SaveChangesAsync();
// UPDATE Books SET Title = @p0 WHERE Id = @p1
// Only Title is updated, not all columns!
```

**Disabling Change Tracking (AsNoTracking):**

```csharp
// With tracking (default for queries)
var book = await _context.Books.FirstAsync(b => b.Id == 1);
// State: Unchanged
// ChangeTracker holds reference to entity
// 30-40% slower, uses more memory

// Without tracking (read-only)
var book = await _context.Books
    .AsNoTracking()
    .FirstAsync(b => b.Id == 1);
// State: Detached
// ChangeTracker does NOT hold reference
// 30-40% faster, uses less memory
// Cannot SaveChanges (no tracking)
```

**When to use AsNoTracking:**
- ✅ Read-only queries (displaying data)
- ✅ API endpoints that return DTOs
- ✅ Reports and analytics
- ❌ When you need to update entities
- ❌ When you need to track relationships

### 4. SaveChanges: The Update Pipeline

When you call `SaveChangesAsync()`, EF Core:

1. **Detects changes** (if not already detected)
2. **Validates entities** (data annotations, custom validation)
3. **Orders operations** (respects foreign key dependencies)
4. **Generates SQL commands**
5. **Executes within transaction**
6. **Updates tracked entities** (sets IDs for new entities)

**Example: SaveChanges Internals**

```csharp
// Your code:
var book = new Book { Title = "New Book", ISBN = "123" };
_context.Books.Add(book);

var author = await _context.Authors.FirstAsync(a => a.Id == 1);
author.FirstName = "John";

await _context.SaveChangesAsync();

// What EF Core does internally:
// 1. Detect changes
var addedEntities = _context.ChangeTracker.Entries()
    .Where(e => e.State == EntityState.Added)
    .ToList(); // [book]

var modifiedEntities = _context.ChangeTracker.Entries()
    .Where(e => e.State == EntityState.Modified)
    .ToList(); // [author]

// 2. Order operations (INSERTs before UPDATEs before DELETEs)

// 3. Begin transaction
using var transaction = await _context.Database.BeginTransactionAsync();

try
{
    // 4. Execute INSERTs
    // INSERT INTO Books (Title, ISBN, ...) VALUES (@p0, @p1, ...); SELECT SCOPE_IDENTITY();
    // book.Id is set to generated ID

    // 5. Execute UPDATEs
    // UPDATE Authors SET FirstName = @p0 WHERE Id = @p1

    // 6. Commit transaction
    await transaction.CommitAsync();

    // 7. Update entity states
    foreach (var entry in addedEntities)
    {
        entry.State = EntityState.Unchanged;
    }
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### 5. Lazy Loading vs Eager Loading vs Explicit Loading

**Problem: Related data is in separate tables**

```csharp
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; }

    // Navigation property
    public List<BookAuthor> BookAuthors { get; set; }
}
```

#### Lazy Loading (NOT recommended, requires proxies)

```csharp
// Requires: Microsoft.EntityFrameworkCore.Proxies
// OnConfiguring: optionsBuilder.UseLazyLoadingProxies()

var book = await _context.Books.FirstAsync(b => b.Id == 1);
// SQL: SELECT * FROM Books WHERE Id = 1

// Accessing navigation property triggers query
foreach (var ba in book.BookAuthors) // <-- Triggers query here!
{
    Console.WriteLine(ba.Author.Name); // <-- Another query here!
}

// Total queries: 1 (book) + 1 (bookAuthors) + N (authors for each bookAuthor)
// This is the N+1 problem!
```

#### Eager Loading (Recommended for related data)

```csharp
var book = await _context.Books
    .Include(b => b.BookAuthors)
        .ThenInclude(ba => ba.Author)
    .FirstAsync(b => b.Id == 1);

// SQL: Single query with JOINs
// SELECT b.*, ba.*, a.*
// FROM Books b
// LEFT JOIN BookAuthors ba ON b.Id = ba.BookId
// LEFT JOIN Authors a ON ba.AuthorId = a.Id
// WHERE b.Id = 1

// Total queries: 1 (all data loaded)
```

#### Explicit Loading

```csharp
var book = await _context.Books.FirstAsync(b => b.Id == 1);
// SQL: SELECT * FROM Books WHERE Id = 1

// Explicitly load related data
await _context.Entry(book)
    .Collection(b => b.BookAuthors)
    .LoadAsync();
// SQL: SELECT * FROM BookAuthors WHERE BookId = 1

// Total queries: 2 (book, then bookAuthors)
```

### 6. Query Compilation and Caching

**EF Core caches compiled queries** to avoid re-translating the same LINQ query:

```csharp
// First execution
var books1 = await _context.Books.Where(b => b.IsActive).ToListAsync();
// - Expression tree → SQL translation
// - SQL generation
// - Query plan cached

// Second execution (same query shape)
var books2 = await _context.Books.Where(b => b.IsActive).ToListAsync();
// - Uses cached translation
// - Skips expression tree analysis
// - Faster!
```

**Manual Query Compilation (for frequently-used queries):**

```csharp
// Define compiled query (once, static)
private static readonly Func<LibraryDbContext, int, CancellationToken, Task<Book?>>
    CompiledGetById = EF.CompileAsyncQuery(
        (LibraryDbContext context, int id, CancellationToken ct) =>
            context.Books
                .AsNoTracking()
                .FirstOrDefault(b => b.Id == id)
    );

// Use compiled query (many times)
var book = await CompiledGetById(_context, bookId, cancellationToken);
// No expression tree analysis, directly to SQL
// 10-30% faster for hot paths
```

**See:** `src/DbDemo.Infrastructure.EFCore/Repositories/BookRepository.cs:38-59` for compiled query examples.

### 7. Transaction Management

**Implicit transactions:**

```csharp
// SaveChanges wraps all operations in a transaction
var book = new Book { Title = "Book 1" };
var author = new Author { FirstName = "Author 1" };

_context.Books.Add(book);
_context.Authors.Add(author);

await _context.SaveChangesAsync(); // <-- Both INSERT or both ROLLBACK
```

**Explicit transactions:**

```csharp
using var transaction = await _context.Database.BeginTransactionAsync();

try
{
    var book = new Book { Title = "Book 1" };
    _context.Books.Add(book);
    await _context.SaveChangesAsync();

    // Custom logic
    await SomeOtherOperation();

    var author = new Author { FirstName = "Author 1" };
    _context.Authors.Add(author);
    await _context.SaveChangesAsync();

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

**External transaction (ADO.NET SqlTransaction):**

```csharp
// Our approach in this project:
public async Task<Book> CreateAsync(Book book, SqlTransaction transaction, ...)
{
    // Attach EF context to external transaction
    await _context.Database.UseTransactionAsync(transaction, cancellationToken);

    // EF operations participate in external transaction
    var efBook = new EFBook();
    efBook.UpdateFromDomain(book, isNewEntity: true);

    _context.Books.Add(efBook);
    await _context.SaveChangesAsync(cancellationToken);

    return efBook.ToDomain();
}
```

**See:** All repository methods in `src/DbDemo.Infrastructure.EFCore/Repositories/` use external transactions.

---

## Advanced Query Patterns

### 1. Projections (Select specific columns)

**Problem: Loading entire entity when you only need a few fields**

```csharp
// ❌ BAD: Loads all columns
var books = await _context.Books.ToListAsync();
// SELECT Id, Title, ISBN, PublishedDate, Description, CoverImageUrl, PageCount, ... (50 columns)

var titles = books.Select(b => b.Title).ToList(); // In-memory projection
```

```csharp
// ✅ GOOD: Project to anonymous type or DTO
var bookTitles = await _context.Books
    .Select(b => new { b.Id, b.Title })
    .ToListAsync();
// SELECT Id, Title FROM Books (only 2 columns)
```

**Projection to DTO:**

```csharp
public class BookSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string AuthorNames { get; set; }
}

var summaries = await _context.Books
    .Select(b => new BookSummaryDto
    {
        Id = b.Id,
        Title = b.Title,
        AuthorNames = string.Join(", ", b.BookAuthors.Select(ba => ba.Author.LastName))
    })
    .ToListAsync();

// SQL: Joins Books → BookAuthors → Authors, but only selects needed columns
```

### 2. GroupBy and Aggregations

```csharp
// Count books per author
var bookCounts = await _context.BookAuthors
    .GroupBy(ba => ba.AuthorId)
    .Select(g => new
    {
        AuthorId = g.Key,
        BookCount = g.Count()
    })
    .ToListAsync();

// SQL:
// SELECT AuthorId, COUNT(*) as BookCount
// FROM BookAuthors
// GROUP BY AuthorId
```

```csharp
// Average page count per genre (using metadata JSON)
var avgPages = await _context.Books
    .Where(b => b.Metadata != null)
    .GroupBy(b => EF.Property<string>(b, "Metadata")) // Simplified
    .Select(g => new
    {
        Genre = g.Key,
        AvgPageCount = g.Average(b => b.PageCount)
    })
    .ToListAsync();
```

### 3. Raw SQL Queries

**When LINQ can't express complex logic:**

```csharp
// Execute raw SQL, map to entities
var books = await _context.Books
    .FromSqlRaw(@"
        SELECT b.*
        FROM Books b
        CROSS APPLY OPENJSON(b.Metadata, '$.tags') AS tags
        WHERE tags.[value] = {0}
    ", "science-fiction")
    .ToListAsync();

// Combines raw SQL with LINQ
var recentSciFiBooks = await _context.Books
    .FromSqlRaw("SELECT * FROM Books WHERE Metadata LIKE '%science-fiction%'")
    .Where(b => b.PublishedDate.Year > 2020) // Additional LINQ filter
    .ToListAsync();

// SQL:
// SELECT * FROM (
//     SELECT * FROM Books WHERE Metadata LIKE '%science-fiction%'
// ) AS b
// WHERE DATEPART(year, b.PublishedDate) > 2020
```

**See:** `src/DbDemo.Infrastructure.EFCore/Repositories/BookRepository.cs:334-357` for JSON query examples.

### 4. Stored Procedures and Functions

**Calling stored procedures:**

```csharp
// Execute stored procedure
await _context.Database.ExecuteSqlRawAsync(
    "EXEC sp_UpdateBookAvailability @BookId = {0}, @IsAvailable = {1}",
    bookId,
    isAvailable
);
```

**Calling table-valued functions:**

```csharp
// Our approach: Use raw ADO.NET within transaction
using var command = new SqlCommand(
    "SELECT * FROM dbo.fn_GetMemberStatistics(@MemberId)",
    transaction.Connection,
    transaction
);
command.Parameters.Add("@MemberId", SqlDbType.Int).Value = memberId;

using var reader = await command.ExecuteReaderAsync(cancellationToken);
if (await reader.ReadAsync(cancellationToken))
{
    return MemberStatistics.FromDatabase(
        memberId: reader.GetInt32(reader.GetOrdinal("MemberId")),
        totalBooksLoaned: reader.GetInt32(reader.GetOrdinal("TotalBooksLoaned")),
        // ...
    );
}
```

**See:** `src/DbDemo.Infrastructure.EFCore/Repositories/MemberRepository.cs:254-296` for TVF example.

### 5. Global Query Filters

**Automatically apply filters to all queries:**

```csharp
// In OnModelCreating:
modelBuilder.Entity<Book>()
    .HasQueryFilter(b => !b.IsDeleted); // Soft delete filter

// Now all queries automatically filter out deleted books:
var books = await _context.Books.ToListAsync();
// SQL: SELECT * FROM Books WHERE IsDeleted = 0

// Override filter when needed:
var allBooksIncludingDeleted = await _context.Books
    .IgnoreQueryFilters()
    .ToListAsync();
// SQL: SELECT * FROM Books (no IsDeleted filter)
```

**See:** `src/DbDemo.Infrastructure.EFCore/LibraryDbContext.cs:249-251` for configuration.

### 6. Split Queries vs Single Queries

**Problem: Cartesian explosion with multiple includes**

```csharp
// ❌ BAD: Single query with multiple joins
var books = await _context.Books
    .Include(b => b.BookAuthors)      // 5 authors per book
        .ThenInclude(ba => ba.Author)
    .Include(b => b.Loans)            // 10 loans per book
    .ToListAsync();

// SQL: Single query with CROSS JOIN
// Returns: 1 book × 5 authors × 10 loans = 50 rows for 1 book!
// Lots of duplicate data transferred
```

```csharp
// ✅ GOOD: Split query (EF Core 5+)
var books = await _context.Books
    .Include(b => b.BookAuthors)
        .ThenInclude(ba => ba.Author)
    .Include(b => b.Loans)
    .AsSplitQuery() // <-- Splits into separate queries
    .ToListAsync();

// SQL: Three separate queries
// Query 1: SELECT * FROM Books
// Query 2: SELECT * FROM BookAuthors WHERE BookId IN (...)
// Query 3: SELECT * FROM Loans WHERE BookId IN (...)

// More efficient for multiple collections
```

**Trade-offs:**
- Single query: Consistent snapshot, but cartesian explosion
- Split query: Multiple round-trips, but less duplicate data

---

## Performance Optimization

### 1. AsNoTracking for Read-Only Queries

```csharp
// ❌ With tracking (default)
var books = await _context.Books.ToListAsync();
// - ChangeTracker holds reference to each entity
// - Takes snapshots of property values
// - 30-40% slower, uses more memory

// ✅ Without tracking
var books = await _context.Books.AsNoTracking().ToListAsync();
// - No change tracking overhead
// - Cannot SaveChanges on these entities
// - 30-40% faster, less memory
```

**When to use:**
- ✅ API endpoints returning DTOs
- ✅ Reports and analytics
- ✅ Read-only displays
- ❌ When you need to update entities

**See:** Most repository methods use `.AsNoTracking()` for read operations.

### 2. Compiled Queries

```csharp
// Define once (static field)
private static readonly Func<LibraryDbContext, int, CancellationToken, Task<Book?>>
    GetByIdCompiled = EF.CompileAsyncQuery(
        (LibraryDbContext ctx, int id, CancellationToken ct) =>
            ctx.Books.AsNoTracking().FirstOrDefault(b => b.Id == id)
    );

// Use many times
var book = await GetByIdCompiled(_context, bookId, cancellationToken);
// 10-30% faster (skips expression tree analysis)
```

**See:** `src/DbDemo.Infrastructure.EFCore/Repositories/BookRepository.cs:38-59`

### 3. Select Only What You Need

```csharp
// ❌ BAD: Load entire entity
var books = await _context.Books.ToListAsync();
var titles = books.Select(b => b.Title).ToList();

// ✅ GOOD: Project in database
var titles = await _context.Books
    .Select(b => b.Title)
    .ToListAsync();
```

### 4. Avoid N+1 Queries

```csharp
// ❌ BAD: N+1 queries
var books = await _context.Books.ToListAsync(); // 1 query

foreach (var book in books)
{
    var authors = await _context.BookAuthors
        .Where(ba => ba.BookId == book.Id)
        .ToListAsync(); // N queries (one per book)
}
// Total: 1 + N queries

// ✅ GOOD: Single query with Include
var books = await _context.Books
    .Include(b => b.BookAuthors)
        .ThenInclude(ba => ba.Author)
    .ToListAsync();
// Total: 1 query
```

### 5. Batch Operations

```csharp
// ❌ BAD: SaveChanges in loop
foreach (var book in books)
{
    book.IsActive = true;
    await _context.SaveChangesAsync(); // Database round-trip per book
}

// ✅ GOOD: Single SaveChanges
foreach (var book in books)
{
    book.IsActive = true;
}
await _context.SaveChangesAsync(); // Single database round-trip
```

### 6. Pagination

```csharp
// Always use Skip/Take for large result sets
var page = await _context.Books
    .OrderBy(b => b.Title) // Required for deterministic pagination
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// SQL: Uses OFFSET/FETCH NEXT
// SELECT * FROM Books
// ORDER BY Title
// OFFSET 20 ROWS
// FETCH NEXT 10 ROWS ONLY
```

**See:** `src/DbDemo.Infrastructure.EFCore/Repositories/BookRepository.cs:192-219`

### 7. Bulk Operations (EF Core Extensions)

EEF Core doesn't have built-in bulk operations. Use extensions like `EFCore.BulkExtensions`:

```csharp
// Standard EF Core (slow for large datasets)
_context.Books.AddRange(10000_books);
await _context.SaveChangesAsync(); // 10,000 individual INSERTs

// With BulkExtensions (much faster)
await _context.BulkInsertAsync(10000_books); // Single bulk INSERT
```

---

## ORM Problems and Trade-offs

### 1. The Impedance Mismatch Problem

**Object-Oriented vs Relational paradigms are fundamentally different:**

```
Object-Oriented (C#)              Relational (SQL)
====================              ================
- Inheritance                     - No inheritance (workarounds: TPH, TPT)
- Encapsulation                   - All data is flat/public
- Polymorphism                    - No polymorphism
- Object graphs                   - Normalized tables with foreign keys
- Lazy loading                    - Everything loaded explicitly
- Identity (reference equality)   - Primary keys (value equality)
```

**Example: Inheritance mapping problems**

```csharp
// C# domain model (inheritance)
public abstract class LibraryItem
{
    public int Id { get; set; }
    public string Title { get; set; }
}

public class Book : LibraryItem
{
    public string ISBN { get; set; }
}

public class Magazine : LibraryItem
{
    public int IssueNumber { get; set; }
}

// EF Core must map this to SQL tables
// Option 1: Table-Per-Hierarchy (single table, discriminator column)
CREATE TABLE LibraryItems (
    Id INT PRIMARY KEY,
    Discriminator NVARCHAR(50),  -- "Book" or "Magazine"
    Title NVARCHAR(500),
    ISBN NVARCHAR(20),           -- NULL for magazines
    IssueNumber INT              -- NULL for books
)
// ✓ Simple queries
// ✗ Sparse tables with many NULLs
// ✗ Can't enforce NOT NULL constraints on subclass columns

// Option 2: Table-Per-Type (separate tables)
CREATE TABLE LibraryItems (Id INT PRIMARY KEY, Title NVARCHAR(500))
CREATE TABLE Books (Id INT PRIMARY KEY FK, ISBN NVARCHAR(20))
CREATE TABLE Magazines (Id INT PRIMARY KEY FK, IssueNumber INT)
// ✓ Normalized schema
// ✗ Requires JOINs for every query
// ✗ Complex to query

// Neither solution is perfect!
```

### 2. Rigid Query Structure (The Big Join Problem)

**Problem: ORMs encourage loading entire entity graphs**

```csharp
// "Simple" query to get book with all related data
var book = await _context.Books
    .Include(b => b.BookAuthors)
        .ThenInclude(ba => ba.Author)
    .Include(b => b.Publisher)
    .Include(b => b.Genre)
    .Include(b => b.Loans)
        .ThenInclude(l => l.Member)
    .Include(b => b.Reservations)
        .ThenInclude(r => r.Member)
    .Include(b => b.Reviews)
        .ThenInclude(r => r.Member)
    .FirstAsync(b => b.Id == bookId);

// Generated SQL: MASSIVE JOIN
SELECT b.*, ba.*, a.*, p.*, g.*, l.*, lm.*, res.*, resm.*, rev.*, revm.*
FROM Books b
LEFT JOIN BookAuthors ba ON b.Id = ba.BookId
LEFT JOIN Authors a ON ba.AuthorId = a.Id
LEFT JOIN Publishers p ON b.PublisherId = p.Id
LEFT JOIN Genres g ON b.GenreId = g.Id
LEFT JOIN Loans l ON b.Id = l.BookId
LEFT JOIN Members lm ON l.MemberId = lm.Id
LEFT JOIN Reservations res ON b.Id = res.BookId
LEFT JOIN Members resm ON res.MemberId = resm.Id
LEFT JOIN Reviews rev ON b.Id = rev.BookId
LEFT JOIN Members revm ON rev.MemberId = revm.Id
WHERE b.Id = @p0

// Cartesian explosion:
// 1 book × 3 authors × 5 loans × 2 reservations × 10 reviews
// = 300 rows returned for a single book!
```

**Why this is a problem:**
- Transfers massive amounts of duplicate data
- Query planner struggles with complex joins
- Locks multiple tables
- All-or-nothing: can't easily load just what you need

**Solutions:**

#### A. Use Projections (DTOs)
```csharp
// Only select what you need
var bookSummary = await _context.Books
    .Where(b => b.Id == bookId)
    .Select(b => new BookDetailDto
    {
        Title = b.Title,
        ISBN = b.ISBN,
        AuthorNames = b.BookAuthors.Select(ba => ba.Author.FullName).ToList(),
        ActiveLoansCount = b.Loans.Count(l => l.Status == LoanStatus.Active),
        // Only the data you actually need
    })
    .FirstAsync();

// Much simpler SQL with subqueries instead of joins
```

#### B. Use Multiple Queries
```csharp
// Query 1: Get book
var book = await _context.Books.FirstAsync(b => b.Id == bookId);

// Query 2: Get authors (only if needed)
if (includeAuthors)
{
    var authors = await _context.BookAuthors
        .Where(ba => ba.BookId == bookId)
        .Select(ba => ba.Author)
        .ToListAsync();
}

// Query 3: Get loan statistics (only if needed)
if (includeLoanStats)
{
    var loanStats = await _context.Loans
        .Where(l => l.BookId == bookId)
        .GroupBy(l => l.Status)
        .Select(g => new { Status = g.Key, Count = g.Count() })
        .ToListAsync();
}
```

#### C. Use Raw SQL for Complex Queries
```csharp
// Complex analytical query is better in raw SQL
var stats = await _context.Database.SqlQueryRaw<BookStatistics>(@"
    SELECT
        b.Id,
        b.Title,
        COUNT(DISTINCT l.Id) AS TotalLoans,
        COUNT(DISTINCT r.Id) AS TotalReservations,
        AVG(CAST(rev.Rating AS FLOAT)) AS AvgRating
    FROM Books b
    LEFT JOIN Loans l ON b.Id = l.BookId
    LEFT JOIN Reservations r ON b.Id = r.BookId
    LEFT JOIN Reviews rev ON b.Id = rev.BookId
    WHERE b.Id = @bookId
    GROUP BY b.Id, b.Title
", new SqlParameter("@bookId", bookId))
.FirstAsync();
```

### 3. Performance Overhead

**EF Core adds layers of abstraction:**

```
Your Code: _context.Books.Where(b => b.IsActive).ToListAsync()
    ↓
Expression Tree Analysis (EF Core)
    ↓
Query Optimization (EF Core)
    ↓
SQL Translation (Database Provider)
    ↓
SQL Generation
    ↓
ADO.NET Command Execution
    ↓
Result Materialization (EF Core)
    ↓
Change Tracking (EF Core)
    ↓
Your Objects: List<Book>
```

**Each layer adds overhead:**
- Expression tree parsing: ~0.1-1ms per query (cached after first execution)
- Object materialization: ~100-500 µs per object
- Change tracking: ~50-200 µs per tracked entity
- **Total overhead: ~10-30% slower than raw ADO.NET**

**Benchmark example:**

```csharp
// ADO.NET (fastest)
using var command = new SqlCommand("SELECT * FROM Books WHERE IsActive = 1", connection);
using var reader = await command.ExecuteReaderAsync();
var books = new List<Book>();
while (await reader.ReadAsync())
{
    books.Add(new Book
    {
        Id = reader.GetInt32(0),
        Title = reader.GetString(1),
        // ...
    });
}
// Time: ~10ms for 1000 books

// EF Core with AsNoTracking (good)
var books = await _context.Books.AsNoTracking()
    .Where(b => b.IsActive)
    .ToListAsync();
// Time: ~13ms for 1000 books (30% overhead)

// EF Core with tracking (slower)
var books = await _context.Books
    .Where(b => b.IsActive)
    .ToListAsync();
// Time: ~18ms for 1000 books (80% overhead)
```

**When EF Core overhead matters:**
- High-throughput APIs (thousands of requests/second)
- Real-time systems with latency requirements
- Batch processing of millions of records
- Complex analytical queries

**When EF Core overhead is acceptable:**
- CRUD operations for business applications
- Typical web applications (hundreds of requests/second)
- Developer productivity is more important than raw speed

### 4. Leaky Abstraction

**EF Core doesn't hide the database completely:**

```csharp
// ❌ This looks simple, but...
var books = await _context.Books
    .Where(b => b.Authors.Any(a => a.Country == "USA"))
    .ToListAsync();

// You need to know:
// - This generates a JOIN (or subquery)
// - Navigation property must be configured in OnModelCreating
// - May cause N+1 if not careful
// - Performance depends on indexes
// - Different SQL on different database providers
```

**You still need SQL knowledge:**
- Understanding JOINs (Include, ThenInclude)
- Understanding indexes (query performance)
- Understanding transactions (SaveChanges behavior)
- Understanding query plans (when EF generates slow SQL)

### 5. Magic Behavior and Surprises

**Change tracking can cause unexpected behavior:**

```csharp
// Load a book
var book1 = await _context.Books.FirstAsync(b => b.Id == 1);
book1.Title = "Modified Title";

// Load the same book again (within same DbContext)
var book2 = await _context.Books.FirstAsync(b => b.Id == 1);

Console.WriteLine(book1 == book2); // True (same instance!)
Console.WriteLine(book2.Title);    // "Modified Title" (not from database!)

// EF Core returns the tracked instance, not a new instance from DB
// This is identity map pattern, but can be surprising
```

**Global query filters can hide data:**

```csharp
// Configured: modelBuilder.Entity<Book>().HasQueryFilter(b => !b.IsDeleted);

var books = await _context.Books.ToListAsync();
// Only returns non-deleted books (filter is invisible here!)

// To include deleted:
var allBooks = await _context.Books.IgnoreQueryFilters().ToListAsync();
```

### 6. When NOT to Use EF Core

**EF Core is NOT ideal for:**

1. **High-performance bulk operations**
   - Use: Bulk INSERT libraries, SSIS, or raw SQL
   - EF Core: Individual INSERT/UPDATE for each entity

2. **Complex analytical queries**
   - Use: Raw SQL, stored procedures, or SqlKata
   - EF Core: Complex LINQ is hard to write and may generate inefficient SQL

3. **Reporting systems**
   - Use: Raw SQL or dedicated reporting tools
   - EF Core: Forces you to map to entities even for simple projections

4. **Legacy databases with complex schemas**
   - Use: ADO.NET or Dapper
   - EF Core: Requires extensive configuration, may not support all database features

5. **Microservices with database-per-service**
   - Use: Lighter-weight data access (Dapper, ADO.NET)
   - EF Core: Overhead may not be worth it for simple queries

### 7. The "Anemic vs Rich Domain Model" Problem

**EF Core entities are often anemic (no behavior):**

```csharp
// EF Core entity (anemic)
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; }        // Public setter (required by EF)
    public decimal Price { get; set; }       // No validation
    public int AvailableCopies { get; set; } // Can be set to invalid values
}

// Anyone can do this:
book.AvailableCopies = -100; // Invalid state!
book.Price = -50;            // Invalid state!
```

**Rich domain model (encapsulated):**

```csharp
// Domain entity (rich)
public class Book
{
    public int Id { get; private set; }
    public string Title { get; private set; }
    private decimal _price;
    private int _availableCopies;

    // Factory method
    public static Book Create(string title, decimal price, int copies)
    {
        if (price < 0) throw new ArgumentException("Price cannot be negative");
        if (copies < 0) throw new ArgumentException("Copies cannot be negative");

        return new Book { Title = title, _price = price, _availableCopies = copies };
    }

    // Business logic
    public void BorrowCopy()
    {
        if (_availableCopies <= 0)
            throw new InvalidOperationException("No copies available");

        _availableCopies--;
    }

    // Cannot create in invalid state!
}
```

**Solution in this project:**

We use **separate EF entities and Domain entities with a mapping layer**:

```
Domain Layer (Rich)          Mapping Layer          Persistence Layer (Anemic)
===================          =============          ==========================
Book (rich domain)     <-->  EntityMapper    <-->  EFBook (anemic EF entity)
- Encapsulated              .ToDomain()             - Public setters
- Invariants enforced       .UpdateFromDomain()     - No behavior
- Business logic                                    - EF Core requirements
```

**See:** `src/DbDemo.Infrastructure.EFCore/README.md` for detailed explanation.

---

## Anti-Patterns and Best Practices

### Anti-Pattern 1: Loading All Data Then Filtering in Memory

```csharp
// ❌ ANTI-PATTERN: Fetch everything, filter in C#
var allBooks = await _context.Books.ToListAsync(); // Loads 1 million books
var activeBooks = allBooks.Where(b => b.IsActive).ToList();

// SQL: SELECT * FROM Books (returns 1 million rows)
// Memory: ~1 GB for 1 million books
// Time: ~5 seconds
```

```csharp
// ✅ BEST PRACTICE: Filter in database
var activeBooks = await _context.Books
    .Where(b => b.IsActive)
    .ToListAsync();

// SQL: SELECT * FROM Books WHERE IsActive = 1 (returns 100k rows)
// Memory: ~100 MB
// Time: ~0.5 seconds
```

**Rule:** Always filter, sort, and paginate in the database using LINQ methods **before** calling `ToListAsync()`.

---

### Anti-Pattern 2: N+1 Query Problem

```csharp
// ❌ ANTI-PATTERN: N+1 queries
var books = await _context.Books.Take(100).ToListAsync(); // 1 query

foreach (var book in books)
{
    // Lazy loading triggers query for each book
    Console.WriteLine(book.Publisher.Name); // 100 queries
}

// Total: 101 queries (1 for books + 100 for publishers)
```

```csharp
// ✅ BEST PRACTICE: Eager load with Include
var books = await _context.Books
    .Include(b => b.Publisher)
    .Take(100)
    .ToListAsync();

foreach (var book in books)
{
    Console.WriteLine(book.Publisher.Name); // No additional query
}

// Total: 1 query with JOIN
```

**Rule:** Use `.Include()` for related data you know you'll need.

---

### Anti-Pattern 3: Tracking Entities for Read-Only Scenarios

```csharp
// ❌ ANTI-PATTERN: Default tracking for read-only display
public async Task<List<BookDto>> GetBooksForDisplay()
{
    var books = await _context.Books.ToListAsync(); // Tracking enabled

    return books.Select(b => new BookDto
    {
        Title = b.Title,
        ISBN = b.ISBN
    }).ToList();
}

// Wastes memory on change tracking for data that won't be modified
```

```csharp
// ✅ BEST PRACTICE: Use AsNoTracking for read-only
public async Task<List<BookDto>> GetBooksForDisplay()
{
    return await _context.Books
        .AsNoTracking()
        .Select(b => new BookDto
        {
            Title = b.Title,
            ISBN = b.ISBN
        })
        .ToListAsync();
}

// 30-40% faster, uses less memory
```

**Rule:** Use `.AsNoTracking()` for all read-only queries.

---

### Anti-Pattern 4: Multiple SaveChanges in Loop

```csharp
// ❌ ANTI-PATTERN: SaveChanges in loop
foreach (var book in books)
{
    book.IsActive = true;
    await _context.SaveChangesAsync(); // Database round-trip per book
}

// 100 books = 100 database round-trips
// Time: ~2 seconds (20ms per SaveChanges)
```

```csharp
// ✅ BEST PRACTICE: Batch changes, single SaveChanges
foreach (var book in books)
{
    book.IsActive = true;
}
await _context.SaveChangesAsync(); // Single database round-trip

// 100 books = 1 database round-trip
// Time: ~50ms
```

**Rule:** Batch changes and call `SaveChangesAsync()` once.

---

### Anti-Pattern 5: Using .Include() When Only Need Count or Existence

```csharp
// ❌ ANTI-PATTERN: Load all authors to check if book has any
var book = await _context.Books
    .Include(b => b.BookAuthors)
        .ThenInclude(ba => ba.Author)
    .FirstAsync(b => b.Id == bookId);

if (book.BookAuthors.Any())
{
    // Do something
}

// Loads all authors (expensive) just to check existence
```

```csharp
// ✅ BEST PRACTICE: Use Any() in query
var hasAuthors = await _context.BookAuthors
    .AnyAsync(ba => ba.BookId == bookId);

if (hasAuthors)
{
    // Do something
}

// SQL: SELECT CASE WHEN EXISTS(...) THEN 1 ELSE 0 END
// Much faster, doesn't load data
```

**Rule:** Use `.Any()`, `.Count()`, or `.Sum()` directly in LINQ instead of loading entities.

---

### Anti-Pattern 6: Not Using Pagination

```csharp
// ❌ ANTI-PATTERN: Load all results
public async Task<List<Book>> SearchBooks(string searchTerm)
{
    return await _context.Books
        .Where(b => b.Title.Contains(searchTerm))
        .ToListAsync();
}

// Could return 100,000 books
// Memory: ~500 MB
// Client can't display 100,000 books anyway
```

```csharp
// ✅ BEST PRACTICE: Always paginate
public async Task<List<Book>> SearchBooks(string searchTerm, int page, int pageSize)
{
    return await _context.Books
        .Where(b => b.Title.Contains(searchTerm))
        .OrderBy(b => b.Title) // Required for deterministic pagination
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
}

// Returns max 20 books per page
// Memory: ~10 KB
```

**Rule:** Always paginate large result sets.

---

### Anti-Pattern 7: Using String Concatenation in Queries

```csharp
// ❌ ANTI-PATTERN: String concatenation (SQL injection risk!)
public async Task<List<Book>> SearchByTitle(string userInput)
{
    var sql = $"SELECT * FROM Books WHERE Title LIKE '%{userInput}%'";
    return await _context.Books.FromSqlRaw(sql).ToListAsync();
}

// If userInput = "'; DROP TABLE Books; --"
// SQL: SELECT * FROM Books WHERE Title LIKE '%'; DROP TABLE Books; --%'
// 💀 SQL INJECTION VULNERABILITY!
```

```csharp
// ✅ BEST PRACTICE: Use parameterized queries
public async Task<List<Book>> SearchByTitle(string userInput)
{
    return await _context.Books
        .Where(b => EF.Functions.Like(b.Title, $"%{userInput}%"))
        .ToListAsync();

    // Or with FromSqlRaw:
    return await _context.Books
        .FromSqlRaw("SELECT * FROM Books WHERE Title LIKE {0}", $"%{userInput}%")
        .ToListAsync();
}

// EF Core parameterizes automatically
// SQL: SELECT * FROM Books WHERE Title LIKE @p0
// Parameter: @p0 = "%user input%"
```

**Rule:** **NEVER** concatenate user input into SQL strings. Always use parameterized queries.

---

### Anti-Pattern 8: Ignoring Disposal (DbContext Lifetime)

```csharp
// ❌ ANTI-PATTERN: Singleton DbContext (memory leak!)
public class BookService
{
    private readonly LibraryDbContext _context; // Singleton!

    public BookService(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<Book> GetBook(int id)
    {
        return await _context.Books.FirstAsync(b => b.Id == id);
    }
}

// Problem: DbContext accumulates tracked entities forever
// Memory grows unbounded
```

```csharp
// ✅ BEST PRACTICE: Scoped DbContext (DI creates new instance per request)
// In Startup.cs / Program.cs:
services.AddDbContext<LibraryDbContext>(ServiceLifetime.Scoped);

// DbContext is created per HTTP request, disposed after request
// Change tracker cleared automatically
```

**Rule:** Use **scoped** lifetime for DbContext (default in ASP.NET Core).

---

### Anti-Pattern 9: Modifying Entities from Different Contexts

```csharp
// ❌ ANTI-PATTERN: Attach entity from different context
public async Task UpdateBook(Book book) // Received from API
{
    _context.Books.Update(book); // Throws or updates wrong entity
    await _context.SaveChangesAsync();
}

// Problem: 'book' might be tracked by a different context or not tracked at all
```

```csharp
// ✅ BEST PRACTICE: Load entity in current context, then update
public async Task UpdateBook(int bookId, string newTitle)
{
    var book = await _context.Books.FindAsync(bookId);
    if (book == null) throw new NotFoundException();

    book.Title = newTitle;
    await _context.SaveChangesAsync();
}

// Or use mapping pattern (our approach):
public async Task<bool> UpdateAsync(Book domainBook, SqlTransaction transaction, ...)
{
    var efBook = await _context.Books.FindAsync(domainBook.Id);
    if (efBook == null) return false;

    efBook.UpdateFromDomain(domainBook, isNewEntity: false);
    await _context.SaveChangesAsync();
    return true;
}
```

**Rule:** Always load entity in current context before modifying.

---

### Anti-Pattern 10: Using EF for Bulk Operations

```csharp
// ❌ ANTI-PATTERN: Insert 10,000 books with EF
var books = GenerateBooks(10000);

_context.Books.AddRange(books);
await _context.SaveChangesAsync();

// EF Core executes 10,000 individual INSERTs
// Time: ~30 seconds
```

```csharp
// ✅ BEST PRACTICE: Use bulk extensions or raw SQL
// Option 1: EFCore.BulkExtensions
await _context.BulkInsertAsync(books);
// Time: ~2 seconds

// Option 2: SqlBulkCopy (ADO.NET)
using var bulkCopy = new SqlBulkCopy(connection);
bulkCopy.DestinationTableName = "Books";
await bulkCopy.WriteToServerAsync(dataTable);
// Time: ~1 second
```

**Rule:** For bulk operations (>1000 rows), use specialized libraries or raw SQL.

---

## Comparison: EF Core vs ADO.NET vs SqlKata

| Aspect | ADO.NET | SqlKata | EF Core |
|--------|---------|---------|---------|
| **Abstraction Level** | Low (raw SQL) | Medium (query builder) | High (ORM) |
| **Type Safety** | ❌ No | ⚠️ Partial | ✅ Yes |
| **Performance** | ✅ Fastest | ✅ Fast | ⚠️ Good (overhead) |
| **Boilerplate Code** | ❌ High | ⚠️ Medium | ✅ Low |
| **Learning Curve** | ✅ Easy (if you know SQL) | ⚠️ Medium | ❌ Steep |
| **SQL Control** | ✅ Full control | ✅ Full control | ⚠️ Limited |
| **Change Tracking** | ❌ Manual | ❌ Manual | ✅ Automatic |
| **Relationships** | ❌ Manual JOIN/mapping | ⚠️ Manual JOIN | ✅ Automatic |
| **Database Portability** | ❌ SQL is DB-specific | ✅ Good | ✅ Excellent |
| **Complex Queries** | ✅ Easy (write SQL) | ✅ Easy | ❌ Difficult |
| **Migrations** | ❌ Manual | ❌ Manual | ✅ Automatic |
| **Testing** | ❌ Requires DB | ⚠️ Requires DB | ✅ Can mock/in-memory |

### When to Use Each

**Use ADO.NET when:**
- Maximum performance is critical
- Complex stored procedures
- Bulk operations
- Full control over SQL is required
- **Example:** High-frequency trading, ETL pipelines

**Use SqlKata when:**
- Dynamic query building
- Need abstraction but not full ORM
- Performance is important
- Want database portability
- **Example:** Reporting systems, admin panels with complex filters

**Use EF Core when:**
- Standard CRUD operations
- Rapid development is priority
- Want automatic change tracking
- Need database migrations
- Cross-database support required
- **Example:** Business applications, typical web APIs

**Our Project Uses All Three:**
- `DbDemo.Infrastructure` (ADO.NET): Full control, maximum performance
- `DbDemo.Infrastructure.SqlKata`: Middle ground, query builder flexibility
- `DbDemo.Infrastructure.EFCore`: ORM convenience, rapid development

**See:**
- `docs/27-transactions-and-architecture.md` (ADO.NET patterns)
- `docs/28-scaffolding-and-query-builders.md` (SqlKata patterns)
- This document (EF Core patterns)

---

## Summary

### Key Takeaways

1. **LINQ** is powerful because:
   - Type-safe queries (compile-time checking)
   - IntelliSense support
   - Queries execute on database side, not client side
   - Composable query building

2. **Expression Trees** are the magic behind LINQ:
   - C# code represented as data structures
   - EF Core walks the tree to generate SQL
   - Not all C# code can be translated

3. **EF Core Architecture** involves:
   - DbContext (connection, change tracking, SaveChanges)
   - LINQ query pipeline (expression tree → SQL)
   - Database provider (SQL translation)
   - ADO.NET layer (execution)

4. **Performance Optimization:**
   - Use `AsNoTracking()` for read-only queries
   - Use compiled queries for hot paths
   - Project to DTOs (don't load entire entities)
   - Avoid N+1 queries (use `Include`)
   - Always paginate

5. **ORM Problems:**
   - Impedance mismatch (OOP ≠ Relational)
   - Cartesian explosion with multiple includes
   - Performance overhead (~10-30%)
   - Leaky abstraction (need SQL knowledge)
   - Not ideal for bulk operations or complex analytics

6. **Anti-Patterns to Avoid:**
   - Filtering in memory instead of database
   - N+1 queries
   - Using tracking for read-only queries
   - Multiple SaveChanges in loops
   - Not using pagination
   - SQL injection via string concatenation

7. **Our Architecture:**
   - Separate EF entities (anemic) from Domain entities (rich)
   - Mapping layer between persistence and domain
   - External transaction management (SqlTransaction)
   - Repository pattern for abstraction

### Final Recommendation

**EF Core is a powerful tool, but not a silver bullet:**

✅ **Use EF Core for:**
- Standard CRUD operations
- Typical web applications
- When developer productivity matters more than raw performance
- When you need database portability

❌ **Don't use EF Core for:**
- High-performance bulk operations
- Complex analytical queries
- When you need full SQL control
- Reporting systems

**The best approach:** Use the right tool for the job. Our project demonstrates all three approaches (ADO.NET, SqlKata, EF Core) so you can choose based on your specific requirements.

---

## Additional Resources

**Official Documentation:**
- [EF Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [LINQ Documentation](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/linq/)
- [Expression Trees](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/expression-trees/)

**Books:**
- "Entity Framework Core in Action" by Jon P. Smith
- "Programming Entity Framework: Code First" by Julia Lerman

**Performance:**
- [EF Core Performance Best Practices](https://docs.microsoft.com/en-us/ef/core/performance/)
- [Query Performance Tuning](https://docs.microsoft.com/en-us/ef/core/performance/efficient-querying)

**Our Project Files:**
- `src/DbDemo.Infrastructure.EFCore/` (Implementation)
- `docs/27-transactions-and-architecture.md` (ADO.NET comparison)
- `docs/28-scaffolding-and-query-builders.md` (SqlKata comparison)

---

**Last Updated:** 2025-11-03
**Project:** DbDemo - Database Demonstration Project
**Author:** University Database Course Materials
