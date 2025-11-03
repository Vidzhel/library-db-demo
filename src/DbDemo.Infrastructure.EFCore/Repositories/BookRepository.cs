using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using DbDemo.Application.Repositories;
using DbDemo.Domain.Entities;
using DbDemo.Infrastructure.EFCore.Mappers;
using EFBook = DbDemo.Infrastructure.EFCore.EFModels.Book;

namespace DbDemo.Infrastructure.EFCore.Repositories;

/// <summary>
/// Entity Framework Core implementation of IBookRepository.
///
/// This repository demonstrates COMPREHENSIVE EF Core features:
/// ============================================================
///
/// PERFORMANCE OPTIMIZATIONS:
/// - AsNoTracking() for read-only queries (no change tracking overhead)
/// - Compiled queries for frequently-executed queries
/// - Query splitting to avoid cartesian explosion
/// - Projections to reduce data transfer
///
/// ADVANCED LINQ:
/// - Complex predicates with expression trees
/// - Include/ThenInclude for eager loading
/// - WhereContains for LIKE queries
/// - JSON column queries
///
/// CHANGE TRACKING:
/// - Tracking vs. NoTracking trade-offs
/// - Explicit vs. automatic change detection
/// - Entry state management
///
/// GLOBAL QUERY FILTERS:
/// - Automatic soft delete filtering
/// - IgnoreQueryFilters() to bypass global filters
///
/// TRANSACTION MANAGEMENT:
/// - External SqlTransaction integration
/// - UseTransaction() to attach EF to ADO.NET transaction
///
/// See docs/29-ef-core-orm.md for detailed explanations.
/// </summary>
public class BookRepository : IBookRepository
{
    private readonly LibraryDbContext _context;

    /// <summary>
    /// Creates a new BookRepository.
    /// </summary>
    /// <param name="context">The EF Core DbContext</param>
    public BookRepository(LibraryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Compiled Queries for Performance

    // COMPILED QUERIES: Pre-compiled LINQ expressions for frequently-executed queries
    // Benefits: ~2x faster execution, reduces CPU overhead of query translation
    //
    // How it works:
    // 1. EF Core translates LINQ expression to SQL at first execution
    // 2. Compiled query caches this translation
    // 3. Subsequent executions reuse the cached SQL plan
    //
    // When to use: Queries executed many times with different parameters
    // When NOT to use: Ad-hoc queries, queries with dynamic predicates

    /// <summary>
    /// Compiled query for GetByIdAsync.
    /// This is executed frequently, so we precompile it for performance.
    /// </summary>
    private static readonly Func<LibraryDbContext, int, CancellationToken, Task<EFBook?>> CompiledGetById =
        EF.CompileAsyncQuery(
            (LibraryDbContext context, int id, CancellationToken ct) =>
                context.Books
                    .AsNoTracking()  // Read-only: no change tracking overhead
                    .FirstOrDefault(b => b.Id == id)
        );

    /// <summary>
    /// Compiled query for GetByIsbnAsync.
    /// ISBN lookups are common in library operations.
    /// </summary>
    private static readonly Func<LibraryDbContext, string, CancellationToken, Task<EFBook?>> CompiledGetByIsbn =
        EF.CompileAsyncQuery(
            (LibraryDbContext context, string isbn, CancellationToken ct) =>
                context.Books
                    .AsNoTracking()
                    .FirstOrDefault(b => b.ISBN == isbn)
        );

    #endregion

    #region Create

    /// <summary>
    /// Creates a new book in the database.
    ///
    /// EF CORE FEATURES DEMONSTRATED:
    /// - Add() to mark entity as Added
    /// - SaveChangesAsync() to persist changes
    /// - Automatic ID generation (IDENTITY column)
    /// - Transaction integration via UseTransaction()
    /// - Trigger handling (TR_Books_Audit fires automatically)
    /// </summary>
    public async Task<Book> CreateAsync(Book book, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(transaction);

        // Attach EF context to the external ADO.NET transaction
        // This ensures all operations are part of the same transaction
        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        // Create EF entity from Domain entity
        var efBook = new EFBook();
        efBook.UpdateFromDomain(book, isNewEntity: true);

        // Add to context (marks as Added state)
        // EF will track this entity for changes
        _context.Books.Add(efBook);

        // Persist changes to database
        // EF generates INSERT statement
        // Database generates new ID via IDENTITY
        // Trigger TR_Books_Audit fires automatically
        await _context.SaveChangesAsync(cancellationToken);

        // Return domain entity with generated ID
        return efBook.ToDomain();
    }

    #endregion

    #region Read - Basic Queries

    /// <summary>
    /// Gets a book by ID.
    ///
    /// EF CORE FEATURES DEMONSTRATED:
    /// - Compiled query for performance
    /// - AsNoTracking() for read-only data
    /// - Global query filter (IsDeleted = false) automatically applied
    /// - FirstOrDefault vs. Single
    /// </summary>
    public async Task<Book?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        // Use compiled query for performance
        // Note: Global query filter (IsDeleted = false) is automatically applied
        // See LibraryDbContext.OnModelCreating() for filter configuration
        var efBook = await CompiledGetById(_context, id, cancellationToken);

        return efBook?.ToDomain();
    }

