using Microsoft.Data.SqlClient;
using DbDemo.Application.Repositories;
using AdoNetRepositories = DbDemo.Infrastructure.Repositories;
using SqlKataRepositories = DbDemo.Infrastructure.SqlKata.Repositories;
using EFCoreRepositories = DbDemo.Infrastructure.EFCore.Repositories;
using DbDemo.Infrastructure.EFCore;
using Microsoft.EntityFrameworkCore;

namespace DbDemo.ConsoleApp.Infrastructure;

/// <summary>
/// Factory for creating repository instances based on the selected provider.
///
/// This demonstrates the STRATEGY PATTERN and ABSTRACT FACTORY PATTERN:
/// - Different implementations (ADO.NET, SqlKata, EF Core) with same interface
/// - Allows switching between providers without changing client code
/// - Each provider has different trade-offs (performance, ease of use, features)
/// </summary>
public class RepositoryFactory
{
    private readonly RepositoryProvider _provider;
    private readonly string _connectionString;
    private LibraryDbContext? _dbContext;

    public RepositoryFactory(RepositoryProvider provider, string connectionString)
    {
        _provider = provider;
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

        // For EF Core, create a single DbContext instance
        // Note: In production, use DI with scoped lifetime
        if (_provider == RepositoryProvider.EFCore)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LibraryDbContext>();
            optionsBuilder.UseSqlServer(_connectionString);
            _dbContext = new LibraryDbContext(optionsBuilder.Options);
        }
    }

    /// <summary>
    /// Gets the name of the current provider.
    /// </summary>
    public string ProviderName => _provider switch
    {
        RepositoryProvider.AdoNet => "ADO.NET (Raw SQL)",
        RepositoryProvider.SqlKata => "SqlKata Query Builder",
        RepositoryProvider.EFCore => "Entity Framework Core",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets a description of the current provider.
    /// </summary>
    public string ProviderDescription => _provider switch
    {
        RepositoryProvider.AdoNet => "Maximum control and performance with raw SQL queries",
        RepositoryProvider.SqlKata => "Fluent query builder - middle ground between ORM and raw SQL",
        RepositoryProvider.EFCore => "Full-featured ORM with LINQ, change tracking, and expression trees",
        _ => "Unknown provider"
    };

    /// <summary>
    /// Creates a book repository instance.
    /// </summary>
    public IBookRepository CreateBookRepository()
    {
        return _provider switch
        {
            RepositoryProvider.AdoNet => new AdoNetRepositories.BookRepository(),
            RepositoryProvider.SqlKata => new SqlKataRepositories.BookRepository(),
            RepositoryProvider.EFCore => new DbDemo.Infrastructure.EFCore.Repositories.BookRepository(_dbContext!),
            _ => throw new NotSupportedException($"Provider {_provider} is not supported")
        };
    }

    /// <summary>
    /// Creates an author repository instance.
    /// </summary>
    public IAuthorRepository CreateAuthorRepository()
    {
        return _provider switch
        {
            RepositoryProvider.AdoNet => new AdoNetRepositories.AuthorRepository(),
            RepositoryProvider.SqlKata => throw new NotSupportedException("SqlKata AuthorRepository not yet implemented"),
            RepositoryProvider.EFCore => new DbDemo.Infrastructure.EFCore.Repositories.AuthorRepository(_dbContext!),
            _ => throw new NotSupportedException($"Provider {_provider} is not supported")
        };
    }

    /// <summary>
    /// Creates a member repository instance.
    /// </summary>
    public IMemberRepository CreateMemberRepository()
    {
        return _provider switch
        {
            RepositoryProvider.AdoNet => new AdoNetRepositories.MemberRepository(),
            RepositoryProvider.SqlKata => throw new NotSupportedException("SqlKata MemberRepository not yet implemented"),
            RepositoryProvider.EFCore => new DbDemo.Infrastructure.EFCore.Repositories.MemberRepository(_dbContext!),
            _ => throw new NotSupportedException($"Provider {_provider} is not supported")
        };
    }

    /// <summary>
    /// Creates a loan repository instance.
    /// </summary>
    public ILoanRepository CreateLoanRepository()
    {
        return _provider switch
        {
            RepositoryProvider.AdoNet => new AdoNetRepositories.LoanRepository(),
            RepositoryProvider.SqlKata => throw new NotSupportedException("SqlKata LoanRepository not yet implemented"),
            RepositoryProvider.EFCore => throw new NotSupportedException("EFCore LoanRepository not yet implemented"),
            _ => throw new NotSupportedException($"Provider {_provider} is not supported")
        };
    }

    /// <summary>
    /// Creates a category repository instance.
    /// </summary>
    public ICategoryRepository CreateCategoryRepository()
    {
        return _provider switch
        {
            RepositoryProvider.AdoNet => new AdoNetRepositories.CategoryRepository(),
            RepositoryProvider.SqlKata => throw new NotSupportedException("SqlKata CategoryRepository not yet implemented"),
            RepositoryProvider.EFCore => throw new NotSupportedException("EFCore CategoryRepository not yet implemented"),
            _ => throw new NotSupportedException($"Provider {_provider} is not supported")
        };
    }

    /// <summary>
    /// Creates a book audit repository instance.
    /// </summary>
    public IBookAuditRepository CreateBookAuditRepository()
    {
        return _provider switch
        {
            RepositoryProvider.AdoNet => new AdoNetRepositories.BookAuditRepository(),
            RepositoryProvider.SqlKata => throw new NotSupportedException("SqlKata BookAuditRepository not yet implemented"),
            RepositoryProvider.EFCore => throw new NotSupportedException("EFCore BookAuditRepository not yet implemented"),
            _ => throw new NotSupportedException($"Provider {_provider} is not supported")
        };
    }

    /// <summary>
    /// Creates a system statistics repository instance.
    /// </summary>
    public ISystemStatisticsRepository CreateSystemStatisticsRepository()
    {
        return _provider switch
        {
            RepositoryProvider.AdoNet => new AdoNetRepositories.SystemStatisticsRepository(),
            RepositoryProvider.SqlKata => throw new NotSupportedException("SqlKata SystemStatisticsRepository not yet implemented"),
            RepositoryProvider.EFCore => throw new NotSupportedException("EFCore SystemStatisticsRepository not yet implemented"),
            _ => throw new NotSupportedException($"Provider {_provider} is not supported")
        };
    }

    /// <summary>
    /// Checks if all repositories are implemented for the current provider.
    /// </summary>
    public bool AreAllRepositoriesImplemented()
    {
        return _provider == RepositoryProvider.AdoNet;
    }

    /// <summary>
    /// Gets information about which repositories are implemented.
    /// </summary>
    public string GetImplementationStatus()
    {
        return _provider switch
        {
            RepositoryProvider.AdoNet => "✅ All repositories implemented",
            RepositoryProvider.SqlKata => "⚠️  Only BookRepository implemented (demo)",
            RepositoryProvider.EFCore => "⚠️  BookRepository, AuthorRepository, MemberRepository implemented (demo)",
            _ => "❌ No repositories implemented"
        };
    }
}
