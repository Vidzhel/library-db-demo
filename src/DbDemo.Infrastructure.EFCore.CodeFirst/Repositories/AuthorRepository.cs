using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using DbDemo.Application.Repositories;
using DbDemo.Domain.Entities;
using CodeFirstAuthor = DbDemo.Infrastructure.EFCore.CodeFirst.Entities.Author;

namespace DbDemo.Infrastructure.EFCore.CodeFirst.Repositories;

/// <summary>
/// Entity Framework Core Code-First implementation of IAuthorRepository.
///
/// CODE-FIRST REPOSITORY PATTERN:
/// ===============================
///
/// This repository demonstrates basic CRUD operations with Code-First entities:
/// - Direct entity usage (no mapper layer)
/// - Standard LINQ queries
/// - AsNoTracking for read-only operations
/// - External transaction support
///
/// COMPUTED COLUMNS:
/// - FullName is computed by database ([FirstName] + ' ' + [LastName])
/// - EF Core automatically populates computed columns on SaveChanges
/// - See AuthorConfiguration for computed column configuration
///
/// See docs/30-ef-code-first.md for Code-First patterns.
/// </summary>
public class AuthorRepository : IAuthorRepository
{
    private readonly LibraryCodeFirstDbContext _context;

    public AuthorRepository(LibraryCodeFirstDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Author> CreateAsync(Author author, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(author);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var codeFirstAuthor = new CodeFirstAuthor
        {
            FirstName = author.FirstName,
            LastName = author.LastName,
            Email = author.Email,
            Bio = author.Biography,
            CreatedAt = author.CreatedAt,
            UpdatedAt = author.UpdatedAt
            // Note: FullName is computed by database - don't set it here
        };

        _context.Authors.Add(codeFirstAuthor);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDomain(codeFirstAuthor);
    }

    public async Task<Author?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var author = await _context.Authors
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return author == null ? null : MapToDomain(author);
    }

    public async Task<Author?> GetByEmailAsync(string email, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var author = await _context.Authors
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Email == email, cancellationToken);

        return author == null ? null : MapToDomain(author);
    }

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

        var authors = await _context.Authors
            .AsNoTracking()
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return authors.Select(MapToDomain).ToList();
    }

    public async Task<List<Author>> SearchByNameAsync(
        string searchTerm,
        SqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchTerm);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var pattern = $"%{searchTerm}%";

        var authors = await _context.Authors
            .AsNoTracking()
            .Where(a =>
                EF.Functions.Like(a.FirstName, pattern) ||
                EF.Functions.Like(a.LastName, pattern) ||
                EF.Functions.Like(a.FullName!, pattern))  // Can search computed FullName too!
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .ToListAsync(cancellationToken);

        return authors.Select(MapToDomain).ToList();
    }

    public async Task<int> GetCountAsync(SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        return await _context.Authors.CountAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(Author author, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(author);
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var existingAuthor = await _context.Authors.FindAsync(new object[] { author.Id }, cancellationToken);

        if (existingAuthor == null)
        {
            return false;
        }

        // Update properties
        existingAuthor.FirstName = author.FirstName;
        existingAuthor.LastName = author.LastName;
        existingAuthor.Email = author.Email;
        existingAuthor.Bio = author.Biography;
        existingAuthor.UpdatedAt = DateTime.UtcNow;
        // Note: FullName is computed - will be updated automatically by database

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _context.Database.UseTransactionAsync(transaction, cancellationToken);

        var author = await _context.Authors.FindAsync(new object[] { id }, cancellationToken);

        if (author == null)
        {
            return false;
        }

        // Hard delete (no soft delete in Author entity)
        // CASCADE DELETE will remove BookAuthor junction entries automatically
        _context.Authors.Remove(author);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    #region Mapping

    /// <summary>
    /// Maps Code-First entity to domain entity.
    /// </summary>
    private static Author MapToDomain(CodeFirstAuthor author)
    {
        return Author.FromDatabase(
            id: author.Id,
            firstName: author.FirstName,
            lastName: author.LastName,
            biography: author.Bio,
            dateOfBirth: null,  // Not in simplified model
            nationality: null,  // Not in simplified model
            email: author.Email,
            createdAt: author.CreatedAt,
            updatedAt: author.UpdatedAt
        );
    }

    #endregion
}
