using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using DbDemo.Application.Repositories;
using DbDemo.Domain.Entities;
using CodeFirstBook = DbDemo.Infrastructure.EFCore.CodeFirst.Entities.Book;

namespace DbDemo.Infrastructure.EFCore.CodeFirst.Repositories;

/// <summary>
/// Entity Framework Core Code-First implementation of IBookRepository.
///
/// CODE-FIRST REPOSITORY PATTERN:
/// ===============================
///
/// This repository demonstrates working with Code-First entities:
/// - Entities ARE the domain model (no separate mapper needed)
/// - Navigation properties loaded via Include/ThenInclude
/// - Global query filter for soft delete (configured in BookConfiguration)
/// - External transaction support via UseTransactionAsync
/// - Simplified model without JSON metadata columns
///
/// DIFFERENCES FROM DATABASE-FIRST:
/// - No mapper layer (entities match domain model)
/// - No metadata/JSON column support (simplified schema)
/// - Direct navigation property usage
///
/// See docs/30-ef-code-first.md for Code-First patterns.
/// </summary>
public class BookRepository : IBookRepository
{
    private readonly LibraryCodeFirstDbContext _context;

    public BookRepository(LibraryCodeFirstDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Create

    public async Task<Book> CreateAsync(Book book, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        // Map domain entity to Code-First entity
        var codeFirstBook = new CodeFirstBook
        {
            ISBN = book.ISBN,
            Title = book.Title,
            Subtitle = book.Subtitle,
            CategoryId = book.CategoryId,
            IsDeleted = book.IsDeleted,
            CreatedAt = book.CreatedAt,
            UpdatedAt = book.UpdatedAt
        };

        _context.Books.Add(codeFirstBook);
        await _context.SaveChangesAsync(cancellationToken);

        // Map back to domain with generated ID
        return MapToDomain(codeFirstBook);
    }

    #endregion

    #region Read - Basic Queries

    public async Task<Book?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        // Global query filter (IsDeleted = false) automatically applied
        var book = await _context.Books
            .AsNoTracking()
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        return book == null ? null : MapToDomain(book);
    }

    public async Task<Book?> GetByIsbnAsync(string isbn, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(isbn);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var book = await _context.Books
            .AsNoTracking()
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.ISBN == isbn, cancellationToken);

        return book == null ? null : MapToDomain(book);
    }

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

        IQueryable<CodeFirstBook> query = _context.Books.AsNoTracking();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        var books = await query
            .Include(b => b.Category)
            .OrderBy(b => b.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return books.Select(MapToDomain).ToList();
    }

    public async Task<List<Book>> SearchByTitleAsync(string searchTerm, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchTerm);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var books = await _context.Books
            .AsNoTracking()
            .Include(b => b.Category)
            .Where(b => EF.Functions.Like(b.Title, $"%{searchTerm}%"))
            .OrderBy(b => b.Title)
            .ToListAsync(cancellationToken);

        return books.Select(MapToDomain).ToList();
    }

    public async Task<List<Book>> GetByCategoryAsync(int categoryId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var books = await _context.Books
            .AsNoTracking()
            .Include(b => b.Category)
            .Where(b => b.CategoryId == categoryId)
            .OrderBy(b => b.Title)
            .ToListAsync(cancellationToken);

        return books.Select(MapToDomain).ToList();
    }

    public async Task<int> GetCountAsync(bool includeDeleted, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        IQueryable<CodeFirstBook> query = _context.Books;

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.CountAsync(cancellationToken);
    }

    #endregion

    #region Update

    public async Task<bool> UpdateAsync(Book book, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var existingBook = await _context.Books.FindAsync(new object[] { book.Id }, cancellationToken);

        if (existingBook == null)
        {
            return false;
        }

        // Update properties
        existingBook.ISBN = book.ISBN;
        existingBook.Title = book.Title;
        existingBook.Subtitle = book.Subtitle;
        existingBook.CategoryId = book.CategoryId;
        existingBook.IsDeleted = book.IsDeleted;
        existingBook.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    #endregion

    #region Atomic Operations

    public async Task<bool> BorrowCopyAsync(int bookId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        // Note: Code-First simplified model doesn't have AvailableCopies/TotalCopies
        // This would need to be added to the entity and migration if needed
        throw new NotSupportedException(
            "BorrowCopyAsync is not supported in the Code-First simplified model. " +
            "Add AvailableCopies and TotalCopies properties to the Book entity to enable this feature.");
    }

    public async Task<bool> ReturnCopyAsync(int bookId, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        // Note: Code-First simplified model doesn't have AvailableCopies/TotalCopies
        throw new NotSupportedException(
            "ReturnCopyAsync is not supported in the Code-First simplified model. " +
            "Add AvailableCopies and TotalCopies properties to the Book entity to enable this feature.");
    }

    #endregion

    #region Delete

    public async Task<bool> DeleteAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var book = await _context.Books.FindAsync(new object[] { id }, cancellationToken);

        if (book == null)
        {
            return false;
        }

        // Soft delete
        book.SoftDelete();

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    #endregion

    #region JSON Column Queries (Not Supported)

    public Task<List<Book>> SearchByMetadataValueAsync(
        string jsonPath,
        string searchValue,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        // Code-First simplified model doesn't include JSON metadata column
        throw new NotSupportedException(
            "Metadata queries are not supported in the Code-First simplified model. " +
            "The Code-First entities don't include a Metadata JSON column. " +
            "Add a Metadata property to the Book entity and create a migration to enable this feature.");
    }

    public Task<List<Book>> GetBooksByTagAsync(
        string tag,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        // Code-First simplified model doesn't include JSON metadata column
        throw new NotSupportedException(
            "Tag queries are not supported in the Code-First simplified model. " +
            "The Code-First entities don't include a Metadata JSON column. " +
            "Add a Metadata property to the Book entity and create a migration to enable this feature.");
    }

    public Task<List<Book>> GetBooksWithMetadataAsync(
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        // Code-First simplified model doesn't include JSON metadata column
        throw new NotSupportedException(
            "Metadata queries are not supported in the Code-First simplified model. " +
            "The Code-First entities don't include a Metadata JSON column. " +
            "Add a Metadata property to the Book entity and create a migration to enable this feature.");
    }

    #endregion

    #region Mapping

    /// <summary>
    /// Maps Code-First entity to domain entity.
    /// Note: In Code-First, the entity IS the domain model in many cases.
    /// This mapping exists for compatibility with the existing repository interface.
    /// </summary>
    private static Book MapToDomain(CodeFirstBook book)
    {
        return Book.FromDatabase(
            id: book.Id,
            isbn: book.ISBN,
            title: book.Title,
            subtitle: book.Subtitle,
            description: null,  // Not in simplified model
            publisher: null,  // Not in simplified model
            publishedDate: null,  // Not in simplified model
            pageCount: null,  // Not in simplified model
            language: null,  // Not in simplified model
            categoryId: book.CategoryId,
            totalCopies: 0,  // Not in simplified model
            availableCopies: 0,  // Not in simplified model
            shelfLocation: null,  // Not in simplified model
            isDeleted: book.IsDeleted,
            createdAt: book.CreatedAt,
            updatedAt: book.UpdatedAt,
            metadataJson: null  // Not in simplified model
        );
    }

    #endregion
}
