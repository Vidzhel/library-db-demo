# Database Scaffolding and Query Builders with SqlKata

This document explains the database scaffolding approach and SqlKata query builder integration, demonstrating how to achieve compile-time safety for database queries without using a full ORM.

## Table of Contents
- [Overview](#overview)
- [The Problem: Magic Strings](#the-problem-magic-strings)
- [The Solution: Scaffolding + Query Builder](#the-solution-scaffolding--query-builder)
- [Architecture](#architecture)
- [Scaffolding Tool](#scaffolding-tool)
- [Generated Schema Code](#generated-schema-code)
- [SqlKata Query Builder](#sqlkata-query-builder)
- [Repository Implementation Examples](#repository-implementation-examples)
- [Setup Workflow](#setup-workflow)
- [Compile-Time Safety Demo](#compile-time-safety-demo)
- [Comparison: ADO.NET vs SqlKata](#comparison-adonet-vs-sqlkata)
- [When to Use Each Approach](#when-to-use-each-approach)
- [Completing Remaining Repositories](#completing-remaining-repositories)

---

## Overview

This implementation demonstrates a **middle ground** between raw ADO.NET and full ORMs:

- **Migration SQL files** remain the single source of truth
- **Scaffolding tool** extracts schema metadata and generates compile-time constants
- **SqlKata query builder** provides fluent API with type safety
- **No magic behavior** - still explicit queries, just better tooling

### Key Benefits

✅ **Compile-Time Safety**: Column renames in migrations → Regenerate → Build errors guide refactoring
✅ **IntelliSense Support**: Autocomplete for table and column names
✅ **Query Builder Ergonomics**: Fluent API vs string concatenation
✅ **Migration-First**: SQL migrations control schema, not code
✅ **No ORM Overhead**: No change tracking, no lazy loading surprises

---

## The Problem: Magic Strings

### ADO.NET Approach (Current)

```csharp
const string sql = @"
    SELECT Id, ISBN, Title, CategoryId, IsDeleted
    FROM Books
    WHERE CategoryId = @CategoryId
      AND IsDeleted = 0
    ORDER BY Title;";
```

**Problems:**
- Typo in column name? Runtime error
- Renamed column in migration? Find-and-replace across codebase
- No IntelliSense or autocomplete
- Easy to miss references during refactoring

---

## The Solution: Scaffolding + Query Builder

### Step 1: Migrations Define Schema

```sql
-- migrations/V001__initial_schema.sql
CREATE TABLE Books (
    Id INT PRIMARY KEY IDENTITY,
    ISBN NVARCHAR(20) NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    CategoryId INT NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
    -- ...
);
```

### Step 2: Scaffolding Generates Constants

```bash
dotnet run --project src/DbDemo.Scaffolding
```

Generates `Tables.cs` and `Columns.cs`:

```csharp
// Generated/Tables.cs
public static class Tables
{
    public const string Books = "Books";
    public const string Authors = "Authors";
    // ...
}

// Generated/Columns.cs
public static class Columns
{
    public static class Books
    {
        /// <summary>int (not null)</summary>
        public const string Id = "Id";

        /// <summary>nvarchar(20) (not null)</summary>
        public const string ISBN = "ISBN";

        /// <summary>nvarchar(200) (not null)</summary>
        public const string Title = "Title";

        /// <summary>int (not null)</summary>
        public const string CategoryId = "CategoryId";

        /// <summary>bit (not null)</summary>
        public const string IsDeleted = "IsDeleted";
        // ...
    }
}
```

### Step 3: SqlKata Query Builder

```csharp
var factory = QueryFactoryProvider.Create(transaction);

var results = await factory
    .Query(Tables.Books)                      // ← Compile-time checked!
    .Select(Columns.Books.Id,                  // ← Compile-time checked!
            Columns.Books.ISBN,
            Columns.Books.Title,
            Columns.Books.CategoryId)
    .Where(Columns.Books.CategoryId, categoryId)  // ← Compile-time checked!
    .Where(Columns.Books.IsDeleted, false)
    .OrderBy(Columns.Books.Title)
    .GetAsync<dynamic>(transaction: transaction);
```

**Benefits:**
- Typo in `Columns.Books.Titel`? **Compile error!**
- Renamed column? Regenerate constants → **Build fails** → Compiler guides you
- IntelliSense autocompletes table/column names
- Refactoring tools work correctly

---

## Architecture

```
                                    ┌─────────────────────────┐
                                    │   Migration SQL Files   │
                                    │  (Source of Truth)      │
                                    └───────────┬─────────────┘
                                                │
                            ┌───────────────────┴────────────────────┐
                            │                                        │
                    ┌───────▼─────────┐                      ┌───────▼────────┐
                    │  MigrationRunner│                      │  DbDemo.       │
                    │  (Applies DDL)  │                      │  Scaffolding   │
                    └───────┬─────────┘                      │  Tool          │
                            │                                └───────┬────────┘
                            │                                        │
                    ┌───────▼─────────┐                              │
                    │   SQL Server    │◄────queries──────────────────┘
                    │   (LibraryDb)   │  INFORMATION_SCHEMA          │
                    └─────────────────┘                              │
                                                                     │
                                                            ┌────────▼────────┐
                                                            │  Generated/     │
                                                            │  - Tables.cs    │
                                                            │  - Columns.cs   │
                                                            └────────┬────────┘
                                                                     │
                                    ┌────────────────────────────────┘
                                    │
                    ┌───────────────▼─────────────────┐
                    │  SqlKata Repository             │
                    │  Implementations                │
                    │  - Uses Tables.* & Columns.*    │
                    │  - QueryFactory for fluent API  │
                    └────────────────┬────────────────┘
                                     │
                            ┌────────▼──────────┐
                            │  Application      │
                            │  (Via Interfaces) │
                            └───────────────────┘
```

---

## Understanding INFORMATION_SCHEMA

### What is INFORMATION_SCHEMA?

`INFORMATION_SCHEMA` is a **standard SQL schema** that exists in all major relational databases (SQL Server, PostgreSQL, MySQL, etc.). It provides **metadata views** about the database structure.

Think of it as the **"database's database"** - it stores information about:
- Tables and their properties
- Columns and their data types
- Constraints (primary keys, foreign keys, unique constraints)
- Views and their definitions
- Stored procedures and functions
- Indexes and statistics
- Permissions and grants

### Key INFORMATION_SCHEMA Views

#### 1. INFORMATION_SCHEMA.TABLES
Lists all tables and views in the database.

```sql
SELECT TABLE_CATALOG, TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_TYPE = 'BASE TABLE';
```

**Columns:**
- `TABLE_CATALOG` - Database name
- `TABLE_SCHEMA` - Schema name (usually 'dbo')
- `TABLE_NAME` - Name of the table
- `TABLE_TYPE` - 'BASE TABLE' or 'VIEW'

#### 2. INFORMATION_SCHEMA.COLUMNS
Lists all columns for all tables.

```sql
SELECT TABLE_NAME, COLUMN_NAME, ORDINAL_POSITION,
       DATA_TYPE, CHARACTER_MAXIMUM_LENGTH,
       IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
ORDER BY TABLE_NAME, ORDINAL_POSITION;
```

**Columns:**
- `COLUMN_NAME` - Name of the column
- `ORDINAL_POSITION` - Column position in table (1-based)
- `DATA_TYPE` - SQL data type (int, nvarchar, datetime2, etc.)
- `CHARACTER_MAXIMUM_LENGTH` - Max length for string types
- `NUMERIC_PRECISION` - Precision for numeric types
- `IS_NULLABLE` - 'YES' or 'NO'
- `COLUMN_DEFAULT` - Default value expression

#### 3. INFORMATION_SCHEMA.KEY_COLUMN_USAGE
Lists primary key and foreign key columns.

```sql
SELECT TABLE_NAME, COLUMN_NAME, CONSTRAINT_NAME
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'dbo';
```

#### 4. INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS
Lists foreign key relationships.

```sql
SELECT CONSTRAINT_NAME,
       UNIQUE_CONSTRAINT_NAME,
       UPDATE_RULE,
       DELETE_RULE
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS;
```

#### 5. Other Useful Views
- `INFORMATION_SCHEMA.TABLE_CONSTRAINTS` - All constraints (PK, FK, UNIQUE, CHECK)
- `INFORMATION_SCHEMA.ROUTINES` - Stored procedures and functions
- `INFORMATION_SCHEMA.PARAMETERS` - Stored procedure parameters
- `INFORMATION_SCHEMA.VIEWS` - View definitions

### What Can You Use INFORMATION_SCHEMA For?

#### 1. Schema Documentation Generation (Our Use Case)
Generate code, documentation, or diagrams from database schema.

#### 2. Schema Comparison
Compare two databases to find differences:

```sql
-- Find tables that exist in DB1 but not DB2
SELECT TABLE_NAME
FROM [Database1].INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME NOT IN (
    SELECT TABLE_NAME
    FROM [Database2].INFORMATION_SCHEMA.TABLES
);
```

#### 3. Dependency Analysis
Find which tables reference a specific table:

```sql
-- Find all foreign keys pointing to 'Books' table
SELECT
    fk.TABLE_NAME as ReferencingTable,
    fk.COLUMN_NAME as ReferencingColumn
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE fk
    ON rc.CONSTRAINT_NAME = fk.CONSTRAINT_NAME
INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE pk
    ON rc.UNIQUE_CONSTRAINT_NAME = pk.CONSTRAINT_NAME
WHERE pk.TABLE_NAME = 'Books';
```

#### 4. Dynamic SQL Generation
Generate SQL statements based on schema:

```sql
-- Generate SELECT statements for all tables
SELECT 'SELECT * FROM ' + TABLE_NAME + ';' as GeneratedSQL
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE';
```

#### 5. Data Type Auditing
Find all columns of a specific type:

```sql
-- Find all NVARCHAR(MAX) columns
SELECT TABLE_NAME, COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE DATA_TYPE = 'nvarchar'
  AND CHARACTER_MAXIMUM_LENGTH = -1;  -- -1 means MAX
```

### INFORMATION_SCHEMA vs System Catalog Views

SQL Server also has **system catalog views** (like `sys.tables`, `sys.columns`) that provide more detailed metadata:

| Feature | INFORMATION_SCHEMA | System Catalog (sys.*) |
|---------|-------------------|----------------------|
| Standard | ✅ ANSI SQL Standard | ❌ SQL Server specific |
| Portability | ✅ Works across databases | ❌ Database-specific |
| Detail level | Basic metadata | Detailed, SQL Server-specific |
| Performance | Good | Often better |

**For our scaffolding tool**, we use `INFORMATION_SCHEMA` because:
- ✅ Portable across different databases (could support PostgreSQL later)
- ✅ Standard SQL - easier to understand
- ✅ Sufficient detail for generating constants

---

## Scaffolding Tool

### Project: `DbDemo.Scaffolding`

#### Purpose
Queries `INFORMATION_SCHEMA` to extract:
- All table names
- Column names, data types, nullability, max lengths

Then generates C# constants for compile-time checking.

#### Key Files

**SchemaReader.cs** - Queries database metadata
```csharp
public async Task<List<TableSchema>> ReadSchemaAsync()
{
    const string query = @"
        SELECT t.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE,
               c.IS_NULLABLE, c.CHARACTER_MAXIMUM_LENGTH
        FROM INFORMATION_SCHEMA.TABLES t
        INNER JOIN INFORMATION_SCHEMA.COLUMNS c
            ON t.TABLE_NAME = c.TABLE_NAME
        WHERE t.TABLE_TYPE = 'BASE TABLE'
          AND t.TABLE_SCHEMA = 'dbo'
        ORDER BY t.TABLE_NAME, c.ORDINAL_POSITION";

    // Execute and parse...
}
```

**CodeGenerator.cs** - Generates `Tables.cs` and `Columns.cs`

**Program.cs** - Orchestrates: Read schema → Generate code → Write files

#### Usage

```bash
# From project root
dotnet run --project src/DbDemo.Scaffolding
```

Output:
```
═══════════════════════════════════════════════════════════
  Database Scaffolding Tool - DbDemo
═══════════════════════════════════════════════════════════

Reading database schema...
Found 10 tables with 115 total columns

Generating code to: /path/to/src/DbDemo.Infrastructure.SqlKata/Generated

✓ Generated .../Generated/Tables.cs
✓ Generated .../Generated/Columns.cs

Schema Summary:
  • Authors (10 columns)
  • Books (19 columns)
  • Categories (6 columns)
  • Loans (15 columns)
  • Members (16 columns)
  ...

✓ Scaffolding completed successfully!
```

---

## Generated Schema Code

### Tables.cs

```csharp
// <auto-generated />
namespace DbDemo.Infrastructure.SqlKata.Generated;

public static class Tables
{
    /// <summary>Table: Authors</summary>
    public const string Authors = "Authors";

    /// <summary>Table: Books</summary>
    public const string Books = "Books";

    /// <summary>Table: Categories</summary>
    public const string Categories = "Categories";

    // ...
}
```

### Columns.cs

```csharp
// <auto-generated />
namespace DbDemo.Infrastructure.SqlKata.Generated;

public static class Columns
{
    /// <summary>Columns for Books table</summary>
    public static class Books
    {
        /// <summary>int (not null)</summary>
        public const string Id = "Id";

        /// <summary>nvarchar(20) (not null)</summary>
        public const string ISBN = "ISBN";

        /// <summary>nvarchar(200) (not null)</summary>
        public const string Title = "Title";

        /// <summary>nvarchar(200) (nullable)</summary>
        public const string Subtitle = "Subtitle";

        /// <summary>int (not null)</summary>
        public const string CategoryId = "CategoryId";

        /// <summary>bit (not null)</summary>
        public const string IsDeleted = "IsDeleted";

        /// <summary>nvarchar(-1) (nullable)</summary>
        public const string Metadata = "Metadata";

        // ... all columns
    }

    /// <summary>Columns for Authors table</summary>
    public static class Authors { /* ... */ }

    // ... all tables
}
```

---

## SqlKata Query Builder

### What is SqlKata?

SqlKata is a **query builder** library (not an ORM):
- Fluent API for building SQL queries
- Compiles to raw SQL for various databases (SQL Server, PostgreSQL, MySQL, etc.)
- No entities, no change tracking, no lazy loading
- Transaction support via `QueryFactory`

### QueryFactoryProvider

```csharp
public static class QueryFactoryProvider
{
    public static QueryFactory Create(SqlTransaction transaction)
    {
        var compiler = new SqlServerCompiler();
        return new QueryFactory(transaction.Connection, compiler);
    }
}
```

**Usage:**
```csharp
var factory = QueryFactoryProvider.Create(transaction);
```

### Common Query Patterns

#### Simple SELECT
```csharp
var book = await factory
    .Query(Tables.Books)
    .Select(Columns.Books.Id, Columns.Books.Title)
    .Where(Columns.Books.Id, bookId)
    .FirstOrDefaultAsync<dynamic>(transaction: transaction);
```

#### Pagination
```csharp
var books = await factory
    .Query(Tables.Books)
    .Select(/* columns */)
    .Where(Columns.Books.IsDeleted, false)
    .OrderBy(Columns.Books.Title)
    .ForPage(pageNumber, pageSize)  // OFFSET-FETCH
    .GetAsync<dynamic>(transaction: transaction);
```

#### Search with LIKE
```csharp
var books = await factory
    .Query(Tables.Books)
    .WhereContains(Columns.Books.Title, searchTerm)  // LIKE '%searchTerm%'
    .GetAsync<dynamic>(transaction: transaction);
```

#### Count
```csharp
var count = await factory
    .Query(Tables.Books)
    .Where(Columns.Books.IsDeleted, false)
    .CountAsync<int>(transaction: transaction);
```

#### Insert
```csharp
var insertData = new Dictionary<string, object?>
{
    [Columns.Books.ISBN] = "978-0-123456-78-9",
    [Columns.Books.Title] = "Domain-Driven Design",
    [Columns.Books.CategoryId] = 5
};

// Note: For SCOPE_IDENTITY() with triggers, use raw SQL
var query = factory.Query(Tables.Books).AsInsert(insertData);
var sql = factory.Compiler.Compile(query).Sql + "; SELECT CAST(SCOPE_IDENTITY() AS INT);";
// Execute with parameters...
```

#### Update
```csharp
var updateData = new Dictionary<string, object?>
{
    [Columns.Books.Title] = "Updated Title",
    [Columns.Books.UpdatedAt] = DateTime.UtcNow
};

var affectedRows = await factory
    .Query(Tables.Books)
    .Where(Columns.Books.Id, bookId)
    .UpdateAsync(updateData, transaction: transaction);
```

---

## Repository Implementation Examples

### Full Example: BookRepository with SqlKata

```csharp
using DbDemo.Infrastructure.SqlKata.Generated;
using SqlKata.Execution;

public class BookRepository : IBookRepository
{
    public async Task<Book?> GetByIdAsync(int id, SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var factory = QueryFactoryProvider.Create(transaction);

        var result = await factory
            .Query(Tables.Books)
            .Select(GetBookColumns())
            .Where(Columns.Books.Id, id)
            .FirstOrDefaultAsync<dynamic>(transaction: transaction,
                                          cancellationToken: cancellationToken);

        return result != null ? MapDynamicToBook(result) : null;
    }

    public async Task<List<Book>> GetPagedAsync(int pageNumber, int pageSize,
        bool includeDeleted, SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var factory = QueryFactoryProvider.Create(transaction);

        var query = factory
            .Query(Tables.Books)
            .Select(GetBookColumns());

        if (!includeDeleted)
        {
            query = query.Where(Columns.Books.IsDeleted, false);
        }

        var results = await query
            .OrderBy(Columns.Books.Title)
            .ForPage(pageNumber, pageSize)
            .GetAsync<dynamic>(transaction: transaction,
                              cancellationToken: cancellationToken);

        var books = new List<Book>();
        foreach (var result in results)
        {
            books.Add(MapDynamicToBook(result));
        }
        return books;
    }

    private static string[] GetBookColumns() => new[]
    {
        Columns.Books.Id,
        Columns.Books.ISBN,
        Columns.Books.Title,
        Columns.Books.Subtitle,
        Columns.Books.CategoryId,
        // ... all columns using generated constants
    };

    private static Book MapDynamicToBook(dynamic row)
    {
        return Book.FromDatabase(
            id: (int)row.Id,
            isbn: (string)row.ISBN,
            title: (string)row.Title,
            subtitle: row.Subtitle,
            categoryId: (int)row.CategoryId,
            // ... map all fields
        );
    }
}
```

### When SqlKata Can't Help: Raw SQL with Parameters

For complex queries (JSON, CTEs, window functions), fall back to raw SQL:

```csharp
public async Task<List<Book>> GetBooksByTagAsync(string tag,
    SqlTransaction transaction, CancellationToken cancellationToken = default)
{
    // SqlKata doesn't have OPENJSON support - use raw SQL
    const string sql = @"
        SELECT DISTINCT b.Id, b.ISBN, b.Title, b.CategoryId, b.Metadata
        FROM Books b
        CROSS APPLY OPENJSON(b.Metadata, '$.tags') AS tags
        WHERE tags.[value] = @Tag
          AND b.IsDeleted = 0
        ORDER BY b.Title;";

    await using var command = new SqlCommand(sql, transaction.Connection, transaction);
    command.Parameters.Add("@Tag", SqlDbType.NVarChar, 50).Value = tag;

    await using var reader = await command.ExecuteReaderAsync(cancellationToken);

    var books = new List<Book>();
    while (await reader.ReadAsync(cancellationToken))
    {
        books.Add(MapReaderToBook(reader));
    }
    return books;
}
```

**Note:** Still use parameterized queries for security! You may still use your table and column constants.

---

## Setup Workflow

### Project: `DbDemo.Setup`

Combines migration running and scaffolding into a single command.

#### Usage

```bash
# From project root
dotnet run --project src/DbDemo.Setup
```

#### What It Does

1. **Runs Migrations**
   - Uses `MigrationRunner` from `DbDemo.Infrastructure`
   - Applies any new migration files
   - Reports how many migrations executed

2. **Runs Scaffolding**
   - Launches `DbDemo.Scaffolding` project
   - Regenerates `Tables.cs` and `Columns.cs`
   - Updates schema constants based on current database

#### Output Example

```
═══════════════════════════════════════════════════════════
  Database Setup Tool - DbDemo
  Runs Migrations + Scaffolding
═══════════════════════════════════════════════════════════

═══════════════════════════════════════════════════════════
Step 1: Running Database Migrations
═══════════════════════════════════════════════════════════

Checking for pending migrations...
✓ No new migrations to run (database is up to date)

═══════════════════════════════════════════════════════════
Step 2: Running Database Scaffolding
═══════════════════════════════════════════════════════════

Reading database schema...
Found 10 tables with 115 total columns
✓ Generated Tables.cs
✓ Generated Columns.cs

═══════════════════════════════════════════════════════════
✓ Setup Completed Successfully!
═══════════════════════════════════════════════════════════

Next steps:
  1. Build the solution to verify generated schema code compiles
  2. Run the ConsoleApp to choose between ADO.NET or SqlKata repositories
```

---

## Compile-Time Safety Demo

### Scenario: Renaming a Column

**Step 1:** Create migration to rename `Title` → `BookTitle`

```sql
-- migrations/V022__rename_title_column.sql
EXEC sp_rename 'Books.Title', 'BookTitle', 'COLUMN';
```

**Step 2:** Run Setup

```bash
dotnet run --project src/DbDemo.Setup
```

This:
1. Applies migration (column renamed in database)
2. Regenerates `Columns.Books.BookTitle` (was `Columns.Books.Title`)

**Step 3:** Try to Build

```bash
dotnet build
```

**Result:** Build fails with errors like:

```
error CS0117: 'Columns.Books' does not contain a definition for 'Title'
  at DbDemo.Infrastructure.SqlKata/Repositories/BookRepository.cs:142
  at DbDemo.Infrastructure.SqlKata/Repositories/BookRepository.cs:278
```

**Step 4:** Fix Code

Use IDE's "Find All References" or compiler errors to update:

```csharp
// Before
.OrderBy(Columns.Books.Title)

// After
.OrderBy(Columns.Books.BookTitle)
```

**Step 5:** Build Succeeds

All references updated, no runtime errors!

---

## Comparison: ADO.NET vs SqlKata

### ADO.NET Approach

```csharp
public async Task<List<Book>> GetByCategoryAsync(int categoryId,
    SqlTransaction transaction, CancellationToken cancellationToken)
{
    const string sql = @"
        SELECT Id, ISBN, Title, Subtitle, CategoryId, IsDeleted
        FROM Books
        WHERE CategoryId = @CategoryId
          AND IsDeleted = 0
        ORDER BY Title;";

    var connection = transaction.Connection;
    await using var command = new SqlCommand(sql, connection, transaction);
    command.Parameters.Add("@CategoryId", SqlDbType.Int).Value = categoryId;

    await using var reader = await command.ExecuteReaderAsync(cancellationToken);

    var books = new List<Book>();
    while (await reader.ReadAsync(cancellationToken))
    {
        books.Add(MapReaderToBook(reader));
    }
    return books;
}
```

**Pros:**
- Full control over SQL
- No dependencies beyond Microsoft.Data.SqlClient
- Familiar to SQL developers

**Cons:**
- Magic strings (typos = runtime errors)
- Manual parameter setup
- No compile-time checking
- Refactoring is error-prone

### SqlKata Approach

```csharp
public async Task<List<Book>> GetByCategoryAsync(int categoryId,
    SqlTransaction transaction, CancellationToken cancellationToken)
{
    var factory = QueryFactoryProvider.Create(transaction);

    var results = await factory
        .Query(Tables.Books)
        .Select(GetBookColumns())
        .Where(Columns.Books.CategoryId, categoryId)
        .Where(Columns.Books.IsDeleted, false)
        .OrderBy(Columns.Books.Title)
        .GetAsync<dynamic>(transaction: transaction,
                          cancellationToken: cancellationToken);

    var books = new List<Book>();
    foreach (var result in results)
    {
        books.Add(MapDynamicToBook(result));
    }
    return books;
}
```

**Pros:**
- Compile-time checking (Columns.Books.Title)
- IntelliSense autocomplete
- Fluent, readable API
- Refactoring-safe

**Cons:**
- Additional dependency (SqlKata)
- Learning curve for query builder API
- Complex queries may require raw SQL anyway

---

## When to Use Each Approach

### Use ADO.NET When:
- Complex SQL (CTEs, window functions, JSON queries, spatial queries)
- Stored procedures or table-valued parameters
- Performance-critical queries needing exact SQL control
- Team is SQL-first and prefers raw queries

### Use SqlKata When:
- Standard CRUD operations
- Simple queries (SELECT, INSERT, UPDATE, DELETE)
- Pagination, filtering, ordering
- Want compile-time safety and refactoring support
- Team prefers fluent API over SQL strings

### Hybrid Approach (Recommended)
Use **both** in the same repository:
- SqlKata for simple queries
- ADO.NET for complex queries

Example from `BookRepository.cs`:
```csharp
// Simple query: SqlKata
public async Task<Book?> GetByIdAsync(...)
{
    var factory = QueryFactoryProvider.Create(transaction);
    return await factory.Query(Tables.Books)...
}

// Complex query: Raw SQL with parameters
public async Task<List<Book>> GetBooksByTagAsync(string tag, ...)
{
    const string sql = @"SELECT ... FROM Books b CROSS APPLY OPENJSON(...) ...";
    await using var command = new SqlCommand(sql, transaction.Connection, transaction);
    // ...
}
```

---

## Completing Remaining Repositories

### Pattern to Follow

All repositories follow the same structure demonstrated in `BookRepository.cs` and `MemberRepository.cs`:

#### 1. Constructor (No State)
```csharp
public class AuthorRepository : IAuthorRepository
{
    public AuthorRepository() { }
}
```

#### 2. Create Method
```csharp
public async Task<Author> CreateAsync(Author author, SqlTransaction transaction, ...)
{
    var factory = QueryFactoryProvider.Create(transaction);

    var insertData = new Dictionary<string, object?>
    {
        [Columns.Authors.FirstName] = author.FirstName,
        [Columns.Authors.LastName] = author.LastName,
        // ...
    };

    // Use raw SQL for SCOPE_IDENTITY() due to triggers
    var query = factory.Query(Tables.Authors).AsInsert(insertData);
    var sql = factory.Compiler.Compile(query).Sql + "; SELECT CAST(SCOPE_IDENTITY() AS INT);";
    var bindings = factory.Compiler.Compile(query).Bindings;

    await using var command = new SqlCommand(sql, transaction.Connection, transaction);
    for (int i = 0; i < bindings.Count; i++)
    {
        command.Parameters.AddWithValue($"@p{i}", bindings[i] ?? DBNull.Value);
    }

    var newId = (int)await command.ExecuteScalarAsync(cancellationToken);
    return await GetByIdAsync(newId, transaction, cancellationToken)
        ?? throw new InvalidOperationException("Failed to retrieve newly created author");
}
```

#### 3. Read Methods (GetById, GetPaged, Search)
```csharp
public async Task<Author?> GetByIdAsync(int id, SqlTransaction transaction, ...)
{
    var factory = QueryFactoryProvider.Create(transaction);

    var result = await factory
        .Query(Tables.Authors)
        .Select(GetAuthorColumns())
        .Where(Columns.Authors.Id, id)
        .FirstOrDefaultAsync<dynamic>(transaction: transaction,
                                      cancellationToken: cancellationToken);

    return result != null ? MapDynamicToAuthor(result) : null;
}
```

#### 4. Update Method
```csharp
public async Task<bool> UpdateAsync(Author author, SqlTransaction transaction, ...)
{
    var factory = QueryFactoryProvider.Create(transaction);

    var updateData = new Dictionary<string, object?>
    {
        [Columns.Authors.FirstName] = author.FirstName,
        [Columns.Authors.LastName] = author.LastName,
        [Columns.Authors.UpdatedAt] = DateTime.UtcNow,
        // ...
    };

    var affectedRows = await factory
        .Query(Tables.Authors)
        .Where(Columns.Authors.Id, author.Id)
        .UpdateAsync(updateData, transaction: transaction,
                    cancellationToken: cancellationToken);

    return affectedRows > 0;
}
```

#### 5. Helper Methods
```csharp
private static string[] GetAuthorColumns() => new[]
{
    Columns.Authors.Id,
    Columns.Authors.FirstName,
    Columns.Authors.LastName,
    Columns.Authors.Email,
    // ... all columns using generated constants
};

private static Author MapDynamicToAuthor(dynamic row)
{
    return Author.FromDatabase(
        id: (int)row.Id,
        firstName: (string)row.FirstName,
        lastName: (string)row.LastName,
        email: row.Email,
        // ... map all fields
    );
}
```

### Repositories to Implement

Following this pattern, implement:
- [x] `BookRepository` - ✅ Complete (example)
- [x] `MemberRepository` - ✅ Partial (example)
- [ ] `AuthorRepository`
- [ ] `LoanRepository`
- [ ] `CategoryRepository`
- [ ] `BookAuditRepository`
- [ ] `LibraryBranchRepository`
- [ ] `ReportRepository`
- [ ] `SystemStatisticsRepository`

**Tip:** Use `BookRepository.cs` as a reference - it demonstrates all common patterns.

---

## Next Steps

### 1. Complete Repository Implementations

Implement remaining repositories in `DbDemo.Infrastructure.SqlKata/Repositories/` following the established pattern.

### 2. Add Repository Provider Selection to ConsoleApp

Update `Program.cs` to allow choosing between ADO.NET and SqlKata implementations:

```csharp
private static void ChooseRepositoryProvider()
{
    Console.WriteLine("Select Repository Provider:");
    Console.WriteLine("  1. ADO.NET (raw SQL)");
    Console.WriteLine("  2. SqlKata (query builder with compile-time safety)");
    Console.Write("Choice: ");

    var choice = Console.ReadLine();

    if (choice == "2")
    {
        InitializeSqlKataRepositories();
    }
    else
    {
        InitializeAdoNetRepositories();
    }
}

private static void InitializeSqlKataRepositories()
{
    _bookRepository = new DbDemo.Infrastructure.SqlKata.Repositories.BookRepository();
    _memberRepository = new DbDemo.Infrastructure.SqlKata.Repositories.MemberRepository();
    // ...
}
```

### 3. Test Both Implementations

Run demos with both repository providers to verify:
- Same results
- Same transaction behavior
- Performance comparison

### 4. Document Performance Characteristics

Benchmark ADO.NET vs SqlKata for common operations.

### 5. Consider Caching Generated Constants

For production, consider pre-compiling the scaffolded constants rather than regenerating on every schema change.

---

## Summary

This implementation provides a **practical middle ground** between raw ADO.NET and full ORMs:

| Feature | ADO.NET | SqlKata + Scaffolding | Full ORM (EF Core) |
|---------|---------|----------------------|-------------------|
| Compile-time safety | ❌ | ✅ | ✅ |
| Migration-first | ✅ | ✅ | ❌ (code-first) |
| SQL control | ✅ Full | ✅ Hybrid | ❌ Limited |
| IntelliSense | ❌ | ✅ | ✅ |
| Learning curve | Low | Medium | High |
| Dependencies | Minimal | +SqlKata | +EF Core |
| Query complexity | Any | Simple→Medium | Simple→Medium |
| Performance | Optimal | Very Good | Good |
| Change tracking | Manual | Manual | Automatic |

### When This Approach Shines

✅ Schema changes frequently (migrations define structure)
✅ Want compile-time safety without ORM overhead
✅ Team comfortable with SQL but wants better tooling
✅ Need explicit control over queries
✅ Want to avoid ORM pitfalls (N+1, lazy loading, etc.)

### When to Use Full ORM Instead

❌ Prefer code-first (generate DB from models)
❌ Want automatic change tracking
❌ Need extensive relationship navigation
❌ Team prefers LINQ over SQL

---

**Generated by Claude Code**
Documentation Version: 1.0
Last Updated: 2025-11-02
