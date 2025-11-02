# 06 - Migration Runner

## 📖 What You'll Learn

- Automated migration execution with C# and ADO.NET
- SHA256 checksum validation for tamper detection
- Transaction-based migrations (atomic all-or-nothing)
- Migration history tracking
- Forward-only migration philosophy
- Idempotent migration patterns

## 🎯 Why This Matters

Manual migration execution is error-prone and doesn't scale:
- **Forgetting migrations** - Did I run V003 on production?
- **Running out of order** - V004 before V002 causes failures
- **Tampering** - Accidentally modifying an applied migration
- **Partial failures** - Migration fails halfway, leaves database in bad state
- **No audit trail** - When was this migration applied? By whom?

**Automated migration runners solve all of these problems!**

### Real-World Impact

Consider deploying to multiple environments:
```
Development → Staging → Production
    ↓            ↓          ↓
   V001-V010   V001-V008  V001-V005
```

**Without automation:**
- Manually track which migrations ran where
- Risk of human error
- No guarantee of consistency

**With automation:**
- Run once: `dotnet run`
- Migrations applied automatically
- Guaranteed consistency
- Full audit trail

> "Automate everything that can be automated. Manual processes are the enemy of reliability." - Site Reliability Engineering

## 🔍 Migration Philosophy

### 1. Forward-Only Migrations

**Never modify an applied migration. Always create a new one.**

**❌ WRONG (modifying V001):**
```sql
-- V001__create_users.sql (MODIFIED - DON'T DO THIS!)
CREATE TABLE Users (
    Id INT,
    Name NVARCHAR(100),
    Email NVARCHAR(255)  -- ← Added this column later
);
```

**✅ RIGHT (new migration):**
```sql
-- V002__add_email_to_users.sql
ALTER TABLE Users ADD Email NVARCHAR(255);
```

**Why?**
- Different environments may have different versions
- Checksum validation catches tampering
- Can't "undo" a migration that's already run
- Test only the forward path (production path)

### 2. No Up/Down Migrations

Many tools (Flyway, Entity Framework) support "down" migrations for rollback.

**We deliberately DON'T support this because:**

1. **Production reality**: You rarely roll back in production
   - Data migrations can't be reversed (what if users added data?)
   - Safer to fix forward (V004 fixes bugs in V003)

2. **Testing complexity**: Must test BOTH up and down paths
   - Doubles your testing burden
   - Down migrations are rarely tested until needed
   - When you need them, they often fail

3. **False sense of security**: "We can always roll back!"
   - Not true with data changes
   - Better to design careful migrations

**Instead of rollback:**
- **Fix forward**: Create V004 to undo V003 changes
- **Test thoroughly** before applying to production
- **Use transactions**: Each migration is atomic

### 3. Idempotent Migrations

**Migrations should be safe to re-run.**

**❌ NOT idempotent:**
```sql
CREATE TABLE Books (...);  -- Fails if table exists!
```

**✅ Idempotent:**
```sql
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'Books'))
BEGIN
    CREATE TABLE Books (...);
END
```

**Benefits:**
- Safe to re-run after failures
- Can replay migrations on new environments
- Easier debugging (just re-run)

### 4. Checksum Validation

Every migration has a SHA256 checksum stored in `__MigrationsHistory`.

**On each run:**
```
1. Calculate checksum of V001__initial_schema.sql file
2. Compare to checksum in database
3. If different → ERROR! File was modified!
```

**This prevents:**
- Accidental modifications
- Intentional tampering
- File corruption
- Environment drift

**Example:**
```
❌ CHECKSUM MISMATCH DETECTED!
The following migrations have been modified after being applied:

  ❌ V001__initial_schema.sql
     Expected: 3F7B9A2E1C8D...
     Actual:   8A1D4F6C2B9E...

⚠️  IMPORTANT: Never modify an applied migration!
   Instead, create a new migration to make changes.
```

## 🏗️ How It Works

