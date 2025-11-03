# DbDemo.Infrastructure.EFCore

Entity Framework Core implementation of repository interfaces, demonstrating comprehensive ORM features and advanced LINQ patterns.

## Overview

This project provides a **database-first** EF Core implementation that showcases:

- **Scaffolded EF entities** from existing database schema
- **Separation of concerns**: EF entities (anemic) vs Domain entities (rich)
- **Entity mapping layer** for converting between EF and Domain models
- **Advanced LINQ** queries with expression trees
- **Comprehensive EF features**: compiled queries, query splitting, spatial data, JSON columns, interceptors, etc.

## Architecture

```
Database (SQL Server)
     ↓ (scaffold)
EF Entity Models (EFModels/)
     ↓ (mapping)
Domain Entities (from DbDemo.Domain)
     ↓ (use)
Application Services
```

### Key Components

1. **EFModels/** - Scaffolded entity classes (anemic data models)
   - Generated via `dotnet ef dbcontext scaffold`
   - Simple POCOs matching database schema
   - No business logic, just properties

2. **Configurations/** - Fluent API entity configurations
   - Custom mappings for JSON columns, spatial data, etc.
   - Index and constraint definitions
   - Relationship configuration

3. **Mappers/** - Entity mapping layer
   - `EntityMapper.cs` - Converts between EF entities ↔ Domain entities
   - Handles nested relationships and null values
   - Preserves domain invariants

4. **Repositories/** - Repository implementations
   - All 9 repositories fully implemented
   - Demonstrate different EF Core features per repository
   - Transaction support via external SqlTransaction

5. **LibraryDbContext.cs** - Main DbContext
   - Configured for external transaction management
   - Global query filters (soft delete)
   - Query splitting configuration
   - Change tracking strategies

## Scaffolding

The EF entity models were scaffolded using:

```bash
cd src/DbDemo.Infrastructure.EFCore

dotnet ef dbcontext scaffold \
  "Server=localhost,1453;Database=LibraryDb;User Id=library_app_user;Password=LibraryApp@2024!;TrustServerCertificate=True;" \
  Microsoft.EntityFrameworkCore.SqlServer \
  --output-dir EFModels \
  --context-dir . \
  --context LibraryDbContext \
  --data-annotations \
  --force \
  --use-database-names \
  --no-onconfiguring
```

## Why Separate EF and Domain Entities?

### The Problem with Using EF Entities as Domain Models

Many developers use EF entities directly as domain models. This creates several problems:

1. **Anemic Domain Model Anti-Pattern**
   - EF entities are just property bags with no behavior
   - Business logic ends up scattered in services
   - Violates encapsulation and domain-driven design principles

2. **EF Requirements Conflict with DDD**
   - EF needs parameterless constructors → Can't enforce invariants
   - EF needs public setters → Can't prevent invalid states
   - EF navigation properties expose collections → Can't control relationships

3. **Tight Coupling to ORM**
   - Domain model depends on EF attributes/conventions
   - Can't easily switch ORMs or use different persistence
   - Testing requires EF infrastructure

### Our Solution: Separation of Concerns

```
EF Entities (EFModels/)           Domain Entities (DbDemo.Domain/)
=======================           ================================
✓ Anemic (no behavior)            ✓ Rich (business logic)
✓ Public setters                  ✓ Immutable/controlled mutation
✓ Parameterless constructor       ✓ Factory methods
✓ EF navigation properties        ✓ Encapsulated collections
✓ Database schema focus           ✓ Business domain focus
✓ Easy to scaffold/regenerate     ✓ Manually crafted with care
```

The mapping layer (`EntityMapper`) bridges the two worlds.

## Features Demonstrated

### Repository-Specific Features

- **BookRepository** - Advanced LINQ, JSON columns, compiled queries, AsNoTracking
- **AuthorRepository** - Basic CRUD, simple LINQ, pagination patterns
- **MemberRepository** - Stored procedures (FromSqlRaw), table-valued functions
- **LoanRepository** - Complex LINQ, GroupJoin, LEFT JOIN patterns, aggregations
- **CategoryRepository** - Recursive queries, self-referencing navigation, tree structures
- **LibraryBranchRepository** - Spatial queries with NetTopologySuite (STDistance, geography)
- **BookAuditRepository** - Read-only tracking, QueryTrackingBehavior, immutable entities
- **ReportRepository** - Complex aggregations, PIVOT/UNPIVOT, window functions via raw SQL
- **SystemStatisticsRepository** - Bulk operations, change tracking optimization, time-series

### Advanced EF Core Patterns

- ✅ External transaction management (SqlTransaction integration)
- ✅ Query splitting (AsSplitQuery vs AsSingleQuery)
- ✅ Compiled queries for performance
- ✅ AsNoTracking for read-only queries
- ✅ Global query filters (IsDeleted)
- ✅ JSON column support (EF Core 7+)
- ✅ Spatial data with NetTopologySuite
- ✅ Raw SQL interop (FromSqlRaw, ExecuteSqlRaw)
- ✅ Stored procedures and functions
- ✅ Complex LINQ expressions
- ✅ Projection queries vs entity queries
- ✅ Include/ThenInclude for eager loading
- ✅ Explicit loading for on-demand data
- ✅ Change tracking behavior customization

## Documentation

See **docs/29-ef-core-orm.md** for comprehensive coverage of:

1. **LINQ Fundamentals** - What is LINQ, how it works, query vs method syntax
2. **Expression Trees** - How EF translates C# to SQL, ExpressionVisitor pattern
3. **EF Core Architecture** - DbContext lifecycle, change tracking, SaveChanges internals
4. **Advanced Queries** - Include, projections, GroupBy, window functions, raw SQL
5. **Performance** - AsNoTracking, query splitting, compiled queries, batch operations
6. **ORM Problems** - N+1 queries, cartesian explosion, massive joins, abstraction leaks
7. **Anti-Patterns** - Common mistakes with before/after examples

## Usage

```csharp
// Create DbContext with external transaction
var context = new LibraryDbContext(optionsBuilder.Options);
await context.Database.UseTransactionAsync(sqlTransaction);

// Create repository
var bookRepository = new BookRepository(context);

// Use repository
var book = await bookRepository.GetByIdAsync(1, sqlTransaction, cancellationToken);
```

## Comparison: Three Implementations

| Feature | ADO.NET | SqlKata | **EF Core** |
|---------|---------|---------|-------------|
| Abstraction Level | Low (SQL strings) | Medium (query builder) | **High (LINQ)** |
| Type Safety | ❌ Runtime only | ✅ Compile-time | **✅ Compile-time** |
| SQL Control | ✅ Full | ✅ Hybrid | **⚠️ Limited** |
| Learning Curve | Low | Medium | **High** |
| Boilerplate | High | Medium | **Low** |
| Change Tracking | Manual | Manual | **Automatic** |
| Navigation Props | None | None | **✅ Built-in** |
| Complex Queries | ✅ Easy | ⚠️ Fallback | **⚠️ Fallback** |
| Performance | Optimal | Very Good | **Good** |
| Use Case | Maximum control | Middle ground | **Rapid development** |

## When to Use EF Core

### ✅ Good Fit

- Standard CRUD operations dominate
- Rapid development is priority
- Team prefers LINQ over SQL
- Navigation properties add value
- Change tracking is useful

### ❌ Poor Fit

- Complex queries with CTEs, window functions, PIVOT
- Performance-critical hot paths
- Database-first with many stored procedures
- Team expertise is in SQL, not LINQ
- Need exact SQL control

## Trade-offs

### Benefits

- **Productivity**: Less boilerplate, automatic change tracking
- **Type Safety**: LINQ expressions checked at compile-time
- **Navigation**: Easy to traverse relationships
- **Migrations**: Code-first database evolution (not used in this project)

### Costs

- **Abstraction Leaks**: Complex LINQ may not translate, need raw SQL fallback
- **Performance**: Change tracking overhead, potential N+1 queries
- **Complexity**: Large codebase for EF Core itself, steep learning curve
- **Rigidity**: Difficult to express some SQL patterns in LINQ

## License

Part of DbDemo - Database Programming Educational Project
