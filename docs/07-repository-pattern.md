# 07 - Repository Pattern with ADO.NET

## 📖 What You'll Learn

- Repository pattern for data access abstraction
- Proper parameterized queries to prevent SQL injection
- Resource disposal with `using` and `await using` statements
- Async/await patterns with CancellationToken
- Explicit column selection (avoiding SELECT *)
- Paging with OFFSET-FETCH
- Integration testing against real databases

## 🎯 Why This Matters

[Read this](https://dev.to/jnavez/make-your-microservices-tastier-by-cooking-them-with-a-sweet-onion-34n2)

The Repository Pattern is a fundamental design pattern that separates domain/business logic from data access logic. This provides several critical benefits:

### Without Repository Pattern
```csharp
// Business logic mixed with data access - ❌ BAD
public async Task<Book?> GetBookForCheckout(int bookId)
{
    // SQL scattered throughout application
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    var sql = "SELECT * FROM Books WHERE Id = " + bookId; // SQL injection vulnerability!
    // ... data reader code ...
    // Business logic intertwined with SQL
}
```

### With Repository Pattern
```csharp
// Clean separation - ✅ GOOD
public async Task<Book?> GetBookForCheckout(int bookId)
{
    // Business logic uses abstraction
    var book = await _bookRepository.GetByIdAsync(bookId);
    if (book == null || !book.IsAvailable)
        return null;
    return book;
}
```

### Benefits
- **Testability**: Mock the repository in unit tests
- **Maintainability**: All SQL in one place
- **Security**: Centralized parameterized query enforcement
- **Flexibility**: Swap data access strategies without changing business logic
- **DRY**: No duplicate SQL scattered across codebase

## 🏗️ Architecture Overview

### Interface-Based Design

```csharp
public interface IBookRepository
{
    Task<Book> CreateAsync(Book book, CancellationToken cancellationToken = default);
    Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Book>> GetPagedAsync(int pageNumber = 1, int pageSize = 10, ...);
    Task<bool> UpdateAsync(Book book, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    // ... more methods
}
```

**Why interfaces?**
- Enables dependency injection
- Facilitates testing with mocks
- Supports multiple implementations (SQL Server, PostgreSQL, in-memory, etc.)
- Enforces contract between layers

### Implementation Structure

```
BookRepository (concrete class)
├── Constructor (takes connection string)
├── CRUD Methods
│   ├── CreateAsync
│   ├── GetByIdAsync
│   ├── UpdateAsync
│   └── DeleteAsync
├── Query Methods
│   ├── GetPagedAsync
│   ├── SearchByTitleAsync
│   └── GetByCategoryAsync
└── Helper Methods
    ├── AddBookParameters
    ├── MapReaderToBook
    └── SetPropertyValue/SetBackingField (reflection)
```

## 🔒 Security: Parameterized Queries

### ⚠️ SQL Injection Vulnerability

**NEVER do this:**
```csharp
// ❌ EXTREMELY DANGEROUS
var sql = $"SELECT * FROM Books WHERE Title = '{searchTerm}'";
```

**Why it's dangerous:**
```csharp
// User input: "'; DROP TABLE Books; --"
// Resulting SQL:
// SELECT * FROM Books WHERE Title = ''; DROP TABLE Books; --'
// 💥 YOUR DATABASE IS GONE
```

### ✅ Safe Parameterized Queries

**Our implementation:**
```csharp
const string sql = @"
    SELECT Id, ISBN, Title, ...
    FROM Books
    WHERE Title LIKE @SearchPattern;";

command.Parameters.Add("@SearchPattern", SqlDbType.NVarChar, 202).Value = $"%{searchTerm}%";
```

**How parameters prevent injection:**
1. SQL Server treats parameter values as **data**, not **code**
2. Special characters are properly escaped
3. Type checking enforces data integrity
4. Query plan caching improves performance

**Parameter creation:**
```csharp
// Method 1: Add with explicit type (RECOMMENDED)
command.Parameters.Add("@ISBN", SqlDbType.NVarChar, 20).Value = book.ISBN;

// Method 2: AddWithValue (less explicit, but convenient)
command.Parameters.AddWithValue("@CategoryId", book.CategoryId);

// Handling NULL values
command.Parameters.Add("@Subtitle", SqlDbType.NVarChar, 200).Value =
    (object?)book.Subtitle ?? DBNull.Value;
```

## 🗑️ Resource Management: Using Statements

### The Problem: Resource Leaks

ADO.NET objects hold **unmanaged resources** (database connections, network sockets):
- If not disposed, they leak memory
- Connection pool exhaustion causes application failure
- Database holds locks longer than necessary

### ❌ Without Proper Disposal

```csharp
// ❌ BAD - connection never disposed
var connection = new SqlConnection(connectionString);
connection.Open();
// ... if exception occurs, connection leaks!
```

### ✅ With Using Statements

**Sync version (old):**
```csharp
using (var connection = new SqlConnection(connectionString))
{
    connection.Open();
    // ... connection automatically disposed even if exception occurs
}
```

**Async version (modern):**
```csharp
await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync(cancellationToken);
// ... connection automatically disposed with await DisposeAsync()
```

### Chaining Using Statements

**Our pattern:**
```csharp
await using var connection = new SqlConnection(_connectionString);
await connection.OpenAsync(cancellationToken);

await using var command = new SqlCommand(sql, connection);
// Add parameters...

await using var reader = await command.ExecuteReaderAsync(cancellationToken);
// Read data...

// All three automatically disposed in reverse order:
// 1. reader
// 2. command
// 3. connection
```

**Why `await using`?**
- Calls `DisposeAsync()` instead of `Dispose()`
- Properly handles asynchronous cleanup
- Required for truly async resource disposal

## 🔍 Explicit Column Selection

### ❌ SELECT * Anti-Pattern

```csharp
const string sql = "SELECT * FROM Books"; // ❌ BAD
```

**Problems with SELECT *:**
1. **Performance**: Fetches columns you don't need
2. **Network overhead**: Transfers unnecessary data
3. **Breaking changes**: Adding columns breaks code
4. **Unclear dependencies**: What columns does this code actually use?
5. **Index coverage**: May prevent index-only scans

### ✅ Explicit Columns

```csharp
const string sql = @"
    SELECT
        Id, ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
        PageCount, Language, CategoryId, TotalCopies, AvailableCopies,
        ShelfLocation, IsDeleted, CreatedAt, UpdatedAt
    FROM Books
    WHERE Id = @Id;";
```

**Benefits:**
- Clear which columns are needed
- Better query performance
- Easier to maintain
- Self-documenting code

## 📄 Pagination with OFFSET-FETCH

[Read this](https://medium.com/better-programming/understanding-the-offset-and-cursor-pagination-8ddc54d10d98)

### Why Pagination?

Loading all records is impractical for large datasets:
- Millions of books would exhaust memory
- Slow query execution
- Poor user experience

### SQL Server OFFSET-FETCH (SQL Server 2012+)

```csharp
public async Task<List<Book>> GetPagedAsync(int pageNumber = 1, int pageSize = 10, ...)
{
    if (pageNumber < 1) throw new ArgumentException("Page number must be >= 1");
    if (pageSize < 1) throw new ArgumentException("Page size must be >= 1");

    string sql = @"
        SELECT Id, ISBN, Title, ... FROM Books
        WHERE IsDeleted = 0
        ORDER BY Title  -- ⚠️ ORDER BY required for OFFSET-FETCH
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY;";

    command.Parameters.Add("@Offset", SqlDbType.Int).Value = (pageNumber - 1) * pageSize;
    command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
    // ...
}
```

**Example:**
- Page 1, Size 10: `OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY` (rows 1-10)
- Page 2, Size 10: `OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY` (rows 11-20)
- Page 3, Size 10: `OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY` (rows 21-30)

**Important Notes:**
- `ORDER BY` is **required** for deterministic results
- Consider adding total count query for UI (page X of Y)
- For very large offsets, consider keyset pagination instead

## 🔄 Async/Await with CancellationToken

### Why Async?

**Synchronous blocking:**
```csharp
var books = GetBooks(); // Thread blocked waiting for database
// Thread cannot do any other work
```

**Asynchronous non-blocking:**
```csharp
var books = await GetBooksAsync(cancellationToken); // Thread released
// Thread can handle other requests while waiting
```

**Benefits:**
- Better scalability (more concurrent requests)
- Responsive UI (doesn't freeze)
- Efficient resource utilization

### CancellationToken Pattern

```csharp
public async Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
{
    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync(cancellationToken); // ← Pass token

    await using var command = new SqlCommand(sql, connection);
    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

    await using var reader = await command.ExecuteReaderAsync(cancellationToken); // ← Pass token

    if (await reader.ReadAsync(cancellationToken)) // ← Pass token
    {
        return MapReaderToBook(reader);
    }

    return null;
}
```

**Usage:**
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
try
{
    var book = await repository.GetByIdAsync(123, cts.Token);
}
catch (OperationCanceledException)
{
    // Query was cancelled (timeout or user request)
}
```

## 🔧 Mapping: SqlDataReader to Entity

### The Challenge: Immutable Domain Models

Our `Book` entity has:
- Private setters for encapsulation
- Validation in setters
- Rich behavior methods

**Problem:** How do we reconstruct from database without bypassing validation?

### Our Solution: Reflection

```csharp
private static Book MapReaderToBook(SqlDataReader reader)
{
    // 1. Read all values from database
    var id = reader.GetInt32(0);
    var isbn = reader.GetString(1);
    var title = reader.GetString(2);
    // ... read all columns

    // 2. Create instance via private constructor using reflection
    var book = (Book)Activator.CreateInstance(typeof(Book), nonPublic: true)!;

    // 3. Set properties and backing fields directly
    SetPropertyValue(book, "Id", id);
    SetBackingField(book, "_isbn", isbn);
    SetBackingField(book, "_title", title);
    // ... set all fields

    return book;
}

private static void SetBackingField(object obj, string fieldName, object? value)
{
    var field = obj.GetType().GetField(fieldName,
        BindingFlags.NonPublic | BindingFlags.Instance);
    field?.SetValue(obj, value);
}
```

### Alternative Approaches (For Reference)

1. **Add internal constructor** for persistence layer:
```csharp
internal Book(int id, string isbn, string title, ...) // Used by repository only
```

2. **Factory method** in entity:
```csharp
internal static Book CreateFromDatabase(int id, string isbn, ...)
```

3. **Relax encapsulation** - make setters internal:
```csharp
public string Title { get; internal set; }
```

**Trade-offs:**
- Reflection: Works with any model, but slower and uses private details
- Internal constructor: Clean, but leaks persistence concerns into domain
- Factory method: Clean separation, but extra code
- Internal setters: Simple, but weakens encapsulation

> **Note:** In production, consider using an ORM (Entity Framework Core, Dapper) which solves mapping automatically. This demo uses reflection to show raw ADO.NET.

## 🧪 Integration Testing

### Why Integration Tests?

**Unit tests** verify business logic in isolation.
**Integration tests** verify actual database operations.

**What integration tests catch:**
- SQL syntax errors
- Incorrect column mappings
- NULL handling bugs
- Transaction behavior
- Performance issues
- Database constraint violations

### Test Structure

```csharp
[Collection("Database")]
public class BookRepositoryTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly BookRepository _repository;

    public async Task InitializeAsync()
    {
        // Clean database before EACH test
        await _fixture.CleanupTableAsync("Books");
    }

    [Fact]
    public async Task CreateAsync_ValidBook_ShouldInsertAndReturnBookWithId()
    {
        // Arrange
        var book = new Book("978-0132350884", "Clean Code", 1, 3);

        // Act
        var created = await _repository.CreateAsync(book);

        // Assert
        Assert.True(created.Id > 0);
        Assert.Equal("Clean Code", created.Title);
    }
}
```

### Test Fixture Pattern

**DatabaseTestFixture:**
- Shared across all tests in a class (`IClassFixture<T>`)
- Provides connection string from configuration
- Cleanup methods for test data isolation

**IAsyncLifetime:**
- `InitializeAsync()`: Runs before each test
- `DisposeAsync()`: Runs after each test
- Ensures test isolation

### Running Integration Tests

```bash
# Run all integration tests
dotnet test tests/DbDemo.Integration.Tests

# Run specific test
dotnet test --filter "FullyQualifiedName~CreateAsync_ValidBook"

# Run with verbosity
dotnet test --logger "console;verbosity=detailed"
```

## ⚠️ Common Pitfalls

### 1. Forgetting to Dispose Resources

**Problem:**
```csharp
var connection = new SqlConnection(connectionString);
connection.Open();
// Connection never disposed - memory leak!
```

**Solution:** Always use `await using`

### 2. SQL Injection via String Concatenation

**Problem:**
```csharp
var sql = $"SELECT * FROM Books WHERE Title = '{title}'"; // ❌ VULNERABLE
```

**Solution:** Always use parameterized queries

### 3. SELECT * Performance Issues

**Problem:**
```csharp
const string sql = "SELECT * FROM Books"; // Fetches all columns
```

**Solution:** Explicitly list needed columns

### 4. Incorrect NULL Handling

**Problem:**
```csharp
command.Parameters.Add("@Subtitle", SqlDbType.NVarChar).Value = book.Subtitle; // ❌ NullReferenceException
```

**Solution:**
```csharp
command.Parameters.Add("@Subtitle", SqlDbType.NVarChar).Value = (object?)book.Subtitle ?? DBNull.Value;
```

### 5. Missing CancellationToken Propagation

**Problem:**
```csharp
await connection.OpenAsync(); // ❌ Can't be cancelled
```

**Solution:**
```csharp
await connection.OpenAsync(cancellationToken); // ✅ Respects cancellation
```

### 6. Incorrect Ordinal Indexing

**Problem:**
```csharp
var id = reader.GetInt32(0);
var isbn = reader.GetString(1);
var title = reader.GetString(2);
// If column order changes in SELECT, this breaks!
```

**Solution:** Either:
- Keep column order consistent
- Use name-based access: `reader.GetString(reader.GetOrdinal("Title"))`
- Document column order clearly

## ✅ Best Practices

### 1. Use Constants for SQL

```csharp
private const string SQL_GET_BY_ID = @"
    SELECT Id, ISBN, Title, ...
    FROM Books
    WHERE Id = @Id;";
```

**Benefits:** Easier to find, update, and test SQL

### 2. Explicit Parameter Types

```csharp
// ✅ GOOD - explicit type and size
command.Parameters.Add("@ISBN", SqlDbType.NVarChar, 20).Value = isbn;

// ⚠️ OK - but less explicit
command.Parameters.AddWithValue("@ISBN", isbn);
```

### 3. Validate Input Early

```csharp
public async Task<List<Book>> GetPagedAsync(int pageNumber, int pageSize, ...)
{
    if (pageNumber < 1) throw new ArgumentException("Page number must be >= 1");
    if (pageSize < 1 || pageSize > 100) throw new ArgumentException("Page size must be 1-100");
    // ...
}
```

### 4. Return DTOs for Queries (Optional)

For queries that don't map to entities, consider Data Transfer Objects:
```csharp
public record BookSummary(int Id, string Title, int AvailableCopies);

public async Task<List<BookSummary>> GetAvailableBooksAsync()
{
    // Return lightweight DTOs instead of full entities
}
```

### 5. Log SQL for Debugging

```csharp
private readonly ILogger<BookRepository> _logger;

public async Task<Book?> GetByIdAsync(int id, ...)
{
    _logger.LogDebug("Executing GetByIdAsync for BookId={BookId}", id);
    // ... query execution
}
```

## 🔗 Learn More

### Repository Pattern
- [Martin Fowler: Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html)
- [Microsoft: Repository Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)

### ADO.NET
- [Microsoft: ADO.NET Overview](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/ado-net-overview)
- [SqlConnection Class](https://learn.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlconnection)
- [SqlCommand Class](https://learn.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand)
- [SqlDataReader Class](https://learn.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqldatareader)

### Security
- [OWASP: SQL Injection](https://owasp.org/www-community/attacks/SQL_Injection)
- [Parameterized Queries](https://cheatsheetseries.owasp.org/cheatsheets/Query_Parameterization_Cheat_Sheet.html)

### Async/Await
- [Async/Await Best Practices](https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)
- [CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken)

### Testing
- [xUnit Documentation](https://xunit.net/)
- [Integration Testing in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)

## ❓ Discussion Questions

1. **Why use an interface instead of directly using the concrete BookRepository class?**
   - Think about: Testing, flexibility, dependency injection

2. **What would happen if we used string concatenation for SQL queries?**
   - Consider: Bobby Tables ('; DROP TABLE Students;--)

3. **Why is `await using` important for database connections?**
   - Think about: Connection pool exhaustion, memory leaks

4. **When would you NOT want to use the repository pattern?**
   - Consider: Simple CRUD apps, prototypes, ORMs handling abstraction

5. **How does pagination improve application performance?**
   - Think about: Memory usage, query time, user experience

6. **What are alternatives to reflection for object mapping?**
   - Consider: ORMs, factory methods, internal constructors

## 🎯 Summary

**What We've Built:**
- ✅ IBookRepository interface with comprehensive CRUD operations
- ✅ BookRepository implementation with proper ADO.NET patterns
- ✅ Parameterized queries for SQL injection protection
- ✅ Resource disposal with await using statements
- ✅ Async/await with CancellationToken support
- ✅ Pagination with OFFSET-FETCH
- ✅ Integration tests covering all operations
- ✅ Reflection-based mapping for rich domain models

**Key Principles:**
1. **Abstraction**: Interface separates contract from implementation
2. **Security**: Always use parameterized queries
3. **Resource Management**: Always dispose database resources
4. **Async**: Use async/await for scalability
5. **Explicit**: SELECT specific columns, not *
6. **Testability**: Integration tests verify actual database behavior

**Files Created:**
- `src/DbDemo.ConsoleApp/Infrastructure/Repositories/IBookRepository.cs`
- `src/DbDemo.ConsoleApp/Infrastructure/Repositories/BookRepository.cs`
- `tests/DbDemo.Integration.Tests/BookRepositoryTests.cs`
- `tests/DbDemo.Integration.Tests/DatabaseTestFixture.cs`

## 🚀 Next Steps

Now that we have our first repository, we can:

1. **Commit 8**: Create remaining repositories (Author, Member, Loan, Category)
2. **Commit 9**: Build a console application to demonstrate CRUD operations
3. **Commit 10**: Add transaction support for multi-step operations
4. **Focus on business logic** without worrying about SQL details!

**Your data access layer is now properly abstracted! 🎉**