### Migration Runner Workflow

```
1. Scan migrations/ folder
   ├─ Find all V*.sql files
   ├─ Skip V000 (bootstrap script)
   └─ Parse version numbers

2. Calculate checksums
   ├─ Read file content
   └─ SHA256 hash

3. Query __MigrationsHistory
   ├─ Get applied migrations
   └─ Compare versions

4. Validate checksums
   ├─ For each applied migration
   ├─ Compare file checksum to database
   └─ Abort if mismatch

5. Get pending migrations
   └─ Migrations not in history table

6. Execute pending migrations
   ├─ Sort by version (V001, V002, V003...)
   ├─ For each migration:
   │   ├─ Begin transaction
   │   ├─ Split by GO statements
   │   ├─ Execute each batch
   │   ├─ Record in history
   │   └─ Commit transaction
   └─ Report results
```

### __MigrationsHistory Table

```sql
CREATE TABLE __MigrationsHistory
(
    MigrationVersion VARCHAR(10) PRIMARY KEY,  -- "001", "002"
    FileName NVARCHAR(255),                     -- "V001__initial_schema.sql"
    Checksum VARCHAR(64),                       -- SHA256 hash
    AppliedAt DATETIME2,                        -- 2024-10-25 23:45:12.345
    ExecutionTimeMs INT                         -- 1234 ms
);
```

**Query migration history:**
```sql
SELECT
    MigrationVersion,
    FileName,
    AppliedAt,
    ExecutionTimeMs
FROM __MigrationsHistory
ORDER BY MigrationVersion;
```

**Example output:**
```
Version  FileName                    AppliedAt                   Time
001      V001__initial_schema.sql    2024-10-25 10:15:30.123    1205ms
002      V002__add_indexes.sql       2024-10-25 10:15:31.456     341ms
003      V003__seed_data.sql         2024-10-25 10:15:31.897    2104ms
```

## 💻 Using the Migration Runner

### Automatic Execution (On Startup)

The application automatically runs migrations when it starts:

```bash
dotnet run --project src/DbDemo.ConsoleApp
```

**Output:**
```
===========================================
Library Management System - ADO.NET Demo
===========================================

✅ Configuration loaded (Environment: Development)

📋 Configuration Summary:
─────────────────────────────────────────
...

========================================
🚀 Migration Runner
========================================

📂 Found 3 migration files
✅ Already applied: 3 migrations
✅ Database is up to date - no pending migrations
========================================
```

### First Run (Applying Migrations)

When migrations haven't been applied yet:

```
========================================
🚀 Migration Runner
========================================

📂 Found 3 migration files
✅ Already applied: 0 migrations
🆕 Pending migrations: 3

⏳ Executing: V001__initial_schema.sql...
   ✅ Success (1205ms)
⏳ Executing: V002__add_indexes.sql...
   ✅ Success (341ms)
⏳ Executing: V003__seed_data.sql...
   ✅ Success (2104ms)

========================================
✅ Migration Complete!
========================================
Executed: 3 migrations
Total time: 3650ms
========================================
```

### Configuration

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "SqlServerAdmin": "Server=localhost,1453;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;",
    "LibraryDb": "Server=localhost,1453;Database=LibraryDb;User Id=library_app_user;Password=LibraryApp@2024!;TrustServerCertificate=True;"
  },
  "Database": {
    "MigrationsPath": "../../../../migrations",
    "CommandTimeout": 30
  }
}
```

**Important:**
- Use **admin connection** (SA) for migrations (needs DDL permissions)
- Use **app connection** (library_app_user) for normal operations
- Store passwords in **User Secrets** (not in appsettings.json!)

**User Secrets:**
```bash
dotnet user-secrets set "ConnectionStrings:SqlServerAdmin" "Server=localhost,1453;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
```

## 🏛️ Architecture Details

### MigrationRecord Class

Represents a single migration file:

```csharp
public class MigrationRecord
{
    public string Version { get; set; }           // "001"
    public string FileName { get; set; }          // "V001__initial_schema.sql"
    public string FilePath { get; set; }          // Full path
    public string SqlContent { get; set; }        // File contents
    public string Checksum { get; set; }          // SHA256 hash
    public bool IsApplied { get; set; }           // Already in database?
    public DateTime? AppliedAt { get; set; }      // When applied
    public int? ExecutionTimeMs { get; set; }     // How long
    public string? DatabaseChecksum { get; set; } // Checksum from DB
    public bool ChecksumMatches { get; }          // Validation
}
```

### MigrationRunner Class

Core migration engine:

```csharp
public class MigrationRunner
{
    public async Task<int> RunMigrationsAsync();

