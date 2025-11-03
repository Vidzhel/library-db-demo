# Library Management System - Database Programming Demo

A comprehensive demonstration project for learning database programming in .NET, built incrementally to showcase best practices and common patterns across multiple data access approaches.

## 🎓 About This Project

This project is designed as a teaching tool for students learning database programming with .NET. It demonstrates **three complementary approaches** to data access:

### 1. ADO.NET (Raw SQL)
- Direct database access using `Microsoft.Data.SqlClient`
- Manual migration management
- CRUD operations with parameterized queries
- Transactions and isolation levels
- Async programming with cancellation support
- Bulk operations (SqlBulkCopy and Table-Valued Parameters)
- Advanced SQL features (triggers, stored procedures, functions, CTEs, window functions)
- Performance optimization and benchmarking

### 2. Entity Framework Core - Database-First
- Scaffolding existing databases to generate entity classes
- Reverse engineering database schema
- Working with databases where schema already exists
- Understanding Database-First workflow

### 3. Entity Framework Core - Code-First
- Pure POCO entities without Data Annotations
- Fluent API for all configuration
- EF Core migrations system
- Advanced patterns: soft delete, computed columns, composite keys
- Global query filters and automatic timestamp management
- Semi-rich domain entities with behavior methods

### Cross-Cutting Concerns
- **Clean Architecture** with multi-project structure
- Repository pattern with interface-based abstraction
- Separation of concerns (Domain, Application, Infrastructure)
- Multiple infrastructure implementations (swappable data access)

