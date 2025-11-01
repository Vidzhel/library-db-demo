# 20 - Stored Procedures

## 📖 What You'll Learn

- What stored procedures are and when to use them
- How to create stored procedures with input and output parameters
- Calling stored procedures from C# using ADO.NET
- Working with output parameters and return values
- Benefits and trade-offs of stored procedures vs. inline SQL

## 🎯 Why This Matters

Stored procedures are a fundamental database feature that:
- **Encapsulate Complex Logic**: Move business logic close to the data, reducing network round-trips
- **Improve Performance**: Pre-compiled execution plans, reduced parsing overhead
- **Enhance Security**: Grant execute permissions without exposing table structure
- **Centralize Logic**: Single source of truth for complex queries used by multiple applications
- **Reduce Network Traffic**: Return only necessary data, process joins and aggregations on the server

However, they come with trade-offs: harder to version control, debug, and test compared to application code.

## 🔍 Key Concepts

### What is a Stored Procedure?

A **stored procedure** is a precompiled collection of SQL statements stored in the database:

```sql
CREATE PROCEDURE procedure_name
    @Parameter1 INT,
    @Parameter2 VARCHAR(50),
    @OutputParam INT OUTPUT
AS
BEGIN
    -- SQL logic here
    SELECT @OutputParam = COUNT(*) FROM SomeTable;
END
```

**Key Features:**
- **Input Parameters**: Pass data into the procedure
- **Output Parameters**: Return single values back to caller
- **Return Values**: Return an integer status code (0 = success by convention)
- **Result Sets**: Return query results as tables

### Our Implementation: sp_GetOverdueLoans

In migration `V007__add_overdue_loans_report_sp.sql`, we create a stored procedure that generates an overdue loans report:

```sql
CREATE PROCEDURE dbo.sp_GetOverdueLoans
    @AsOfDate DATETIME2 = NULL,        -- Optional: check overdue status as of this date
    @MinDaysOverdue INT = 0,           -- Filter: only include loans overdue this many days
    @TotalCount INT OUTPUT             -- Output: total number of overdue loans
AS
BEGIN
    SET NOCOUNT ON;

    -- Default to current time if not specified
    IF @AsOfDate IS NULL
        SET @AsOfDate = SYSUTCDATETIME();

    -- Main query: Join loans with members and books
    SELECT
        l.Id AS LoanId,
        m.Id AS MemberId,
        m.FirstName + ' ' + m.LastName AS MemberName,
        m.Email AS MemberEmail,
        m.PhoneNumber,
        b.Id AS BookId,
        b.ISBN,
        b.Title AS BookTitle,
        b.Publisher,
        l.BorrowedAt,
        l.DueDate,
        DATEDIFF(DAY, l.DueDate, @AsOfDate) AS DaysOverdue,
        (DATEDIFF(DAY, l.DueDate, @AsOfDate) * 0.50) AS CalculatedLateFee,
        l.Status,
        l.Notes
    FROM dbo.Loans l
    INNER JOIN dbo.Members m ON l.MemberId = m.Id
    INNER JOIN dbo.Books b ON l.BookId = b.Id
    WHERE (l.Status = 2 OR (l.Status = 0 AND l.DueDate < @AsOfDate))
        AND DATEDIFF(DAY, l.DueDate, @AsOfDate) >= @MinDaysOverdue
    ORDER BY DATEDIFF(DAY, l.DueDate, @AsOfDate) DESC;

    -- Set output parameter with total count
    SELECT @TotalCount = COUNT(*)
    FROM dbo.Loans l
    WHERE (l.Status = 2 OR (l.Status = 0 AND l.DueDate < @AsOfDate))
        AND DATEDIFF(DAY, l.DueDate, @AsOfDate) >= @MinDaysOverdue;
END
```

**Why This Design?**
- **Complex Join**: Combines data from Loans, Members, and Books tables
- **Calculated Fields**: DaysOverdue and CalculatedLateFee computed on the server
- **Flexible Filtering**: Optional parameters with sensible defaults
- **Output Parameter**: Returns total count without requiring a second query
- **Single Round-Trip**: Application gets all data in one call

