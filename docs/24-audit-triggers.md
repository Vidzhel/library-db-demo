# 19 - Database Audit Triggers

## 📖 What You'll Learn

- How database triggers work and when they execute
- Implementing audit trails using AFTER triggers
- Capturing old and new values from INSERTED and DELETED tables
- Querying and displaying audit history in C#
- Benefits and trade-offs of trigger-based auditing

## 🎯 Why This Matters

Audit trails are critical for:
- **Compliance**: Many regulations require tracking who changed what and when (GDPR, SOX, HIPAA)
- **Security**: Detecting unauthorized modifications to sensitive data
- **Debugging**: Understanding how data evolved over time to diagnose issues
- **Accountability**: Maintaining a tamper-proof record of all changes

Database triggers provide automatic, centralized auditing that cannot be bypassed by application code.
But, given that it bypasses application logic, it's very easy to forget about triggers and introduce unexpected behavior 
if not carefully managed.

## 🔍 Key Concepts

### What is a Database Trigger?

A **trigger** is a special type of stored procedure that automatically executes when certain events occur on a table:

```sql
CREATE TRIGGER trigger_name
ON table_name
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    -- Trigger logic here
END
```

**Trigger Types:**
- `AFTER` triggers fire after the operation completes (most common for auditing)
- `INSTEAD OF` triggers replace the operation entirely
- `BEFORE` triggers (not available in SQL Server, use INSTEAD OF)

### The INSERTED and DELETED Tables

SQL Server provides two special "magic tables" inside triggers:

| Table | Contains | Available In |
|-------|----------|--------------|
| `inserted` | New row values | INSERT, UPDATE |
| `deleted` | Old row values | UPDATE, DELETE |

**Examples:**
- **INSERT**: Only `inserted` has data (new rows)
- **UPDATE**: Both tables have data (`deleted` = old values, `inserted` = new values)
- **DELETE**: Only `deleted` has data (removed rows)

### Our Audit Implementation

In migration `V006__add_books_audit_trigger.sql`, we create:

1. **BooksAudit Table**: Stores audit records
   ```sql
   - AuditId: Primary key
   - BookId: Reference to the book
   - Action: 'INSERT', 'UPDATE', 'DELETE'
   - Old* fields: Values before change (UPDATE/DELETE)
   - New* fields: Values after change (INSERT/UPDATE)
   - ChangedAt: Timestamp (SYSUTCDATETIME())
   - ChangedBy: User who made the change (SUSER_SNAME())
   ```

2. **TR_Books_Audit Trigger**: Fires on INSERT, UPDATE, DELETE
   ```sql
   -- Detect operation type
   IF EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
       SET @Action = 'UPDATE';  -- Both tables populated
   ELSE IF EXISTS (SELECT 1 FROM inserted)
       SET @Action = 'INSERT';  -- Only inserted populated
   ELSE IF EXISTS (SELECT 1 FROM deleted)
       SET @Action = 'DELETE';  -- Only deleted populated

   -- Capture changes with FULL OUTER JOIN
   INSERT INTO BooksAudit (...)
   SELECT
       COALESCE(i.Id, d.Id) AS BookId,
       @Action,
       d.ISBN AS OldISBN,  -- NULL for INSERT
       i.ISBN AS NewISBN,  -- NULL for DELETE
       ...
   FROM inserted i
   FULL OUTER JOIN deleted d ON i.Id = d.Id;
   ```

### C# Repository Access

The `BookAuditRepository` provides read-only access to audit records:

```csharp
// Get history for a specific book
var history = await _auditRepository.GetAuditHistoryAsync(bookId, transaction);

// Get all recent audit activity
var recent = await _auditRepository.GetAllAuditRecordsAsync(
    action: "UPDATE",  // Filter by action type
    limit: 50,
    transaction: transaction
);
```

The `BookAudit` model includes a helper method to describe changes:

```csharp
public string GetChangeDescription()
{
    return Action switch
    {
        "INSERT" => $"Book created: '{NewTitle}' (ISBN: {NewISBN})",
        "DELETE" => $"Book deleted: '{OldTitle}' (ISBN: {OldISBN})",
        "UPDATE" => GetUpdateDescription(),  // Details what changed
        _ => $"Unknown action: {Action}"
    };
}
```

## ⚠️ Common Pitfalls

### 1. Trigger Performance Impact

**Problem**: Triggers execute synchronously within the transaction, increasing operation time.

**Mitigation**:
- Keep trigger logic simple and fast
- Only audit essential fields (not every column)
- Consider asynchronous auditing for high-throughput systems

### 2. Recursive Triggers

**Problem**: If a trigger modifies the same table, it can trigger itself infinitely.

**Mitigation**:
- Our trigger only INSERTs into a separate BooksAudit table (safe)
- If you must update the same table, use `SET NOCOUNT ON` and check recursion level
- SQL Server has a max recursion depth (32 levels) as a safety net

### 3. Forgetting SET NOCOUNT ON

**Problem**: Without `SET NOCOUNT ON`, triggers send row count messages that can confuse application code.

**Best Practice**:
```sql
CREATE TRIGGER ...
AS
BEGIN
    SET NOCOUNT ON;  -- ✓ Always include this
    -- Trigger logic
END
```

### 4. Transaction Rollback Affects Audit Records

**Problem**: If the main transaction rolls back, audit records are also rolled back (lost).

**Solution Options**:
- Accept this behavior (audit only committed changes)
- Use a separate transaction for auditing (complex, can cause issues)
- Write to a queue/log file for durability (more robust but complicated)

Our implementation audits only committed changes (simpler and sufficient for most cases).

### 5. Not Handling NULL Values

**Problem**: Using `=` to compare NULLs returns false (NULL = NULL is false in SQL).

**Mitigation**: Our implementation uses FULL OUTER JOIN and `IS NULL` checks appropriately.

## ✅ Best Practices

### 1. Index the Audit Table

```sql
-- Efficient queries by BookId
CREATE NONCLUSTERED INDEX IX_BooksAudit_BookId
    ON BooksAudit(BookId, ChangedAt DESC)
    INCLUDE (Action, NewTitle, NewAvailableCopies);
```

This allows fast retrieval of a book's complete history.

### 2. Capture Metadata

Always include:
- **Timestamp**: `SYSUTCDATETIME()` (UTC for consistency across time zones)
- **User**: `SUSER_SNAME()` (server login) or `USER_NAME()` (database user)
- **Application**: Could add `APP_NAME()` to distinguish different apps

### 3. Store What Changed, Not Everything

For large tables, storing every column on every change wastes space. Our implementation tracks:
- Key identifiers (ISBN, Title)
- Business-critical values (Available/Total Copies)
- Metadata (ChangedAt, ChangedBy)

Consider using JSON for flexible auditing:
```sql
-- Alternative: Store full old/new row as JSON
OldValues NVARCHAR(MAX),  -- JSON_OBJECT(...)
NewValues NVARCHAR(MAX)   -- JSON_OBJECT(...)
```

### 4. Read-Only Audit Tables

Grant only SELECT permission to application users:
```sql
GRANT SELECT ON dbo.BooksAudit TO library_app_user;
-- No INSERT, UPDATE, DELETE permissions
```

Only triggers (which run with elevated permissions) can modify audit tables.

### 5. Partition Large Audit Tables

For tables with millions of audit records:
- Partition by date (e.g., monthly partitions)
- Archive old data to separate tables
- Consider retention policies (keep 7 years, then archive/delete)

## 🧪 Testing This Feature

The `BookAuditTriggerTests.cs` integration test suite verifies:

1. **INSERT trigger**: Creates audit record with Action='INSERT'
2. **UPDATE trigger**: Creates audit record with old and new values
3. **DELETE trigger**: Creates audit record with Action='UPDATE' (soft delete in our case)
4. **Multiple updates**: Maintains chronological order (most recent first)
5. **Filtering**: Can query by action type
6. **Metadata**: Captures timestamp and user information

**Example Test**:
```csharp
[Fact]
public async Task InsertBook_ShouldCreateAuditRecordWithInsertAction()
{
    // Arrange
    var book = new Book("978-0-135-95705-9", "Test Book", categoryId, 5);

    // Act
    var createdBook = await _bookRepository.CreateAsync(book, transaction);

    // Assert - Trigger should have created audit record
    var auditHistory = await _auditRepository.GetAuditHistoryAsync(
        createdBook.Id, transaction);

    Assert.Single(auditHistory);
    Assert.Equal("INSERT", auditHistory[0].Action);
    Assert.Equal("Test Book", auditHistory[0].NewTitle);
    Assert.Null(auditHistory[0].OldTitle);  // No old value for INSERT
}
```

## 🔗 Learn More

### Official Documentation
- [SQL Server Triggers](https://learn.microsoft.com/en-us/sql/relational-databases/triggers/dml-triggers)
- [INSERTED and DELETED Tables](https://learn.microsoft.com/en-us/sql/relational-databases/triggers/use-the-inserted-and-deleted-tables)
- [SYSUTCDATETIME Function](https://learn.microsoft.com/en-us/sql/t-sql/functions/sysutcdatetime-transact-sql)
- [SUSER_SNAME Function](https://learn.microsoft.com/en-us/sql/t-sql/functions/suser-sname-transact-sql)

### Related Concepts
- [Temporal Tables](https://learn.microsoft.com/en-us/sql/relational-databases/tables/temporal-tables) - Built-in system-versioned auditing (SQL Server 2016+)
- [Change Data Capture (CDC)](https://learn.microsoft.com/en-us/sql/relational-databases/track-changes/about-change-data-capture-sql-server) - Enterprise feature for tracking changes
- [Change Tracking](https://learn.microsoft.com/en-us/sql/relational-databases/track-changes/about-change-tracking-sql-server) - Lightweight alternative to CDC

### Audit Patterns
- [Audit Trail Pattern](https://martinfowler.com/eaaDev/AuditLog.html) - Martin Fowler
- [Event Sourcing](https://martinfowler.com/eaaDev/EventSourcing.html) - Alternative architectural pattern

## ❓ Discussion Questions

1. **When would you choose trigger-based auditing vs. application-level auditing?**
   - Triggers: Cannot be bypassed, centralized, automatic
   - Application: More flexible, better performance, easier to debug

2. **How would you handle auditing for a table with 100 columns?**
   - Store full row as JSON (flexible but less queryable)
   - Audit only changed columns (requires dynamic SQL)
   - Audit only business-critical columns (our approach)

3. **What are the security implications of audit trails?**
   - Must protect against tampering (read-only permissions)
   - PII considerations (audit records contain sensitive data)
   - Retention policies (legal requirements vs. storage costs)

4. **How would you implement "who deleted this record" for hard deletes?**
   - Our soft delete approach preserves the record
   - For hard deletes, the audit table captures OldValues before deletion
   - Could use `deleted` table to capture all fields

5. **When would you use temporal tables instead of triggers?**
   - Temporal tables: Automatic, built-in, zero code (SQL Server 2016+)
   - Triggers: More control, works on older SQL Server, custom logic

## 🎓 Try It Yourself

1. **Modify the trigger** to audit changes to the `Subtitle` field
2. **Add a new audit table** for the `Members` table
3. **Implement a view** that shows all changes in the last 24 hours across all tables
4. **Create a stored procedure** that "undoes" a change using audit history (caution: complex!)
5. **Compare performance** of 1000 inserts with and without the trigger

---

**Next**: [20 - Stored Procedures](20-stored-procedures.md) - Learn how to create and call stored procedures from ADO.NET
