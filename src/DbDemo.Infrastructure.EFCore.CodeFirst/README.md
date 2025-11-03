# DbDemo.Infrastructure.EFCore.CodeFirst

## Entity Framework Core - Code-First Approach

This project demonstrates the **Code-First** approach to Entity Framework Core, where **C# entity classes are the source of truth** and the database schema is generated from them.

### Comparison: Code-First vs Database-First

| Aspect | Code-First (This Project) | Database-First (DbDemo.Infrastructure.EFCore) |
|--------|---------------------------|---------------------------------------------|
| **Source of Truth** | C# entity classes | SQL migration files + Database |
| **Workflow** | Entities → Migrations → Database | SQL Scripts → Database → Scaffold Entities |
| **Schema Changes** | Modify entities → Add migration | Modify SQL → Run migration → Re-scaffold |
| **Migrations** | EF Core migrations (`dotnet ef`) | Custom SQL migration files |
| **Entity Style** | Can be rich domain models | Anemic POCOs (scaffolded) |
| **Team Fit** | C#-first teams | SQL-first teams, DBAs |

### What This Project Demonstrates

#### 1. **Code-First Entity Definition**
- Simple entities with Data Annotations
- Fluent API configuration (preferred for complex scenarios)
- Relationships (One-to-Many, Many-to-Many with junction table)
- Indexes, constraints, and computed properties defined in code

#### 2. **Entity Framework Migrations**
- Automatic migration generation from entity changes
- Migration history tracking (`__EFMigrationsHistory`)
- Up/Down migrations for schema versioning
- Migration rollback capabilities

#### 3. **Fluent API Configuration**
- Organized into separate configuration classes (`IEntityTypeConfiguration<T>`)
- Property configuration (types, lengths, nullability)
- Relationship configuration (FK, cascade behaviors)
- Index and constraint definition
- Data seeding

#### 4. **Data Seeding**
- `HasData()` for simple seeding in migrations
- Custom seeder class for complex scenarios
- Seeding related entities with proper FK relationships

### Project Structure

```
DbDemo.Infrastructure.EFCore.CodeFirst/
├── Entities/                    # Code-First entity classes
│   ├── Book.cs                  # Main entity with relationships
│   ├── Author.cs                # Related entity
│   ├── Category.cs              # Lookup entity
│   └── BookAuthor.cs            # Junction table for many-to-many
│
├── Configuration/               # Fluent API configurations
│   ├── BookConfiguration.cs     # Book entity configuration
│   ├── AuthorConfiguration.cs
│   ├── CategoryConfiguration.cs
│   └── BookAuthorConfiguration.cs
│
├── Migrations/                  # EF Core generated migrations
│   ├── 20250101000000_InitialCreate.cs
│   ├── 20250102000000_AddIndexes.cs
│   ├── 20250103000000_AddSoftDelete.cs
│   └── LibraryCodeFirstDbContextModelSnapshot.cs
│
├── Seed/
│   └── LibraryDataSeeder.cs     # Data seeding logic
│
├── Repositories/                # Repository implementations
│   ├── BookRepositoryCodeFirst.cs
│   └── AuthorRepositoryCodeFirst.cs
│
├── LibraryCodeFirstDbContext.cs # Main DbContext
└── README.md (this file)
```

### Simplified Domain Model

To keep the Code-First example focused and educational, we use a **simplified subset of the LibraryDb domain**:

- **Book**: Core entity (Id, ISBN, Title, Subtitle, CategoryId, CreatedAt, UpdatedAt, IsDeleted)
- **Author**: Related entity (Id, FirstName, LastName, Email, Bio)
- **Category**: Lookup entity (Id, Name, Description)
- **BookAuthor**: Junction table with extra property (BookId, AuthorId, Role)

This subset demonstrates all key EF Core Code-First concepts without the complexity of the full schema.

### Key Features

#### Relationship Types Demonstrated
- **One-to-Many**: Category → Books
- **Many-to-Many**: Books ↔ Authors (via BookAuthor junction table)
- **Composite Primary Key**: BookAuthor (BookId, AuthorId)

#### Configuration Patterns
- Data Annotations for simple constraints
- Fluent API for complex configurations
- Separate configuration classes (cleaner code organization)
- Property configuration (max length, required, column types)
- Index configuration (unique, composite, filtered)
- Default values and computed columns

#### Advanced Features
- Global query filters (soft delete on Book)
- Cascade delete behaviors
- Required vs optional navigation properties
- Data seeding in migrations

### Migration Commands

```bash
# Add a new migration (after modifying entities)
dotnet ef migrations add MigrationName --project src/DbDemo.Infrastructure.EFCore.CodeFirst

# Apply migrations to database
dotnet ef database update --project src/DbDemo.Infrastructure.EFCore.CodeFirst

# Rollback to previous migration
dotnet ef database update PreviousMigrationName --project src/DbDemo.Infrastructure.EFCore.CodeFirst

# Generate SQL script for production deployment
dotnet ef migrations script --project src/DbDemo.Infrastructure.EFCore.CodeFirst

# Remove last migration (if not applied)
dotnet ef migrations remove --project src/DbDemo.Infrastructure.EFCore.CodeFirst

# List all migrations
dotnet ef migrations list --project src/DbDemo.Infrastructure.EFCore.CodeFirst
```

### When to Use Code-First

**Use Code-First when:**
- ✅ Starting a new "greenfield" project
- ✅ C# developers own schema design
- ✅ Want automatic schema synchronization
- ✅ Rapid prototyping and iteration
- ✅ Simple to moderate schema complexity
- ✅ Database is owned by a single application

**Avoid Code-First when:**
- ❌ Working with existing legacy database
- ❌ DBAs need full control over DDL
- ❌ Complex database features (triggers, spatial data, etc.)
- ❌ Database shared across multiple applications
- ❌ Team prefers SQL-first approach

### Comparison with Other Approaches

**In this solution, we have 4 data access approaches:**

1. **ADO.NET** (`DbDemo.Infrastructure`): Raw SQL, maximum control
2. **SqlKata** (`DbDemo.Infrastructure.SqlKata`): Query builder, middle ground
3. **EF Core Database-First** (`DbDemo.Infrastructure.EFCore`): Scaffolding from existing DB
4. **EF Core Code-First** (`DbDemo.Infrastructure.EFCore.CodeFirst`): Entities as source of truth ← **This project**

See [docs/30-ef-code-first.md](../../docs/30-ef-code-first.md) for comprehensive Code-First tutorial and comparison.

### Getting Started

1. **Review entity classes** in `Entities/` to understand the domain model
2. **Check configuration classes** in `Configuration/` to see Fluent API examples
3. **Examine migrations** in `Migrations/` to understand schema evolution
4. **Review DbContext** (`LibraryCodeFirstDbContext.cs`) for OnModelCreating setup
5. **See documentation** (`docs/30-ef-code-first.md`) for complete tutorial

### Connection String

Uses the same `LibraryDb` connection string as other implementations. The Code-First approach can create its own schema or coexist with the Database-First tables (using different table names).

### Notes

- This is a **simplified demonstration** (4 entities vs 9 in Database-First)
- Focuses on **educational value** over production completeness
- Shows **best practices** for Code-First entity configuration
- **NOT intended to replace** the Database-First implementation
- Demonstrates **both approaches** for comparison and learning

---

**For comprehensive Code-First tutorial, see:** [docs/30-ef-code-first.md](../../docs/30-ef-code-first.md)