    /// <summary>
    /// Gets a book by ISBN.
    ///
    /// EF CORE FEATURES DEMONSTRATED:
    /// - Compiled query
    /// - String comparison (translates to SQL WHERE ISBN = @p0)
    /// </summary>
    public async Task<Book?> GetByIsbnAsync(string isbn, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(isbn);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var efBook = await CompiledGetByIsbn(_context, isbn, cancellationToken);
        return efBook?.ToDomain();
    }

    /// <summary>
    /// Gets books with pagination.
    ///
    /// EF CORE FEATURES DEMONSTRATED:
    /// - Skip() and Take() for pagination (translates to OFFSET/FETCH in SQL)
    /// - OrderBy() for deterministic pagination
    /// - AsNoTracking() for read-only queries
    /// - IgnoreQueryFilters() to include deleted books when requested
    /// - ToListAsync() for async materialization
    /// </summary>
    public async Task<List<Book>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        bool includeDeleted,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (pageNumber < 1) throw new ArgumentException("Page number must be >= 1", nameof(pageNumber));
        if (pageSize < 1) throw new ArgumentException("Page size must be >= 1", nameof(pageSize));

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        // Build query
        IQueryable<EFBook> query = _context.Books.AsNoTracking();

        // GLOBAL QUERY FILTERS: Bypass soft delete filter if requested
        // By default, the global filter (IsDeleted = false) is applied
        // Use IgnoreQueryFilters() to see deleted books
        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        // PAGINATION: Skip + Take translates to OFFSET/FETCH in SQL Server
        // Always use OrderBy before pagination for deterministic results
        var efBooks = await query
            .OrderBy(b => b.Title)               // Deterministic ordering
            .Skip((pageNumber - 1) * pageSize)   // OFFSET
            .Take(pageSize)                      // FETCH NEXT n ROWS
            .ToListAsync(cancellationToken);     // Execute query

