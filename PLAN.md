# ADO.NET Library System Demo - Incremental Development Plan

     ## 📚 Domain: Library Management System
     **Entities**: Books, Authors, Members, Loans, Categories, BookAuthors (many-to-many)

     ---

     ## Phase 1: Foundation & Core Domain (Commits 1-8)

     ### Commit 1: Project Initialization
     - Create solution structure
     - Add `Microsoft.Data.SqlClient` package
     - Setup `appsettings.json` and User Secrets configuration
     - **Doc**: `docs/01-project-setup.md`

     ### Commit 2: Domain Entities
     - Create basic entity classes: `Book`, `Author`, `Member`, `Loan`, `Category`
     - Simple properties with data annotations
     - **Doc**: `docs/02-domain-entities.md`

     ### Commit 3: Domain Logic & Validation
     - Add validation methods (e.g., ISBN validation, email validation)
     - Business rules (loan periods, max books per member)
     - Value objects (ISBN, Email)
     - **Doc**: `docs/03-domain-logic.md`

     ### Commit 4: Domain Unit Tests
     - xUnit test project
     - Test validation rules and business logic
     - **Doc**: `docs/04-domain-testing.md`

     ### Commit 5: Initial Database Schema
     - `/migrations/001_initial_schema.sql` - Create all tables
     - `/migrations/002_add_indexes.sql` - Performance indexes
     - `/migrations/003_seed_data.sql` - Sample data
     - **Doc**: `docs/05-database-schema.md` (explain migration philosophy)

     ### Commit 6: Migration Runner
     - `MigrationRunner` class with transaction support
     - Migrations table to track applied scripts
     - Checksum validation for migration files
     - **Doc**: `docs/06-migration-runner.md` (emphasize manual approach, no up/down)

     ### Commit 7: First Repository - BookRepository
     - Interface `IBookRepository` with basic CRUD
     - Implementation with parameterized queries
     - Async methods with proper using statements
     - **Doc**: `docs/07-repository-pattern.md`

     ### Commit 8: Console Application Skeleton
     - Basic menu structure
     - Run migrations on startup
     - Simple CRUD demo for books
     - **Doc**: `docs/08-console-app.md`

     ---

     ## Phase 2: Complete CRUD Infrastructure (Commits 9-12)

     ### Commit 9: All Repository Implementations
     - `AuthorRepository`, `MemberRepository`, `LoanRepository`, `CategoryRepository`
     - Implement paging for list operations
     - Search/filter capabilities
     - **Doc**: `docs/09-complete-repositories.md`

     ### Commit 10: Infrastructure Integration Tests
     - Test repositories against real SQL Server (LocalDB/Express)
     - Setup/teardown database per test
     - **Doc**: `docs/10-infrastructure-testing.md` (note: not typical, but for demo)

     ### Commit 11: Automated Demo Scenarios
     - Pre-scripted scenarios: "Create book → Loan to member → Return"
     - Demo runner class
     - **Doc**: `docs/11-automated-demos.md`

     ### Commit 12: Connection Pooling Demonstration
     - Performance test with/without pooling
     - Simple Stopwatch measurements
     - **Doc**: `docs/12-connection-pooling.md`

     ---

     ## Phase 3: Transactions & Async (Commits 13-15)

     ### Commit 13: Multi-Step Transaction - Loan Processing
     - `LoanService.CreateLoanAsync()` - check availability, create loan, update inventory
     - Rollback demonstration on failure
     - **Doc**: `docs/13-transactions.md`

     ### Commit 14: Isolation Levels Demo
     - Create scenarios demonstrating dirty reads, phantom reads
     - Compare isolation levels (ReadCommitted, RepeatableRead, Snapshot, Serializable)
     - **Doc**: `docs/14-isolation-levels.md`

     ### Commit 15: Async with CancellationToken
     - Long-running search operation with `WAITFOR DELAY`
     - Cancellation support in repositories
     - Demo with timeout
     - **Doc**: `docs/15-async-cancellation.md`

     ---

     ## Phase 4: Bulk Operations & Performance (Commits 16-18)

     ### Commit 16: SqlBulkCopy Implementation
     - Bulk import books from CSV/JSON
     - Performance comparison vs. individual inserts
     - **Doc**: `docs/16-bulk-copy.md`

     ### Commit 17: Table-Valued Parameters
     - Create TVP type for books
     - Stored procedure with TVP
     - C# implementation
     - **Doc**: `docs/17-table-valued-parameters.md`

     ### Commit 18: BenchmarkDotNet Comparisons
     - Benchmark project
     - Compare: Individual INSERT vs TVP vs SqlBulkCopy
     - Connection pooling impact
     - **Doc**: `docs/18-benchmarking.md`

     ---

     ## Phase 5: Advanced SQL Features (Commits 19-27)

     ### Commit 19: Audit Trigger
     - Migration: `004_audit_trigger.sql`
     - Create `BookAuditLog` table
     - Trigger on Books table for INSERT/UPDATE/DELETE
     - Demo audit trail viewing
     - **Doc**: `docs/19-audit-triggers.md`

     ### Commit 20: Stored Procedure - Overdue Loans Report
     - Migration: `005_overdue_loans_procedure.sql`
     - Create `sp_GetOverdueLoans` with parameters
     - Call from C# with output parameters
     - **Doc**: `docs/20-stored-procedures.md`

     ### Commit 21: Scalar Function - Late Fee Calculation
     - Migration: `006_late_fee_function.sql`
     - Create `fn_CalculateLateFee(loanId)`
     - Use in SELECT queries from C#
     - **Doc**: `docs/21-scalar-functions.md`

     ### Commit 22: Table-Valued Function - Member Statistics
     - Migration: `007_member_stats_function.sql`
     - Create `fn_GetMemberStatistics(memberId)` returning table
     - Query and materialize results
     - **Doc**: `docs/22-table-valued-functions.md`

     ### Commit 23: Window Functions - Reading Trends
     - Migration: `008_reading_trends_view.sql`
     - Use `ROW_NUMBER()`, `RANK()`, `DENSE_RANK()` for popular books
     - `LAG()`/`LEAD()` for comparing periods
     - **Doc**: `docs/23-window-functions.md`

     ### Commit 24: Recursive CTE - Category Hierarchy
     - Migration: `009_category_hierarchy.sql`
     - Add parent-child relationship to Categories
     - Recursive CTE to get full hierarchy
     - **Doc**: `docs/24-recursive-cte.md`

     ### Commit 25: PIVOT - Monthly Loan Statistics
     - Dynamic PIVOT for loans per category per month
     - Demonstrate UNPIVOT as well
     - **Doc**: `docs/25-pivot-unpivot.md`

     ### Commit 26: Temporary Tables & Performance
     - Use temp tables for complex multi-step queries
     - Compare performance vs. CTEs
     - `#TempTable` vs `@TableVariable` vs CTE
     - **Doc**: `docs/26-temp-tables.md`

     ### Commit 27: Advanced Aggregations
     - `GROUPING SETS`, `ROLLUP`, `CUBE` for multi-dimensional reporting
     - Library statistics dashboard
     - **Doc**: `docs/27-advanced-aggregations.md`

     ---

     ## Additional Suggestions for Advanced Features:

     ### Commit 28: JSON Support (SQL Server 2016+)
     - Store book metadata as JSON column
     - `JSON_VALUE()`, `JSON_QUERY()`, `OPENJSON()`
     - **Doc**: `docs/28-json-support.md`

     ### Commit 29: Computed Columns & Persisted
     - Add computed columns (e.g., `FullName` for authors)
     - Persisted computed columns with indexes
     - **Doc**: `docs/29-computed-columns.md`

     ### Commit 30: Spatial Data (Optional)
     - Library branch locations with geography data type
     - Distance calculations for nearest branch
     - **Doc**: `docs/30-spatial-data.md`

     ---

     ## Documentation Structure

     Each `docs/XX-topic.md` will contain:
     1. **Concept Overview** - What we're implementing and why
     2. **Code Changes** - What files were added/modified
     3. **Key Learning Points** - What students should understand
     4. **Try It Yourself** - Exercises for students
     5. **Common Pitfalls** - What to avoid
     6. **Further Reading** - Links to official docs

     Plus master docs:
     - `docs/00-overview.md` - Project roadmap
     - `docs/migration-philosophy.md` - Deep dive on manual migrations
     - `docs/ado-net-best-practices.md` - Summary of patterns used

     ---

     ## Key Principles Throughout:

     ✅ All SQL queries parameterized (no string concatenation)
     ✅ Proper `using` statements for all IDisposable
     ✅ Async/await consistently
     ✅ No magic strings - constants for SQL
     ✅ Comprehensive error handling
     ✅ Logging (simple Console for now)
     ✅ Self-contained code (minimal dependencies)

     ---

     **Ready to start with Commit 1?** Each step will be reviewable before committing.
     # ADO.NET Library System Demo - Incremental Development Plan (REVISED)

     ## 📚 Domain: Library Management System
     **Entities**: Books, Authors, Members, Loans, Categories, BookAuthors (many-to-many)

     ---

     ## Phase 0: Environment Setup (Commits 1-2)

     ### Commit 1: Docker Compose for SQL Server
     - `docker-compose.yml` with SQL Server 2022
     - Volume for data persistence
     - `.env` for SA password
     - **Doc**: `docs/00-docker-setup.md`
       - What is Docker? Why use it?
       - Container vs VM basics
       - How to start/stop: `docker-compose up -d` / `down`
       - Connecting from host machine
       - Links: Docker intro, SQL Server image docs, Docker Compose docs

     ### Commit 2: Project Initialization
     - Solution structure with `/src`, `/tests`, `/migrations`, `/docs`
     - `.csproj` with `Microsoft.Data.SqlClient` only (self-contained)
     - `appsettings.json` with Docker connection string
     - Setup User Secrets as alternative
     - **Doc**: `docs/01-project-setup.md`
       - Solution structure explanation
       - Package management with NuGet
       - Configuration hierarchy (appsettings → secrets)
       - Links: .NET CLI docs, User Secrets docs, Microsoft.Data.SqlClient

     ---

     ## Phase 1: Core Domain (Commits 3-6)

     ### Commit 3: Domain Entities
     - Simple POCOs: `Book`, `Author`, `Member`, `Loan`, `Category`
     - Basic properties, no validation yet
     - **Doc**: `docs/02-domain-entities.md`
       - Entities vs Value Objects concept
       - POCOs explained
       - Navigation properties (future with ORM)
       - Links: DDD basics, C# record types

     ### Commit 4: Domain Logic & Validation
     - Add validation: ISBN format, email, date ranges
     - Business rules: max loan period (14 days), max books (5 per member)
     - Value objects: `ISBN`, `Email` with implicit operators
     - **Doc**: `docs/03-domain-logic.md`
       - Encapsulation principles
       - Value objects pattern
       - Validation strategies
       - Links: DDD value objects, FluentValidation (for reference)

     ### Commit 5: Domain Unit Tests
     - xUnit project: `DbDemo.Domain.Tests`
     - Test all validation rules
     - Theory tests with inline data
     - **Doc**: `docs/04-domain-testing.md`
       - AAA pattern (Arrange-Act-Assert)
       - Fact vs Theory
       - Running tests with `dotnet test`
       - Links: xUnit docs, unit testing best practices

     ### Commit 6: Test Infrastructure Setup
     - Base test class for integration tests
     - `TestDatabaseFixture` with Docker container management
     - Helper methods for test data cleanup
     - **Doc**: `docs/05-test-infrastructure.md`
       - Unit vs Integration tests
       - Test fixtures and IClassFixture
       - Database isolation strategies
       - Links: xUnit fixtures, Testcontainers (alternative approach)

     ---

     ## Phase 2: Database & Migrations (Commits 7-9)

     ### Commit 7: Initial Database Schema
     - `/migrations/V001__initial_schema.sql` - All tables with PKs/FKs
     - `/migrations/V002__basic_indexes.sql` - Clustered indexes only
     - **Doc**: `docs/06-database-schema.md`
       - SQL Server data types explained
       - Primary/Foreign keys
       - Normalization (1NF, 2NF, 3NF basics)
       - Links: SQL Server data types, database normalization

     ### Commit 8: Migration Philosophy & Runner
     - `MigrationRunner` class
     - `__MigrationsHistory` table (version, filename, checksum, applied_at)
     - Transaction per migration (all-or-nothing)
     - Checksum validation prevents tampering
     - **Doc**: `docs/07-migration-philosophy.md` ⭐ **DEEP DIVE**
       - Why no up/down migrations (test only one path)
       - Idempotency with `IF NOT EXISTS`
       - Forward-only migrations (new file to fix old one)
       - Checksum calculation (SHA256)
       - Transaction handling
       - Links: Evolutionary Database Design, DbUp philosophy

     ### Commit 9: Seed Data Migration
     - `/migrations/V003__seed_data.sql`
     - Sample books, authors, members
     - **Doc**: `docs/08-seed-data.md`
       - MERGE vs INSERT with IF NOT EXISTS
       - Idempotent inserts
       - Test data strategies
       - Links: SQL MERGE, data seeding patterns

     ---

     ## Phase 3: First Repository - Anti-patterns → Best Practices (Commits 10-17)

     ### Commit 10: ⚠️ VULNERABLE BookRepository (SQL Injection)
     - `BookRepository.GetByTitle(string title)` - **STRING CONCATENATION**
     - Works but vulnerable
     - **Doc**: `docs/09-sql-injection-danger.md` ⚠️
       - What is SQL injection?
       - Demo: `'; DROP TABLE Books; --`
       - Why it's dangerous
       - **Red warning box: DO NOT USE IN PRODUCTION**
       - Links: OWASP SQL Injection, Bobby Tables

     ### Commit 11: ✅ FIX: Parameterized Queries
     - Replace with `SqlParameter` / `AddWithValue`
     - All methods now safe
     - **Integration test**: Try injection attempt, verify it fails safely
     - **Doc**: `docs/10-parameterized-queries.md` ✅
       - How parameters work
       - AddWithValue vs Add with explicit type
       - Performance benefits (query plan caching)
       - Links: SqlParameter docs, SQL parameterization

     ### Commit 12: ⚠️ NO Resource Disposal (Memory Leak)
     - Remove `using` statements temporarily
     - Connections pile up
     - **Doc**: `docs/11-resource-leak-danger.md` ⚠️
       - What happens without disposal
       - Connection pool exhaustion demo
       - Memory profiling basics
       - Links: IDisposable pattern, using statement

     ### Commit 13: ✅ FIX: Proper Using Statements
     - `await using` for SqlConnection, SqlCommand, SqlDataReader
     - **Integration test**: Verify no connection leaks
     - **Doc**: `docs/12-proper-disposal.md` ✅
       - using vs await using
       - try-finally manual disposal
       - Best practices
       - Links: IAsyncDisposable, using declarations

     ### Commit 14: ⚠️ SELECT * Anti-pattern
     - All queries use `SELECT *`
     - **Doc**: `docs/13-select-star-problem.md` ⚠️
       - Performance impact
       - Breaking changes when schema evolves
       - Network overhead
       - Links: SQL performance tips

     ### Commit 15: ✅ FIX: Explicit Column Selection
     - Rewrite all queries with specific columns
     - Use ordinal reader (`GetInt32(0)`) or name-based (`reader["Id"]`)
     - **Integration test**: Verify correct data mapping
     - **Doc**: `docs/14-explicit-columns.md` ✅
       - SqlDataReader performance
       - Ordinal vs name-based access
       - Links: SqlDataReader best practices

     ### Commit 16: Complete BookRepository with CRUD
     - Create, GetById, GetPaged, Update, Delete
     - All async with CancellationToken support
     - **Integration tests** for all methods
     - **Doc**: `docs/15-complete-crud.md`
       - Repository pattern
       - Paging with OFFSET/FETCH
       - Soft delete vs hard delete
       - Links: Repository pattern, OFFSET-FETCH

     ### Commit 17: Remaining Repositories
     - AuthorRepository, MemberRepository, CategoryRepository
     - **Integration tests** for each
     - **Doc**: `docs/16-all-repositories.md`
       - Highlights of interesting queries per repo
       - Search with LIKE and wildcards
       - Sorting strategies
       - Links: SQL LIKE patterns

     ---

     ## Phase 4: Console Application (Commits 18-20)

     ### Commit 18: Console App Skeleton
     - `Program.cs` with basic menu
     - Run migrations on startup
     - Simple book CRUD demo
     - **Doc**: `docs/17-console-app.md`
       - Console app structure
       - User input validation
       - Menu-driven design
       - Links: Console class docs

     ### Commit 19: Automated Demo Scenarios
     - `DemoRunner` class with pre-scripted flows
     - "Happy path" scenarios
     - **Doc**: `docs/18-automated-demos.md`
       - Test automation for demos
       - Scripted vs interactive
       - Links: Test automation patterns

     ### Commit 20: Interactive Mode
     - Full CRUD operations via menu
     - Both modes available
     - **Doc**: `docs/19-interactive-mode.md`
       - User experience design
       - Input validation and error messages
       - Links: Command pattern (optional reading)

     ---

     ## Phase 5: Transactions & Concurrency (Commits 21-25)

     ### Commit 21: ⚠️ Multi-Step Operation WITHOUT Transaction
     - `LoanService.CreateLoan()` - NO transaction wrapper
     - Partial updates possible on error
     - **Deliberately cause failure** mid-operation
     - **Doc**: `docs/20-transaction-problem.md` ⚠️
       - ACID properties
       - Partial update danger
       - Data corruption scenarios
       - Links: Database transactions intro

     ### Commit 22: ✅ FIX: Proper Transaction Handling
     - Wrap in `BeginTransactionAsync` / `CommitAsync` / `RollbackAsync`
     - **Integration test**: Verify rollback on error
     - **Doc**: `docs/21-transactions.md` ✅
       - Transaction lifecycle
       - Error handling patterns
       - Savepoints (advanced)
       - Links: SqlTransaction docs, ACID properties

     ### Commit 23: Isolation Levels - Dirty Reads Demo
     - Create scenario with two concurrent connections
     - ReadUncommitted allows dirty reads
     - **Doc**: `docs/22-isolation-levels-intro.md`
       - Four isolation levels
       - Concurrency problems (dirty, non-repeatable, phantom)
       - Links: SQL Server isolation levels, concurrency control

     ### Commit 24: Isolation Levels - Full Comparison
     - Test all levels with integration tests
     - Demonstrate each problem type
     - **Doc**: `docs/23-isolation-comparison.md`
       - When to use each level
       - Performance trade-offs
       - Snapshot isolation special case
       - Links: Snapshot isolation, locking hints

     ### Commit 25: Async & CancellationToken
     - Long-running search with `WAITFOR DELAY`
     - Cancellation support throughout
     - **Integration test**: Verify cancellation works
     - **Doc**: `docs/24-async-cancellation.md`
       - Async/await pattern
       - CancellationToken propagation
       - Timeout strategies
       - Links: Task-based async pattern, CancellationTokenSource

     ---

     ## Phase 6: Performance & Bulk Operations (Commits 26-31)

     ### Commit 26: ⚠️ N+1 Query Problem
     - Load all loans with related books using loop
     - One query per loan (naive approach)
     - **Measure performance** with Stopwatch
     - **Doc**: `docs/25-n-plus-1-problem.md` ⚠️
       - What is N+1?
       - How to detect it
       - Performance impact
       - Links: N+1 query problem explained

     ### Commit 27: ✅ FIX: JOIN Query
     - Single query with JOIN
     - **Integration test**: Same results, faster
     - **Doc**: `docs/26-join-optimization.md` ✅
       - INNER vs LEFT JOIN
       - Multi-table queries
       - When to denormalize
       - Links: SQL JOIN types

     ### Commit 28: Connection Pooling Demo
     - Test with/without pooling (`Pooling=false`)
     - Measure with Stopwatch
     - **Doc**: `docs/27-connection-pooling.md`
       - How pooling works
       - Pool size configuration
       - Monitoring pool stats
       - Links: SQL Server connection pooling

     ### Commit 29: SqlBulkCopy
     - Bulk import 10,000 books
     - Compare to individual INSERTs
     - **Integration test**: Verify all inserted
     - **Doc**: `docs/28-bulk-copy.md`
       - SqlBulkCopy API
       - DataTable mapping
       - Batch size tuning
       - Links: SqlBulkCopy docs, bulk operations

     ### Commit 30: Table-Valued Parameters
     - `/migrations/V004__book_tvp.sql` - CREATE TYPE, CREATE PROCEDURE
     - C# implementation with DataTable
     - **Integration test**: Bulk insert via TVP
     - **Doc**: `docs/29-table-valued-parameters.md`
       - TVP vs SqlBulkCopy
       - When to use each
       - Stored procedure benefits
       - Links: TVP docs, structured types

     ### Commit 31: BenchmarkDotNet Suite
     - Add `BenchmarkDotNet` package (justified external dep)
     - Benchmark project with comparisons:
       - Individual INSERT vs TVP vs SqlBulkCopy
       - Connection pooling on/off
       - Sync vs Async
     - **Doc**: `docs/30-benchmarking.md`
       - BenchmarkDotNet basics
       - Reading results
       - Benchmark methodology
       - Links: BenchmarkDotNet docs, performance testing

     ---

     ## Phase 7: Advanced SQL Features (Commits 32-42)

     ### Commit 32: ⚠️ Missing Indexes (Slow Queries)
     - `/migrations/V005__remove_indexes.sql` - Drop non-clustered indexes
     - Measure slow queries
     - **Doc**: `docs/31-missing-indexes-problem.md` ⚠️
       - Query execution plans
       - Index scan vs seek
       - Links: SQL Server execution plans, indexing basics

     ### Commit 33: ✅ FIX: Proper Indexes
     - `/migrations/V006__add_performance_indexes.sql`
     - Covering indexes, filtered indexes
     - **Integration test**: Query performance comparison
     - **Doc**: `docs/32-index-optimization.md` ✅
       - Index types (clustered, non-clustered, covering)
       - Index maintenance
       - Over-indexing dangers
       - Links: Index design guide, index fragmentation

     ### Commit 34: Audit Trigger
     - `/migrations/V007__audit_trigger.sql`
     - `BookAuditLog` table + trigger on Books
     - Demo in console app
     - **Integration test**: Verify audit records created
     - **Doc**: `docs/33-audit-triggers.md`
       - Trigger types (AFTER, INSTEAD OF)
       - INSERTED/DELETED tables
       - Trigger performance considerations
       - Links: SQL Server triggers, audit patterns

     ### Commit 35: Stored Procedure - Overdue Loans
     - `/migrations/V008__overdue_loans_sp.sql`
     - `sp_GetOverdueLoans` with date parameter, output parameter for count
     - C# call with `SqlParameter.Direction = Output`
     - **Integration test**: Verify correct results
     - **Doc**: `docs/34-stored-procedures.md`
       - Stored procedures vs inline SQL
       - Input/output parameters
       - Return values
       - Links: CREATE PROCEDURE, sp best practices

     ### Commit 36: Scalar Function - Late Fee Calculation
     - `/migrations/V009__late_fee_function.sql`
     - `dbo.fn_CalculateLateFee(@loanId)` - complex calculation
     - Use in SELECT from C#
     - **Integration test**: Validate fee amounts
     - **Doc**: `docs/35-scalar-functions.md`
       - Scalar vs table-valued functions
       - Performance implications
       - Deterministic functions
       - Links: CREATE FUNCTION, scalar UDFs

     ### Commit 37: Table-Valued Function - Member Stats
     - `/migrations/V010__member_stats_tvf.sql`
     - `dbo.fn_GetMemberStats(@memberId)` returns table
     - JOIN with function results
     - **Integration test**: Verify stats accuracy
     - **Doc**: `docs/36-table-valued-functions.md`
       - Inline vs multi-statement TVF
       - Performance differences
       - When to use TVFs
       - Links: Table-valued functions, TVF performance

     ### Commit 38: Window Functions - Popular Books
     - `/migrations/V011__popular_books_view.sql`
     - ROW_NUMBER(), RANK(), DENSE_RANK() for top books
     - LAG()/LEAD() for trend analysis
     - **Integration test**: Verify rankings
     - **Doc**: `docs/37-window-functions.md`
       - Window function syntax
       - OVER() clause and partitioning
       - Use cases for analytics
       - Links: Window functions, OVER clause

     ### Commit 39: Recursive CTE - Category Hierarchy
     - `/migrations/V012__category_hierarchy.sql`
     - Add ParentCategoryId, update seed data
     - Recursive CTE query from C#
     - **Integration test**: Verify full tree
     - **Doc**: `docs/38-recursive-cte.md`
       - CTE basics
       - Recursive CTE structure (anchor + recursive)
       - Infinite loop prevention
       - Links: WITH clause, recursive queries

     ### Commit 40: PIVOT - Monthly Statistics
     - Complex query with PIVOT for loans/month by category
     - Dynamic PIVOT demo
     - UNPIVOT reverse example
     - **Integration test**: Verify pivot results
     - **Doc**: `docs/39-pivot-unpivot.md`
       - PIVOT syntax
       - Dynamic SQL for dynamic PIVOT
       - UNPIVOT use cases
       - Links: PIVOT operator, dynamic SQL

     ### Commit 41: Temporary Tables & Performance
     - Create complex report using #TempTable, @TableVariable, CTE
     - Performance comparison
     - **Integration test**: All three methods produce same result
     - **Doc**: `docs/40-temp-tables.md`
       - #TempTable vs @TableVariable vs CTE
       - Scope and lifetime
       - Statistics and indexing
       - Links: Temporary tables, table variables

     ### Commit 42: Advanced Aggregations
     - GROUPING SETS, ROLLUP, CUBE for multi-dimensional stats
     - Library dashboard query
     - **Integration test**: Verify aggregation levels
     - **Doc**: `docs/41-advanced-aggregations.md`
       - GROUPING SETS explained
       - ROLLUP vs CUBE
       - GROUPING() function
       - Links: GROUP BY extensions, ROLLUP/CUBE

     ---

     ## Phase 8: Bonus Advanced Features (Commits 43-46)

     ### Commit 43: JSON Support
     - `/migrations/V013__book_metadata_json.sql`
     - Add JSON column for flexible metadata
     - JSON_VALUE(), JSON_QUERY(), OPENJSON() demos
     - **Integration test**: Query JSON properties
     - **Doc**: `docs/42-json-support.md`
       - JSON in SQL Server
       - Indexing JSON
       - FOR JSON AUTO
       - Links: JSON functions, JSON path expressions

     ### Commit 44: Computed Columns
     - `/migrations/V014__computed_columns.sql`
     - Add persisted computed columns
     - Index on computed column
     - **Integration test**: Verify computation
     - **Doc**: `docs/43-computed-columns.md`
       - Computed vs persisted computed
       - Deterministic requirements
       - Performance benefits
       - Links: Computed columns, indexing computed columns

     ### Commit 45: Full-Text Search
     - `/migrations/V015__fulltext_catalog.sql`
     - Full-text index on Books
     - CONTAINS(), FREETEXT() queries
     - **Integration test**: Relevance ranking
     - **Doc**: `docs/44-fulltext-search.md`
       - Full-text vs LIKE
       - Catalog and index management
       - Thesaurus and stoplists
       - Links: Full-text search, CONTAINS syntax

     ### Commit 46: Spatial Data (Optional/Bonus)
     - `/migrations/V016__library_locations.sql`
     - GEOGRAPHY type for branch locations
     - Distance calculations
     - **Integration test**: Nearest branch query
     - **Doc**: `docs/45-spatial-data.md`
       - GEOGRAPHY vs GEOMETRY
       - Spatial indexes
       - STDistance() and other methods
       - Links: Spatial data, spatial reference systems

     ---

     ## Master Documentation Files

     - `docs/00-overview.md` - Complete project roadmap with commit graph
     - `docs/README.md` - How to use this demo repository
     - `docs/troubleshooting.md` - Common issues and solutions
     - `docs/resources.md` - Curated learning resources by topic
     - `docs/glossary.md` - ADO.NET and SQL terms explained

     ---

     ## Documentation Template (Each doc file)

     ```markdown
     # [Number] - [Title]

     ## 📖 What You'll Learn
     - Bullet points of concepts

     ## 🎯 Why This Matters
     - Real-world relevance

     ## 🔍 Key Concepts
     - Detailed explanations with examples (no full code, just snippets)

     ## ⚠️ Common Pitfalls (if applicable)
     - What to avoid

     ## ✅ Best Practices
     - What to follow

     ## 🧪 Testing This Feature
     - How integration tests verify it

     ## 🔗 Learn More
     - Link to official docs
     - Link to tutorials
     - Link to related resources

     ## ❓ Discussion Questions
     - For students to think about
     ```

     ---

     ## Key Principles

     ✅ Every SQL change has integration test
     ✅ Anti-patterns shown first, then fixed (with clear warnings)
     ✅ Docs are beginner-friendly with extensive links
     ✅ Docker makes setup trivial
     ✅ Each commit is reviewable and teaches ONE thing
     ✅ No magic libraries (except BenchmarkDotNet, justified)

     **Ready to start with Commit 1: Docker Compose?**