### Calling from C#: The Complete Flow

The `LoanRepository.GetOverdueLoansReportAsync()` method demonstrates proper ADO.NET stored procedure usage:

```csharp
public async Task<(List<OverdueLoanReport> Loans, int TotalCount)> GetOverdueLoansReportAsync(
    DateTime? asOfDate,
    int minDaysOverdue,
    SqlTransaction transaction,
    CancellationToken cancellationToken = default)
{
    var connection = transaction.Connection;

    // 1. Create command with procedure name
    await using var command = new SqlCommand("dbo.sp_GetOverdueLoans", connection, transaction);

    // 2. Set CommandType to StoredProcedure (critical!)
    command.CommandType = System.Data.CommandType.StoredProcedure;

    // 3. Add input parameters
    command.Parameters.Add("@AsOfDate", SqlDbType.DateTime2).Value = (object?)asOfDate ?? DBNull.Value;
    command.Parameters.Add("@MinDaysOverdue", SqlDbType.Int).Value = minDaysOverdue;

    // 4. Add output parameter with Direction.Output
    var totalCountParam = new SqlParameter("@TotalCount", SqlDbType.Int)
    {
        Direction = System.Data.ParameterDirection.Output
    };
    command.Parameters.Add(totalCountParam);

    // 5. Execute and read result set
    var loans = new List<OverdueLoanReport>();
    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
    while (await reader.ReadAsync(cancellationToken))
    {
        loans.Add(MapReaderToOverdueLoanReport(reader));
    }

    // 6. IMPORTANT: Close reader before accessing output parameters
    await reader.CloseAsync();

    // 7. Retrieve output parameter value
    var totalCount = (int)totalCountParam.Value;

    return (loans, totalCount);
}
```

**Critical Details:**
- **CommandType.StoredProcedure**: Without this, SQL Server treats it as inline SQL
- **Parameter Names**: Must match procedure definition (including `@` prefix)
- **DBNull.Value**: Use for nullable parameters, not `null`
- **Output Parameters**: Must close reader before reading output parameter values
- **Transaction Participation**: Procedure executes within the provided transaction

## ⚠️ Common Pitfalls

### 1. Forgetting CommandType.StoredProcedure

**Problem**:
```csharp
var command = new SqlCommand("dbo.sp_GetOverdueLoans", connection);
// Missing: command.CommandType = CommandType.StoredProcedure;
```

**Result**: SQL Server tries to execute "dbo.sp_GetOverdueLoans" as inline SQL, causing:
```
SqlException: Could not find stored procedure 'dbo.sp_GetOverdueLoans'.
```

**Fix**: Always set `CommandType = CommandType.StoredProcedure` when calling stored procedures.

### 2. Reading Output Parameters Before Closing Reader

**Problem**:
```csharp
await using var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync()) { /* ... */ }

// ❌ Reader still open!
var totalCount = (int)totalCountParam.Value;  // Returns 0 or throws exception
```

**Fix**: Close reader first:
```csharp
await reader.CloseAsync();  // ✓ Now output parameters are available
var totalCount = (int)totalCountParam.Value;
```

### 3. Using null Instead of DBNull.Value

**Problem**:
```csharp
command.Parameters.Add("@AsOfDate", SqlDbType.DateTime2).Value = asOfDate;  // C# null
```

**Fix**: Use DBNull.Value for SQL NULL:
```csharp
command.Parameters.Add("@AsOfDate", SqlDbType.DateTime2).Value = (object?)asOfDate ?? DBNull.Value;
```

### 4. Missing SET NOCOUNT ON

**Problem**: Without `SET NOCOUNT ON`, the procedure returns row count messages that can confuse ADO.NET.

**Best Practice**:
```sql
CREATE PROCEDURE dbo.MyProcedure
AS
BEGIN
    SET NOCOUNT ON;  -- ✓ Always include this
    -- Procedure logic
END
```

### 5. Over-Using Stored Procedures