    private List<MigrationRecord> ScanMigrationFiles();
    private async Task<Dictionary<...>> GetAppliedMigrationsAsync();
    private void ValidateChecksums(List<MigrationRecord> migrations);
    private async Task ExecuteMigrationAsync(MigrationRecord migration);
    private List<string> SplitSqlBatches(string sql);
    private string CalculateSHA256(string content);
}
```

### Transaction Per Migration

Each migration runs in its own transaction:

```csharp
await using var transaction = await connection.BeginTransactionAsync();

try
{
    // Execute all SQL batches
    foreach (var batch in batches)
    {
        await cmd.ExecuteNonQueryAsync();
    }

    // Record in history
    await historyCmd.ExecuteNonQueryAsync();

    // Commit everything
    await transaction.CommitAsync();
}
catch
{
    // Rollback everything if anything fails
    await transaction.RollbackAsync();
    throw;
}
```

**Benefits:**
- **Atomic**: All-or-nothing for each migration
- **Rollback**: Automatic on any error
- **Consistency**: Database never in half-migrated state

### Splitting SQL Batches

SQL Server requires GO statements to separate batches:

```sql
CREATE TABLE Books (...);
GO  -- ← Batch separator

CREATE INDEX IX_Books_ISBN ON Books(ISBN);
GO
```

**MigrationRunner splits by GO:**
```csharp
var batches = Regex.Split(sql, @"^\s*GO\s*$",
    RegexOptions.Multiline | RegexOptions.IgnoreCase);
```

Then executes each batch separately (but in same transaction).

## ⚠️ Common Pitfalls

### 1. Modifying Applied Migrations

**Problem:**
```sql
-- Developer modifies V001__create_users.sql after it's been applied
-- Checksum no longer matches!
```

**Error:**
```
❌ CHECKSUM MISMATCH DETECTED!
The following migrations have been modified after being applied:
  ❌ V001__create_users.sql
```

**Solution:** Create V002__fix_users.sql instead

### 2. Wrong Connection String

**Problem:**
Using app user (library_app_user) instead of admin (SA) for migrations.

**Error:**
```
Permission denied creating table 'Books'
```

**Solution:**
- Migrations use `SqlServerAdmin` connection
- App operations use `LibraryDb` connection

### 3. Missing GO Statements

**Problem:**
```sql
CREATE TABLE Foo (...);
CREATE TABLE Bar (...);  -- ← Syntax error without GO!
```

**Solution:**
```sql
CREATE TABLE Foo (...);
GO

CREATE TABLE Bar (...);
GO
```

### 4. Non-Idempotent Migrations

**Problem:**
```sql
ALTER TABLE Books ADD NewColumn INT;  -- Fails if re-run!
```

**Solution:**
```sql
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Books')
    AND name = 'NewColumn'
)
BEGIN
    ALTER TABLE Books ADD NewColumn INT;
END
```

### 5. Forgetting to Copy Migration Files

**Problem:** Migration files not in output directory.

**Error:**
```
📂 Found 0 migration files
```

**Solution:** Ensure .csproj includes:
```xml
<None Include="../../../../migrations/V*.sql">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

## ✅ Best Practices