        // Map to domain entities
        return efBooks.ToDomain();
    }

    /// <summary>
    /// Searches books by title using LIKE.
    ///
    /// EF CORE FEATURES DEMONSTRATED:
    /// - EF.Functions.Like() for SQL LIKE operator
    /// - String interpolation in LIKE patterns
    /// - Case-insensitive search (SQL Server default collation)
    /// </summary>
    public async Task<List<Book>> SearchByTitleAsync(string searchTerm, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchTerm);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        // EF.Functions.Like() translates to SQL LIKE
        // Pattern: %searchTerm% for contains search
        var efBooks = await _context.Books
            .AsNoTracking()
            .Where(b => EF.Functions.Like(b.Title, $"%{searchTerm}%"))
            .OrderBy(b => b.Title)
            .ToListAsync(cancellationToken);

        return efBooks.ToDomain();
    }

    /// <summary>
    /// Gets books by category.
    ///
    /// EF CORE FEATURES DEMONSTRATED:
    /// - Simple WHERE clause
    /// - Foreign key navigation (could also use Include for Category entity)
    /// </summary>
    public async Task<List<Book>> GetByCategoryAsync(int categoryId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var efBooks = await _context.Books
            .AsNoTracking()
            .Where(b => b.CategoryId == categoryId)
            .OrderBy(b => b.Title)
            .ToListAsync(cancellationToken);

        return efBooks.ToDomain();
    }

    /// <summary>
    /// Gets count of books.
    ///
    /// EF CORE FEATURES DEMONSTRATED:
    /// - CountAsync() for efficient COUNT(*) query
    /// - No entity materialization (just returns int)
    /// - Global query filter handling
    /// </summary>
    public async Task<int> GetCountAsync(bool includeDeleted, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        IQueryable<EFBook> query = _context.Books;

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        // CountAsync() generates: SELECT COUNT(*) FROM Books WHERE IsDeleted = 0
        return await query.CountAsync(cancellationToken);
    }

    #endregion

    #region Update

    /// <summary>
    /// Updates an existing book.
    ///
    /// EF CORE FEATURES DEMONSTRATED:
    /// - FindAsync() to get tracked entity
    /// - Automatic change detection
    /// - Update() vs. manual property assignment
    /// - SaveChangesAsync() generates UPDATE only for modified properties
    /// - Optimistic concurrency (could add RowVersion)
    /// </summary>
    public async Task<bool> UpdateAsync(Book book, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        // OPTION 1: Find existing entity (tracked by EF)
        // FindAsync uses primary key and checks change tracker first
        var existingEfBook = await _context.Books.FindAsync(new object[] { book.Id }, cancellationToken);

        if (existingEfBook == null)
        {
            return false;
        }

        // Update properties from domain entity
        // EF automatically detects which properties changed
        existingEfBook.UpdateFromDomain(book, isNewEntity: false);

        // SaveChanges generates UPDATE only for modified columns
        // Example: UPDATE Books SET Title = @p0, UpdatedAt = @p1 WHERE Id = @p2
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    #endregion

    #region Atomic Operations

    /// <summary>
    /// Atomically borrows a book copy (decrements AvailableCopies).
    ///
    /// EF CORE LIMITATION: Cannot express "UPDATE ... SET x = x - 1 WHERE ..."
    /// SOLUTION: Use ExecuteSqlRawAsync for atomic database operation
    ///
    /// WHY NOT EF:
    /// - Read-modify-write in EF has race condition
    /// - Two concurrent requests could both decrement from same value
    /// - Database constraint would fail, but data integrity at risk
    ///
    /// ALTERNATIVE: Use raw SQL for atomic operation (see ADO.NET implementation)
    /// </summary>
    public async Task<bool> BorrowCopyAsync(int bookId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        // EF Core ExecuteSqlRawAsync for atomic update
        // This bypasses change tracking and executes SQL directly
        var sql = @"
            UPDATE Books
            SET AvailableCopies = AvailableCopies - 1
            WHERE Id = {0}
              AND AvailableCopies > 0
              AND IsDeleted = 0";

        var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
            sql,
            parameters: new object[] { bookId },
            cancellationToken: cancellationToken
        );

        return rowsAffected > 0;
    }

    /// <summary>
    /// Atomically returns a book copy (increments AvailableCopies).
    /// </summary>
    public async Task<bool> ReturnCopyAsync(int bookId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var sql = @"
            UPDATE Books
            SET AvailableCopies = AvailableCopies + 1
            WHERE Id = {0}
              AND AvailableCopies < TotalCopies
              AND IsDeleted = 0";

        var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
            sql,
            parameters: new object[] { bookId },
            cancellationToken: cancellationToken
        );

        return rowsAffected > 0;
    }

    #endregion

    #region Delete

    /// <summary>
    /// Soft deletes a book.
    ///
    /// EF CORE FEATURES DEMONSTRATED:
    /// - FindAsync() to get entity
    /// - Modify property on tracked entity
    /// - SaveChangesAsync() generates UPDATE
    /// - Trigger fires automatically
    /// </summary>
    public async Task<bool> DeleteAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var efBook = await _context.Books.FindAsync(new object[] { id }, cancellationToken);

        if (efBook == null)
        {
            return false;
        }

        // Soft delete: set IsDeleted flag
        efBook.IsDeleted = true;

        // EF generates: UPDATE Books SET IsDeleted = 1 WHERE Id = @p0
        // Trigger TR_Books_Audit fires and logs the change
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    #endregion

    #region JSON Column Queries

    /// <summary>
    /// Searches books by metadata JSON value.
    ///
    /// EF CORE JSON SUPPORT (EF Core 7+):
    /// - Can query JSON columns with LINQ
    /// - Translates to SQL Server JSON_VALUE()
    ///
    /// LIMITATION: Complex JSON queries may require raw SQL
    /// For production, consider using FromSqlRaw for JSON queries
    /// </summary>
    public async Task<List<Book>> SearchByMetadataValueAsync(
        string jsonPath,
        string value,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonPath);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        // For complex JSON queries, use raw SQL
        // EF Core's JSON support is improving but not yet fully feature-complete
        var sql = @"
            SELECT *
            FROM Books
            WHERE JSON_VALUE(Metadata, {0}) = {1}
              AND IsDeleted = 0
            ORDER BY Title";

        var efBooks = await _context.Books
            .FromSqlRaw(sql, jsonPath, value)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return efBooks.ToDomain();
    }

    /// <summary>
    /// Gets books by tag using OPENJSON.
    ///
    /// ADVANCED SQL: CROSS APPLY OPENJSON
    /// EF Core cannot express this in LINQ, so we use raw SQL
    /// </summary>
    public async Task<List<Book>> GetBooksByTagAsync(
        string tag,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        // FromSqlRaw executes custom SQL and maps to entity
        // This is WHERE EF Core SHINES: You get SQL power + entity mapping
        var sql = @"
            SELECT DISTINCT b.*
            FROM Books b
            CROSS APPLY OPENJSON(b.Metadata, '$.tags') AS tags
            WHERE tags.[value] = {0}
              AND b.IsDeleted = 0
            ORDER BY b.Title";

        var efBooks = await _context.Books
            .FromSqlRaw(sql, tag)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return efBooks.ToDomain();
    }

    /// <summary>
    /// Gets all books with non-null metadata.
    ///
    /// Demonstrates simple NULL check on JSON column.
    /// </summary>
    public async Task<List<Book>> GetBooksWithMetadataAsync(
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var efBooks = await _context.Books
            .AsNoTracking()
            .Where(b => b.Metadata != null)
            .OrderBy(b => b.Title)
            .ToListAsync(cancellationToken);

        return efBooks.ToDomain();
    }

    #endregion
}