**Problem**: Moving all logic to stored procedures makes code harder to:
- Version control (database schema vs. application code)
- Unit test (requires database connection)
- Debug (less tooling support than C#)
- Refactor (no type safety, no IDE support)

**Guideline**: Use stored procedures for:
- Complex multi-table joins
- Performance-critical queries
- Operations that benefit from server-side processing
- Shared logic used by multiple applications

Avoid for:
- Simple CRUD operations (inline SQL is clearer)
- Business logic that changes frequently
- Operations that need extensive unit testing

## ✅ Best Practices

### 1. Use Meaningful Parameter Names

```sql
-- ✓ Good: Clear intent
CREATE PROCEDURE sp_GetOverdueLoans
    @AsOfDate DATETIME2 = NULL,
    @MinDaysOverdue INT = 0

-- ✗ Bad: Unclear meaning
CREATE PROCEDURE sp_GetLoans
    @Date DATETIME2,
    @Days INT
```

### 2. Provide Default Values for Optional Parameters

```sql
CREATE PROCEDURE dbo.sp_GetOverdueLoans
    @AsOfDate DATETIME2 = NULL,        -- Defaults to NULL (current time)
    @MinDaysOverdue INT = 0            -- Defaults to 0 (all overdue)
```

This allows flexible calling:
```csharp
// All defaults
await GetOverdueLoansReportAsync(null, 0, tx);

// Custom date
await GetOverdueLoansReportAsync(DateTime.UtcNow.AddDays(-7), 0, tx);

// Only loans overdue 7+ days
await GetOverdueLoansReportAsync(null, 7, tx);
```

### 3. Return Status Codes for Error Handling

```sql
CREATE PROCEDURE dbo.ProcessPayment
    @Amount DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Amount <= 0
        RETURN 1;  -- Invalid amount

    IF @Amount > 10000
        RETURN 2;  -- Amount exceeds limit

    -- Process payment

    RETURN 0;  -- Success
END
```

```csharp
var returnValue = (int)command.ExecuteScalar();
if (returnValue != 0)
{
    throw new InvalidOperationException($"Payment failed with code {returnValue}");
}
```

### 4. Document Your Procedures

```sql
-- =============================================
-- Procedure: sp_GetOverdueLoans
-- Description: Generates a report of overdue loans with member and book details
-- Parameters:
--   @AsOfDate: Date to check for overdue status (defaults to current UTC time)
--   @MinDaysOverdue: Minimum days overdue to include (defaults to 0)
--   @TotalCount: OUTPUT - Total count of overdue loans matching criteria
-- Returns: Result set with loan, member, and book information
-- Author: DbDemo Project
-- Date: 2025-10-29
-- =============================================
CREATE PROCEDURE dbo.sp_GetOverdueLoans
```

### 5. Grant Minimal Permissions

```sql
-- Only grant EXECUTE, not SELECT/INSERT/UPDATE on underlying tables
GRANT EXECUTE ON dbo.sp_GetOverdueLoans TO library_app_user;
```

This encapsulates data access and prevents direct table manipulation.

### 6. Use Table-Valued Parameters for Bulk Operations

For procedures that need to process multiple rows:

```sql
-- Create a table type
CREATE TYPE dbo.LoanIdList AS TABLE (LoanId INT);

-- Use it in a procedure
CREATE PROCEDURE dbo.sp_CalculateMultipleLateFees
    @LoanIds dbo.LoanIdList READONLY
AS
BEGIN
    SELECT
        l.Id,
        dbo.fn_CalculateLateFee(l.Id) AS LateFee
    FROM @LoanIds lids
    INNER JOIN dbo.Loans l ON lids.LoanId = l.Id;
END
```

## 🧪 Testing This Feature

The `OverdueLoansReportTests.cs` integration test suite verifies:

1. **No Overdue Loans**: Returns empty list and TotalCount = 0
2. **Output Parameter Matching**: TotalCount equals result set count
3. **Filtering by MinDaysOverdue**: Only includes loans meeting criteria
4. **Calculated Fields**: DaysOverdue and CalculatedLateFee are correct
5. **Ordering**: Results ordered by most overdue first
6. **Transaction Participation**: Runs within the provided transaction

**Example Test**:
```csharp
[Fact]
public async Task GetOverdueLoansReportAsync_WithMultipleLoans_ShouldReturnOrderedByMostOverdue()
{
    // Arrange - Create loans with different overdue amounts
    var loan1Id = await CreateOverdueLoan(5);   // 5 days overdue
    var loan2Id = await CreateOverdueLoan(15);  // 15 days overdue
    var loan3Id = await CreateOverdueLoan(10);  // 10 days overdue

    // Act
    var (loans, totalCount) = await _fixture.WithTransactionAsync(tx =>
        _loanRepository.GetOverdueLoansReportAsync(null, 0, tx));

    // Assert - Should be ordered by most overdue first
    Assert.Equal(3, totalCount);
    Assert.Equal(loan2Id, loans[0].LoanId);  // 15 days - most overdue
    Assert.Equal(loan3Id, loans[1].LoanId);  // 10 days
    Assert.Equal(loan1Id, loans[2].LoanId);  // 5 days - least overdue

    Assert.Equal(15, loans[0].DaysOverdue);
    Assert.Equal(7.50m, loans[0].CalculatedLateFee);  // 15 * 0.50
}
```

## 🔗 Learn More

### Official Documentation
- [Stored Procedures (Database Engine)](https://learn.microsoft.com/en-us/sql/relational-databases/stored-procedures/stored-procedures-database-engine)
- [CREATE PROCEDURE](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-procedure-transact-sql)
- [SqlCommand.CommandType](https://learn.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand.commandtype)
- [SqlParameter Class](https://learn.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlparameter)
- [Table-Valued Parameters](https://learn.microsoft.com/en-us/sql/relational-databases/tables/use-table-valued-parameters-database-engine)

### Related Concepts
- [Query Plans and Performance](https://learn.microsoft.com/en-us/sql/relational-databases/query-processing-architecture-guide)
- [Parameter Sniffing](https://learn.microsoft.com/en-us/sql/relational-databases/query-processing-architecture-guide#parameter-sensitivity) - Important performance consideration
- [WITH RECOMPILE Option](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-procedure-transact-sql#with-recompile) - When to use it

### Design Patterns
- [Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html) - Our implementation approach
- [CQRS](https://martinfowler.com/bliki/CQRS.html) - Command Query Responsibility Segregation

## ❓ Discussion Questions

1. **When should you use stored procedures vs. inline SQL in your repository?**
   - Stored procedures: Complex joins, performance-critical, shared logic
   - Inline SQL: Simple CRUD, frequently changing logic, better type safety

2. **How do stored procedures affect your ability to change database schemas?**
   - Must update procedure definitions when tables change
   - Can provide abstraction layer to hide schema changes from applications
   - Requires careful migration planning

3. **What are the security benefits of stored procedures?**
   - Users need only EXECUTE permission, not direct table access
   - Encapsulates business rules (can't bypass validation)
   - Reduces SQL injection risk (when used properly)

4. **How would you handle errors in stored procedures?**
   - TRY/CATCH blocks for error handling
   - THROW or RAISERROR to propagate errors to caller
   - Return codes for expected error conditions
   - Output parameters for additional error context

5. **What performance considerations apply to stored procedures?**
   - Parameter sniffing (execution plan based on first call's parameters)
   - Plan cache pollution (too many ad-hoc plans)
   - Statistics and index usage
   - When to use WITH RECOMPILE option

## 🎓 Try It Yourself

1. **Create a new stored procedure** `sp_GetMemberLoanSummary` that returns a member's total loans, active loans, and overdue count
2. **Add an output parameter** to `sp_GetOverdueLoans` that returns the total late fees owed
3. **Implement error handling** with TRY/CATCH in a procedure and test how exceptions propagate to C#
4. **Compare performance** of the stored procedure vs. equivalent inline SQL using multiple JOIN operations
5. **Create a procedure with a table-valued parameter** to mark multiple loans as returned in a single call

---

**Previous**: [19 - Audit Triggers](19-audit-triggers.md) | **Next**: [21 - Scalar Functions](21-scalar-functions.md)
