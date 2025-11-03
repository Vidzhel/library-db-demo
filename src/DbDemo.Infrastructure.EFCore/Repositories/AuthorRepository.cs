using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using DbDemo.Application.Repositories;
using DbDemo.Domain.Entities;
using DbDemo.Infrastructure.EFCore.Mappers;
using EFAuthor = DbDemo.Infrastructure.EFCore.EFModels.Author;

namespace DbDemo.Infrastructure.EFCore.Repositories;

/// <summary>
/// Entity Framework Core implementation of IAuthorRepository.
///
/// This repository demonstrates BASIC CRUD and LINQ patterns:
/// ===========================================================
///
/// CORE PATTERNS:
/// - Standard CRUD operations
/// - Simple LINQ queries
/// - OrderBy, Where, Skip, Take
/// - AsNoTracking for read-only queries
/// - FirstOrDefaultAsync vs. SingleOrDefaultAsync
///
/// COMPARISON TO BookRepository:
/// - Simpler queries (no JSON, no compiled queries)
/// - Focus on fundamental EF Core patterns
/// - Good starting point for learning EF Core
///
/// See BookRepository for advanced patterns (compiled queries, JSON, etc.)
/// </summary>
public class AuthorRepository : IAuthorRepository
{
    private readonly LibraryDbContext _context;

    public AuthorRepository(LibraryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Creates a new author.
    ///
    /// PATTERN: Add + SaveChanges
    /// - Add() marks entity as Added in change tracker
    /// - SaveChangesAsync() generates INSERT statement
    /// - Returns entity with generated ID
    /// </summary>
    public async Task<Author> CreateAsync(Author author, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(author);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var efAuthor = new EFAuthor();
        efAuthor.UpdateFromDomain(author, isNewEntity: true);

        _context.Authors.Add(efAuthor);
        await _context.SaveChangesAsync(cancellationToken);

        return efAuthor.ToDomain();
    }

    /// <summary>
    /// Gets an author by ID.
    ///
    /// PATTERN: AsNoTracking + FirstOrDefaultAsync
    /// - AsNoTracking: No change tracking overhead (30-40% faster for read-only)
    /// - FirstOrDefaultAsync: Returns first match or null (vs. Single which throws on multiple)
    /// </summary>
    public async Task<Author?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var efAuthor = await _context.Authors
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return efAuthor?.ToDomain();
    }

    /// <summary>
    /// Gets an author by email.
    ///
    /// PATTERN: Where clause with string equality
    /// - EF translates to: WHERE Email = @p0
    /// - String comparison is case-insensitive by default (SQL Server collation)
    /// </summary>
    public async Task<Author?> GetByEmailAsync(string email, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var efAuthor = await _context.Authors
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Email == email, cancellationToken);

        return efAuthor?.ToDomain();
    }

    /// <summary>
    /// Gets paginated authors.
    ///
    /// PATTERN: OrderBy + Skip + Take (OFFSET/FETCH in SQL)
    /// - OrderBy is REQUIRED for deterministic pagination
    /// - Skip/Take translate to OFFSET/FETCH NEXT in SQL Server
    /// </summary>
    public async Task<List<Author>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (pageNumber < 1) throw new ArgumentException("Page number must be >= 1", nameof(pageNumber));
        if (pageSize < 1) throw new ArgumentException("Page size must be >= 1", nameof(pageSize));

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var efAuthors = await _context.Authors
            .AsNoTracking()
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return efAuthors.ToDomain();
    }

    /// <summary>
    /// Searches authors by name.
    ///
    /// PATTERN: LIKE query using EF.Functions.Like()
    /// - Contains search: %searchTerm%
    /// - Composite WHERE: (FirstName LIKE ... OR LastName LIKE ...)
    /// </summary>
    public async Task<List<Author>> SearchByNameAsync(
        string searchTerm,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchTerm);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var pattern = $"%{searchTerm}%";

        var efAuthors = await _context.Authors
            .AsNoTracking()
            .Where(a =>
                EF.Functions.Like(a.FirstName, pattern) ||
                EF.Functions.Like(a.LastName, pattern))
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .ToListAsync(cancellationToken);

        return efAuthors.ToDomain();
    }

    /// <summary>
    /// Gets count of authors.
    ///
    /// PATTERN: CountAsync for efficient counting
    /// - Generates: SELECT COUNT(*) FROM Authors
    /// - No entity materialization (returns int directly)
    /// </summary>
    public async Task<int> GetCountAsync(SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        return await _context.Authors.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Updates an author.
    ///
    /// PATTERN: FindAsync + property modification + SaveChanges
    /// - FindAsync: Checks change tracker first, then queries DB
    /// - EF automatically tracks property changes
    /// - SaveChanges generates UPDATE only for modified columns
    /// </summary>
    public async Task<bool> UpdateAsync(Author author, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(author);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var existingEfAuthor = await _context.Authors.FindAsync(new object[] { author.Id }, cancellationToken);

        if (existingEfAuthor == null)
        {
            return false;
        }

        existingEfAuthor.UpdateFromDomain(author, isNewEntity: false);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Deletes an author.
    ///
    /// PATTERN: FindAsync + Remove + SaveChanges
    /// - Remove() marks entity as Deleted in change tracker
    /// - SaveChanges generates DELETE statement
    /// - CASCADE DELETE in BookAuthors junction table (configured in DB)
    /// </summary>
    public async Task<bool> DeleteAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var efAuthor = await _context.Authors.FindAsync(new object[] { id }, cancellationToken);

        if (efAuthor == null)
        {
            return false;
        }

        _context.Authors.Remove(efAuthor);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
