# Library Management System - ADO.NET Demo

A comprehensive demonstration project for learning ADO.NET and database programming in .NET, built incrementally to showcase best practices and common patterns.

## 🎓 About This Project

This project is designed as a teaching tool for students learning database programming with ADO.NET. It demonstrates:

- **Clean Architecture** with multi-project structure
- Direct database access using `Microsoft.Data.SqlClient`
- Manual migration management
- CRUD operations with parameterized queries
- Transactions and isolation levels
- Async programming with cancellation support
- Bulk operations (SqlBulkCopy and Table-Valued Parameters)
- Advanced SQL features (triggers, stored procedures, functions, CTEs, etc.)
- Performance optimization and benchmarking
- Repository pattern with interface-based abstraction
- Separation of concerns (Domain, Application, Infrastructure)

## 🚀 Quick Start

**⚡ Want to get started fast?** See [`QUICKSTART.md`](QUICKSTART.md) for a 5-minute setup guide!

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop) installed
- [.NET 8 or 9 SDK](https://dotnet.microsoft.com/download) installed

### Setup Steps

1. **Start SQL Server**:
   ```bash
   cp .env.example .env  # Edit with your SA password
   docker compose up -d
   ```

2. **Configure application secrets**:
   ```bash
   cd DbDemo
   dotnet user-secrets set "ConnectionStrings:SqlServerAdmin" "Server=localhost,1453;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
   dotnet user-secrets set "ConnectionStrings:LibraryDb" "Server=localhost,1453;Database=LibraryDb;User Id=library_app_user;Password=LibraryApp@2024!;TrustServerCertificate=True;"
   ```

3. **Run the application**:
   ```bash
   dotnet run --project src/DbDemo.ConsoleApp
   ```

4. **Refer to detailed documentation**:
   - See `docs/xx-....md` for docs per project part (you can check individual commits)

### Run tests

```bash
cd tests/DbDemo.Domain.Tests && dotnet test
```

```bash
cd tests/DbDemo.Integration.Tests && dotnet test
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
│   ├── DbDemo.Infrastructure/ # 🟡 Infrastructure Layer (Depends on: Domain, Application)
│   │   ├── Repositories/      # Repository implementations (ADO.NET)
│   │   ├── Migrations/        # Database migration system
│   │   └── BulkOperations/    # SqlBulkCopy & TVP implementations
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
DbDemo.Infrastructure (Data Access)
    ↑
DbDemo.Demos, DbDemo.ConsoleApp (Outer Layer)
```

**Key principle**: Dependencies point inward. The Domain layer has no external dependencies and contains pure business logic.

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
| **DbDemo.Infrastructure** | Database access, external services | Domain, Application |
| **DbDemo.Demos** | Feature demonstrations | Domain, Application, Infrastructure |
| **DbDemo.ConsoleApp** | User interface (console) | All projects |

## 🎯 Learning Objectives

After working through this project, you will understand:

1. ✅ **Clean Architecture** - Multi-project structure with proper separation of concerns
2. ✅ **ADO.NET** fundamentals and architecture
3. ✅ **Repository Pattern** - Interface-based abstraction for data access
4. ✅ Safe database access (parameterized queries, preventing SQL injection)
5. ✅ Resource management (using statements, connection pooling)
6. ✅ Transaction handling and isolation levels
7. ✅ Async/await patterns with databases
8. ✅ Performance optimization (bulk operations, indexing)
9. ✅ Advanced SQL features (triggers, procedures, functions, window functions, CTEs)
10. ✅ Manual database migration strategies
11. ✅ Testing database code (unit tests and integration tests)
12. ✅ **Domain-Driven Design** - Rich domain models with business logic

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