### 1. Test Migrations Locally First

```bash
# Test on local Docker database
dotnet run

# Verify results
docker exec dbdemo-sqlserver /opt/mssql-tools18/bin/sqlcmd ...
```

### 2. Commit Migration and Code Together

```bash
# Add migration and related code in same commit
git add migrations/V004__add_feature.sql
git add src/DbDemo.ConsoleApp/Models/NewModel.cs
git commit -m "feat: add new feature with migration"
```

**Why?** Code and schema changes should be deployed together.

### 3. Use Descriptive Migration Names

**❌ Bad:**
```
V004__changes.sql
V005__updates.sql
```

**✅ Good:**
```
V004__add_audit_columns_to_books.sql
V005__create_notifications_table.sql
```

### 4. Keep Migrations Focused

**One logical change per migration:**
- ✅ V004: Add audit columns
- ✅ V005: Create notifications table
- ❌ V004: Add audit columns + create notifications + modify loans

**Why?** Easier to:
- Review
- Debug
- Rollback (via new migration)

### 5. Document Complex Migrations

```sql
-- =============================================
-- Migration V004: Add Soft Delete Support
-- =============================================
-- Adds IsDeleted and DeletedAt columns to all entities
-- for soft delete pattern (preserve data instead of DELETE)
--
-- Impact: All DELETE operations in code must be updated
-- to set IsDeleted=1 instead of actual deletion
-- =============================================
```

## 🔗 Learn More

### Migration Tools (Inspiration)
- [Flyway](https://flywaydb.org/) - Database migration tool (Java)
- [DbUp](https://dbup.readthedocs.io/) - .NET migration library
- [Entity Framework Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/) - ORM-based migrations

### Design Philosophy
- [Evolutionary Database Design](https://martinfowler.com/articles/evodb.html) - Martin Fowler
- [Refactoring Databases](https://databaserefactoring.com/) - Scott Ambler & Pramod Sadalage
- [The Twelve-Factor App: Config](https://12factor.net/config) - Environment config best practices

### ADO.NET Specifics
- [SqlTransaction Class](https://learn.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqltransaction) - Transaction API
- [SHA256 Class](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.sha256) - Checksum calculation
- [Regex.Split](https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex.split) - Batch splitting

## ❓ Discussion Questions

1. **Why don't we support down/rollback migrations?**
   - Think about: Data changes, production reality, testing complexity

2. **What happens if a migration fails halfway through?**
   - Consider: Transactions, rollback, database state

3. **How do you handle different migration states across environments?**
   - Example: Dev has V001-V010, Prod has V001-V005

4. **Why use checksums instead of just filename/version?**
   - Think about: Tampering, file corruption, accidental edits

5. **Should migrations be in source control?**
   - Consider: Team collaboration, deployment, history

6. **How would you handle data migrations (not just schema)?**
   - Example: Migrating data from OldTable to NewTable

## 🎯 Summary

**What We've Built:**
- ✅ Automated migration runner with SHA256 validation
- ✅ Forward-only migration philosophy
- ✅ Transaction-based execution (atomic)
- ✅ Migration history tracking
- ✅ Idempotent patterns
- ✅ Comprehensive error handling

**Key Principles:**
1. **Forward-only**: Never modify applied migrations
2. **No up/down**: Fix forward, don't rollback
3. **Idempotent**: Safe to re-run
4. **Checksums**: Detect tampering
5. **Transactions**: Atomic migrations
6. **Automation**: Runs on startup

**Benefits:**
- Reproducible deployments
- No forgotten migrations
- Tamper detection
- Audit trail
- Consistency across environments

## 🚀 Next Steps

Now that migrations run automatically, we can:

1. **Commit 7**: Create our first repository (BookRepository) using ADO.NET
2. **Commit 8**: Write integration tests that use the migration runner
3. Focus on domain logic without worrying about manual schema setup!

**Your database schema is now fully automated! 🎉**