## 🚀 Quick Start

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop) installed
- [.NET 9 SDK](https://dotnet.microsoft.com/download) installed

### First-Time Setup

Start SQL Server:
```bash
cp .env.example .env  # Edit with your SA password
docker-compose -f .meta/docker-compose.yml up --build -d
```

### Running the Application

After first-time setup, simply run:

```bash
dotnet run --project src/DbDemo.ConsoleApp
```

The application will:
1. Connect to the database
2. Show an interactive menu with demos
3. Allow you to explore various ADO.NET features

### When to Re-run Setup

Run `dotnet run --project src/DbDemo.Setup` again when:
- 🔄 You pull new migration files from git
- 🔄 You create new migrations
- 🔄 Database schema changes

This ensures:
- Migrations are applied
- Generated schema constants are up-to-date
- Your code compiles correctly

### Refer to Documentation

- See `docs/` folder for detailed guides on each feature
- Start with `docs/00-docker-setup.md` and progress sequentially
- Each commit has corresponding documentation

### Run tests

```bash
dotnet test tests/DbDemo.Domain.Tests
```

```bash
dotnet test tests/DbDemo.Integration.Tests
```


### Run benchmarks

```bash
dotnet run --project benchmarks/DbDemo.Benchmarks/ --configuration Release -- all
```

## 📚 Learning Path

This project is organized as a series of commits, each teaching a specific concept:

### Phase 0: Environment Setup
- **Commit 1**: Docker Compose for SQL Server → `docs/00-docker-setup.md`
- **Commit 2**: Project initialization → `docs/01-project-setup.md`

### Phase 1: Core Domain
- **Commit 3**: Domain entities
- **Commit 4**: Domain logic & validation
- **Commit 5**: Domain unit tests
- **Commit 6**: Test infrastructure

### Phase 2: Database & Migrations
- **Commit 7**: Initial database schema
- **Commit 8**: Migration runner (manual approach)
- **Commit 9**: Seed data

### Phase 3+: And many more!

Each commit is documented in the `docs/` folder with detailed explanations, examples, and links to further resources.

## 📖 Documentation Structure

All documentation is in the `docs/` folder:

Each document includes:
- 📖 What You'll Learn
- 🎯 Why This Matters
- 🔍 Key Concepts
- ⚠️ Common Pitfalls
- ✅ Best Practices
- 🔗 Learn More (extensive links to resources)
- ❓ Discussion Questions

## 🛠️ Project Structure

The project follows **Clean Architecture** principles with clear separation of concerns:

```
DbDemo/
├── docker-compose.yml          # SQL Server container definition
├── .env.example               # Environment template
├── DbDemo.sln                 # Solution file
│
├── src/
│   ├── DbDemo.Domain/         # 🔵 Core Domain Layer (No dependencies)
│   │   └── Entities/          # Domain entities (Book, Author, Member, Loan, etc.)
│   │
│   ├── DbDemo.Application/    # 🟢 Application Layer (Depends on: Domain)
│   │   ├── Services/          # Business logic (LoanService)
│   │   ├── Repositories/      # Repository interfaces (I*Repository)
│   │   └── DTOs/              # Data Transfer Objects
│   │
│   ├── DbDemo.Infrastructure/ # 🟡 Infrastructure Layer - ADO.NET (Depends on: Domain, Application)
│   │   ├── Repositories/      # Repository implementations (ADO.NET)
│   │   ├── Migrations/        # Database migration system
│   │   └── BulkOperations/    # SqlBulkCopy & TVP implementations
│   │
│   ├── DbDemo.Infrastructure.SqlKata/  # 🟡 Infrastructure Layer - SqlKata (Query builder)
│   │   ├── Generated/         # Auto-generated schema constants
│   │   ├── Repositories/      # Repository implementations (SqlKata)
│   │   └── QueryFactoryProvider.cs
│   │
│   ├── DbDemo.Infrastructure.EFCore.CodeFirst/  # 🟡 Infrastructure Layer - EF Core Code-First
│   │   ├── Entities/          # Infrastructure entities (Category, Author, Book, BookAuthor)
│   │   ├── Configuration/     # Fluent API entity configurations
│   │   ├── Repositories/      # Repository implementations (EF Core)
│   │   ├── Migrations/        # EF Core migrations
│   │   └── LibraryCodeFirstDbContext.cs
│   │
│   ├── DbDemo.Scaffolding/    # 🔧 Schema Code Generator
│   │   └── Program.cs         # Reads INFORMATION_SCHEMA, generates constants
│   │
│   ├── DbDemo.Setup/          # 🔧 Setup Tool
│   │   └── Program.cs         # Runs migrations + scaffolding
│   │
│   ├── DbDemo.Demos/          # 🟣 Demo Scenarios (Depends on: Domain, Application, Infrastructure)
│   │   └── *Demo.cs           # Demo runners for various features
│   │
│   └── DbDemo.ConsoleApp/     # 🔴 UI Layer - Swappable! (Depends on: All)
│       └── Program.cs         # Console interface only
│
├── tests/
│   ├── DbDemo.Domain.Tests/      # Domain unit tests
│   └── DbDemo.Integration.Tests/ # Integration tests
│
├── migrations/                # SQL migration scripts
├── docs/                      # Detailed documentation
└── scripts/                   # Utility scripts
    └── init/                  # Docker initialization scripts
```

### Dependency Flow (Clean Architecture)

```
DbDemo.Domain (Core - No dependencies)
    ↑
DbDemo.Application (Business Logic)
    ↑
DbDemo.Infrastructure.* (Data Access - Multiple implementations)
    ├── DbDemo.Infrastructure (ADO.NET)
    ├── DbDemo.Infrastructure.SqlKata (Query Builder)
    └── DbDemo.Infrastructure.EFCore.CodeFirst (EF Core)
    ↑
DbDemo.Demos, DbDemo.ConsoleApp (Outer Layer)
```

**Key principle**: Dependencies point inward. The Domain layer has no external dependencies and contains pure business logic.

**Multiple Infrastructure Implementations**: The project demonstrates the **Strategy Pattern** with three interchangeable data access implementations. All implement the same repository interfaces defined in the Application layer, allowing you to switch between ADO.NET, SqlKata, and EF Core Code-First at runtime.

### Architecture Benefits

✅ **Testability** - Domain and Application layers can be tested without databases
✅ **Maintainability** - Each project has a single, well-defined responsibility
✅ **Flexibility** - Easy to swap implementations (e.g., replace console with web API)
✅ **Scalability** - Clear boundaries make it easier to grow and refactor
✅ **Reusability** - Domain and Application layers can be used by multiple UIs

### Project Responsibilities

| Project | Responsibility | Can Reference |
|---------|---------------|---------------|
| **DbDemo.Domain** | Business entities, domain logic, validation | Nothing (pure domain) |
| **DbDemo.Application** | Use cases, business workflows, interfaces | Domain |
| **DbDemo.Infrastructure** | Database access via ADO.NET | Domain, Application |
| **DbDemo.Infrastructure.SqlKata** | Database access via SqlKata query builder | Domain, Application |
| **DbDemo.Infrastructure.EFCore.CodeFirst** | Database access via EF Core Code-First | Domain, Application |
| **DbDemo.Demos** | Feature demonstrations | Domain, Application, Infrastructure |
| **DbDemo.ConsoleApp** | User interface (console) | All projects |

## 🎯 Learning Objectives

After working through this project, you will understand:

1. ✅ **Clean Architecture** - Multi-project structure with proper separation of concerns
2. ✅ **ADO.NET** fundamentals and architecture
3. ✅ **Entity Framework Core** - Both Database-First and Code-First approaches
4. ✅ **Repository Pattern** - Interface-based abstraction for data access
5. ✅ **Strategy Pattern** - Multiple interchangeable infrastructure implementations
6. ✅ Safe database access (parameterized queries, preventing SQL injection)
7. ✅ Resource management (using statements, connection pooling)
8. ✅ Transaction handling and isolation levels
9. ✅ Async/await patterns with databases
10. ✅ Performance optimization (bulk operations, indexing)
11. ✅ Advanced SQL features (triggers, procedures, functions, window functions, CTEs)
12. ✅ Migration strategies (manual SQL and EF Core migrations)
13. ✅ Testing database code (unit tests and integration tests)
14. ✅ **Domain-Driven Design** - Rich domain models with business logic
15. ✅ **Fluent API** - Type-safe configuration without Data Annotations
16. ✅ **Advanced EF Core patterns** - Soft delete, computed columns, global query filters
17. ✅ **Comparing data access approaches** - Trade-offs between ADO.NET, query builders, and ORMs

## 🔀 Data Access Approaches Comparison

This project demonstrates three different approaches to database access, each with its own trade-offs:

### ADO.NET (Raw SQL)
**Best for:** Performance-critical code, complex queries, full control

**Pros:**
- Maximum performance and control
- No abstraction overhead
- Direct access to all SQL Server features
- Explicit resource management
- Best for stored procedures and complex SQL

**Cons:**
- Most verbose code
- Manual mapping between database and domain objects
- More prone to errors (typos in SQL strings)
- Requires more testing
- Manual schema change tracking

**Use when:** You need maximum performance, complex queries, or full control over SQL execution.

### Entity Framework Core - Code-First
**Best for:** New projects, rapid development, strong typing

**Pros:**
- Type-safe queries (LINQ)
- Automatic change tracking
- Built-in migration system
- Convention-based configuration
- Excellent for CRUD operations
- Clean, maintainable code

**Cons:**
- Performance overhead from abstraction
- Generated SQL may not be optimal
- Learning curve for advanced features
- Can hide what's actually happening
- Not ideal for complex queries

**Use when:** Starting a new project, prioritizing maintainability over raw performance, or when CRUD operations dominate.

### SqlKata (Query Builder)
**Best for:** Dynamic queries, middle ground between ADO.NET and EF Core

**Pros:**
- Type-safe query building
- More performant than EF Core
- More maintainable than raw SQL
- Excellent for dynamic filtering/sorting
- Database-agnostic queries

**Cons:**
- Still requires manual mapping
- No change tracking
- No migration system
- Less feature-rich than EF Core
- Smaller community/ecosystem

**Use when:** You need dynamic query building, want better performance than EF Core, but want more structure than raw ADO.NET.

### Which Should You Choose?

**For this educational project:** Learn all three! Understanding the trade-offs helps you make informed decisions in real projects.

**For real projects:**
- Start with EF Core for most applications
- Use ADO.NET for performance-critical sections
- Consider query builders for complex dynamic queries
- Mix and match within the same application (Clean Architecture allows this!)

## ⚠️ Important Notes

This project demonstrates **both bad and good practices** intentionally:

- Some commits introduce **anti-patterns** (e.g., SQL injection vulnerabilities)
- Immediately following commits show **how to fix** these issues
- All anti-pattern commits are clearly marked with ⚠️ warnings
- **Never use anti-pattern code in production!**

## 🔗 Additional Resources

See the comprehensive resource links in each documentation file under `docs/`.

Key starting points:
- [ADO.NET Overview - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/ado-net-overview)
- [Microsoft.Data.SqlClient Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient)
- [SQL Server Documentation](https://learn.microsoft.com/en-us/sql/sql-server/)

## 💡 How to Use This Project

### As a Student

1. Start with first commit and read the corresponding documentation
2. Review the code changes in each commit
3. Try the "Discussion Questions" at the end of each doc
4. Experiment with the code yourself
5. Progress sequentially through commits

### As an Instructor

1. Use each commit as a lesson plan
2. Live-code the changes while explaining
3. Use the documentation as reference material
4. Assign discussion questions as homework
5. Add your own branches to demonstrate additional concepts

## 🤝 Contributing

This is an educational project. If you find issues or have suggestions for improvement, please open an issue or pull request.

## 📄 License

This project is created for educational purposes.

