# 21 - Scalar Functions

## 📖 What You'll Learn

- What scalar functions are and how they differ from stored procedures
- How to create user-defined scalar functions in SQL Server
- Calling scalar functions from C# using ExecuteScalarAsync
- When to use scalar functions vs. stored procedures vs. computed columns
- Performance characteristics and limitations of scalar functions

## 🎯 Why This Matters

Scalar functions are essential for:
- **Encapsulating Calculations**: Reusable business logic (e.g., late fee calculation)
- **Consistent Logic**: Single source of truth used across queries, stored procedures, and applications
- **Readability**: `dbo.fn_CalculateLateFee(@LoanId)` is clearer than complex inline calculations
- **Composability**: Can be used in SELECT, WHERE, and JOIN clauses

However, scalar functions have significant performance implications that you must understand before using them.

## 🔍 Key Concepts

### What is a Scalar Function?

A **scalar function** is a user-defined function that returns a single value (scalar):

```sql
CREATE FUNCTION dbo.fn_FunctionName(@Param1 INT, @Param2 VARCHAR(50))
RETURNS INT
AS
BEGIN
    DECLARE @Result INT;
    -- Calculation logic
    SET @Result = @Param1 * 2;
    RETURN @Result;
END
```

**Key Characteristics:**
- **Single Return Value**: Returns one value of a specific data type (INT, DECIMAL, VARCHAR, etc.)
- **Deterministic or Non-Deterministic**: Can be marked as deterministic (same inputs = same output)
- **Used in Queries**: Can be called in SELECT, WHERE, HAVING, ORDER BY clauses
- **No Side Effects**: Cannot modify database state (no INSERT/UPDATE/DELETE)

### Scalar vs. Other Function Types

| Type | Returns | Use Case | Example |
|------|---------|----------|---------|
| **Scalar Function** | Single value | Calculations, transformations | `fn_CalculateLateFee(123)` → `6.50` |
| **Inline Table-Valued Function** | Table (single SELECT) | Parameterized views | `fn_GetActiveLoans(userId)` → Result set |
| **Multi-Statement Table-Valued Function** | Table (multiple statements) | Complex result sets | `fn_GetMemberStatistics(memberId)` → Result set |
| **Stored Procedure** | Multiple result sets, output params | Operations, reports | `sp_GetOverdueLoans` → Multiple columns |

### Our Implementation: fn_CalculateLateFee

In migration `V008__add_late_fee_calculation_function.sql`, we create a scalar function for calculating late fees:

```sql
CREATE FUNCTION dbo.fn_CalculateLateFee(@LoanId INT)
RETURNS DECIMAL(10,2)
AS
BEGIN
    DECLARE @LateFee DECIMAL(10,2) = 0.00;
    DECLARE @DueDate DATETIME2;
    DECLARE @ReturnedAt DATETIME2;
    DECLARE @Status INT;
    DECLARE @DaysOverdue INT;
    DECLARE @LateFeePerDay DECIMAL(10,2) = 0.50;

    -- Get loan details
    SELECT
        @DueDate = DueDate,
        @ReturnedAt = ReturnedAt,
        @Status = Status
    FROM dbo.Loans
    WHERE Id = @LoanId;

    -- If loan not found, return 0
    IF @DueDate IS NULL
        RETURN 0.00;

    -- Calculate days overdue
    -- If not yet returned, use current date; otherwise use return date
    SET @DaysOverdue = DATEDIFF(DAY, @DueDate, COALESCE(@ReturnedAt, SYSUTCDATETIME()));

    -- Only charge fee if overdue (positive days)
    IF @DaysOverdue > 0
        SET @LateFee = @DaysOverdue * @LateFeePerDay;

    RETURN @LateFee;
END
```

**Design Decisions:**
- **Returns DECIMAL(10,2)**: Precise monetary values (no floating-point errors)
- **Handles Missing Data**: Returns 0.00 for non-existent loans
- **Uses COALESCE**: Calculates based on return date if returned, current time otherwise
- **Business Rule**: £0.50 per day late (hardcoded constant)

### Calling from C#: ExecuteScalarAsync

The `LoanRepository.CalculateLateFeeAsync()` method demonstrates calling a scalar function:

```csharp
public async Task<decimal> CalculateLateFeeAsync(
    int loanId,
    SqlTransaction transaction,
    CancellationToken cancellationToken = default)
{
    // Scalar functions are called like any SQL expression
    const string sql = "SELECT dbo.fn_CalculateLateFee(@LoanId)";

    var connection = transaction.Connection;

    await using var command = new SqlCommand(sql, connection, transaction);
    command.Parameters.Add("@LoanId", SqlDbType.Int).Value = loanId;

    // ExecuteScalarAsync returns object (first column of first row)
    var result = await command.ExecuteScalarAsync(cancellationToken);

    // Convert to decimal, handling NULL
    return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0.00m;
}
```

**Key Points:**
- **SELECT Syntax**: Call function in a SELECT statement, not as stored procedure
- **ExecuteScalarAsync**: Returns the first column of the first row as `object`
- **Type Conversion**: Cast/convert to the expected return type (DECIMAL → decimal)
- **NULL Handling**: Check for DBNull.Value before converting

### Using Scalar Functions in Queries

Scalar functions can be embedded in SQL queries:

```sql
-- In SELECT clause: Calculate fee for each loan
SELECT
    Id,
    MemberId,
    DueDate,
    dbo.fn_CalculateLateFee(Id) AS LateFee
FROM dbo.Loans
WHERE ReturnedAt IS NULL;

-- In WHERE clause: Filter by calculated fee
SELECT * FROM dbo.Loans
WHERE dbo.fn_CalculateLateFee(Id) > 5.00;

-- In ORDER BY clause: Sort by fee amount
SELECT * FROM dbo.Loans
ORDER BY dbo.fn_CalculateLateFee(Id) DESC;

-- In stored procedures: Consistent calculation
CREATE PROCEDURE dbo.sp_GetOverdueLoans
AS
BEGIN
    SELECT
        l.Id,
        dbo.fn_CalculateLateFee(l.Id) AS CalculatedLateFee
    FROM dbo.Loans l;
END
```

**Benefit**: The calculation logic is centralized—change the function once, and all queries/procedures use the updated logic.

## ⚠️ Common Pitfalls

### 1. Performance: Row-by-Row Execution

**Problem**: Scalar functions execute once **per row**, which can be extremely slow for large result sets.

**Example**:
```sql
-- This calls fn_CalculateLateFee 10,000 times!
SELECT Id, dbo.fn_CalculateLateFee(Id) AS Fee
FROM dbo.Loans;  -- 10,000 rows
```

**Why It's Slow**:
- Function executes for each row individually
- Cannot be parallelized
- Query optimizer cannot optimize across function boundary
- Each function call may perform its own database reads

**Mitigation**:
- Use inline calculations for simple logic
- Use inline table-valued functions (iTVF) instead—they can be optimized
- For complex logic, consider computed columns or application-level calculation

### 2. Cannot Call Stored Procedures from Scalar Functions

**Problem**: Scalar functions have strict limitations—they cannot:
- Modify database state (no INSERT/UPDATE/DELETE)
- Call stored procedures
- Use non-deterministic functions (NEWID(), GETDATE() in SQL Server 2016+)
- Create temporary tables

**Fix**: Use multi-statement table-valued functions or stored procedures if you need these capabilities.

### 3. Misleading NULL Results

**Problem**: If the function encounters an error or returns NULL, the result may be silently NULL:

```sql
SELECT dbo.fn_CalculateLateFee(999999);  -- Non-existent loan
-- Returns 0.00 (by our design)
```

**Best Practice**: Design functions to return meaningful defaults (like 0.00) rather than NULL, or document NULL semantics clearly.

### 4. Forgetting Schema Prefix

**Problem**:
```csharp
const string sql = "SELECT fn_CalculateLateFee(@LoanId)";  // Missing dbo.
```

**Result**: `SqlException: 'fn_CalculateLateFee' is not a recognized built-in function name.`

**Fix**: Always use schema-qualified names:
```csharp
const string sql = "SELECT dbo.fn_CalculateLateFee(@LoanId)";  // ✓ Correct
```

### 5. Over-Using Scalar Functions

**Problem**: Creating scalar functions for every small calculation adds overhead and complexity.

**Guideline**:
- **Use scalar functions for**: Shared business logic, complex calculations, domain rules
- **Avoid for**: Simple arithmetic (`Price * Quantity`), single-use calculations

## ✅ Best Practices

### 1. Use Inline Table-Valued Functions for Better Performance

**Instead of Scalar Function**:
```sql
-- Scalar function (slow for large result sets)
CREATE FUNCTION dbo.fn_CalculateLateFee(@LoanId INT)
RETURNS DECIMAL(10,2)
AS
BEGIN
    DECLARE @Fee DECIMAL(10,2);
    -- Complex logic
    RETURN @Fee;
END
```

**Use Inline Table-Valued Function** (when possible):
```sql
-- Inline TVF (can be optimized by query planner)
CREATE FUNCTION dbo.fn_CalculateLateFees(@MinFee DECIMAL(10,2))
RETURNS TABLE
AS
RETURN
(
    SELECT
        Id AS LoanId,
        CASE
            WHEN DATEDIFF(DAY, DueDate, SYSUTCDATETIME()) > 0
            THEN DATEDIFF(DAY, DueDate, SYSUTCDATETIME()) * 0.50
            ELSE 0.00
        END AS LateFee
    FROM dbo.Loans
    WHERE ReturnedAt IS NULL
);

-- Usage
SELECT * FROM dbo.fn_CalculateLateFees(5.00);
```

**Why?** Inline TVFs are treated like views—the query optimizer can inline them into the main query, enabling set-based optimization.

### 2. Mark Functions as SCHEMABINDING

```sql
CREATE FUNCTION dbo.fn_CalculateLateFee(@LoanId INT)
RETURNS DECIMAL(10,2)
WITH SCHEMABINDING  -- Prevent table schema changes that would break function
AS
BEGIN
    -- Function body
END
```

**Benefits**:
- Prevents dropping/altering tables that the function depends on
- Enables indexed views over the function (in some cases)
- Documents dependencies explicitly

### 3. Document Business Rules in Comments

```sql
-- =============================================
-- Function: fn_CalculateLateFee
-- Description: Calculates late fees for overdue loans
-- Business Rule: £0.50 per day overdue
--   - Fee calculated from DueDate to ReturnedAt (or current date if not returned)
--   - No fee for loans returned on time or early
--   - Returns 0.00 for non-existent loans
-- Parameters:
--   @LoanId: The loan ID to calculate fee for
-- Returns: Late fee amount (DECIMAL 10,2)
-- =============================================
```

### 4. Test Edge Cases

Ensure your function handles:
- Non-existent IDs (return 0 or NULL?)
- NULL input parameters
- Divide-by-zero scenarios
- Date boundary conditions (loans returned on due date)

### 5. Consider Computed Columns for Frequently Accessed Calculations

If the calculation is needed in most queries:

```sql
-- Add a persisted computed column
ALTER TABLE dbo.Loans
ADD LateFee AS dbo.fn_CalculateLateFee(Id) PERSISTED;

-- Create index on computed column for performance
CREATE INDEX IX_Loans_LateFee ON dbo.Loans(LateFee);
```

**Trade-offs**:
- **Pros**: Calculated once, stored on disk, indexable, fast reads
- **Cons**: Takes storage space, updated on every write, stale if logic changes

### 6. Handle Time Zones Consistently

Our function uses `SYSUTCDATETIME()` (UTC) for consistency:
```sql
SET @DaysOverdue = DATEDIFF(DAY, @DueDate, COALESCE(@ReturnedAt, SYSUTCDATETIME()));
```

**Avoid**: `GETDATE()` or `SYSDATETIME()` (server local time) unless you have a specific reason.

## 🧪 Testing This Feature

The `LateFeeCalculationTests.cs` integration test suite verifies:

1. **Non-Existent Loan**: Returns 0.00 for invalid loan IDs
2. **Not Overdue**: Returns 0.00 for loans not yet past due date
3. **Overdue Not Returned**: Calculates fee based on current date
4. **Overdue Returned**: Calculates fee based on return date (not current date)
5. **Various Days Overdue**: Theory test with multiple scenarios (1, 7, 14, 30 days)
6. **Consistency Check**: Scalar function matches stored procedure calculation

**Example Test**:
```csharp
[Theory]
[InlineData(1, 0.50)]   // 1 day overdue = £0.50
[InlineData(7, 3.50)]   // 7 days = £3.50
[InlineData(14, 7.00)]  // 14 days = £7.00
[InlineData(30, 15.00)] // 30 days = £15.00
public async Task CalculateLateFeeAsync_VariousDaysOverdue_ShouldCalculateCorrectly(
    int daysOverdue,
    decimal expectedFee)
{
    // Arrange
    var loanId = await CreateOverdueLoan(daysOverdue);

    // Act
    var fee = await _fixture.WithTransactionAsync(tx =>
        _loanRepository.CalculateLateFeeAsync(loanId, tx));

    // Assert
    Assert.Equal(expectedFee, fee);
}
```

**Consistency Test** (verifies scalar function matches stored procedure):
```csharp
[Fact]
public async Task CalculateLateFeeAsync_MatchesStoredProcedureCalculation()
{
    var loanId = await CreateOverdueLoan(12);  // 12 days overdue

    var (scalarFee, storedProcFee) = await _fixture.WithTransactionAsync(async tx =>
    {
        // Calculate using scalar function
        var feeFromScalar = await _loanRepository.CalculateLateFeeAsync(loanId, tx);

        // Calculate using stored procedure
        var (loans, _) = await _loanRepository.GetOverdueLoansReportAsync(null, 0, tx);
        var feeFromProc = loans.FirstOrDefault(l => l.LoanId == loanId)?.CalculatedLateFee ?? 0m;

        return (feeFromScalar, feeFromProc);
    });

    // Both methods should calculate the same fee
    Assert.Equal(storedProcFee, scalarFee);
}
```

## 🔗 Learn More

### Official Documentation
- [User-Defined Functions](https://learn.microsoft.com/en-us/sql/relational-databases/user-defined-functions/user-defined-functions)
- [CREATE FUNCTION (Transact-SQL)](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-function-transact-sql)
- [Scalar Functions vs. Table-Valued Functions](https://learn.microsoft.com/en-us/sql/relational-databases/user-defined-functions/user-defined-functions#types-of-functions)
- [SqlCommand.ExecuteScalar](https://learn.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand.executescalar)

### Performance and Optimization
- [Scalar UDF Inlining (SQL Server 2019+)](https://learn.microsoft.com/en-us/sql/relational-databases/user-defined-functions/scalar-udf-inlining) - Significant performance improvement
- [Computed Columns](https://learn.microsoft.com/en-us/sql/relational-databases/tables/specify-computed-columns-in-a-table)
- [Why Scalar Functions Are Slow](https://www.brentozar.com/archive/2018/01/scalar-user-defined-functions-are-bad-mmkay/) - Brent Ozar

### Related Concepts
- [Deterministic and Nondeterministic Functions](https://learn.microsoft.com/en-us/sql/relational-databases/user-defined-functions/deterministic-and-nondeterministic-functions)
- [SCHEMABINDING](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-function-transact-sql#schemabinding)
- [Inline Table-Valued Functions](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-function-transact-sql#inline-table-valued-functions)

## ❓ Discussion Questions

1. **When should you use a scalar function vs. a computed column?**
   - Scalar function: Logic may change, used across multiple tables, complex calculation
   - Computed column: Frequently accessed, benefits from indexing, storage is acceptable

2. **What are the performance implications of calling a scalar function in a WHERE clause?**
   - Function executes for every row (even rows that don't match other filters)
   - Index on the column cannot be used (function wraps the column)
   - Query cannot be parallelized effectively
   - Consider computed column with index instead

3. **How does SQL Server 2019's Scalar UDF Inlining improve performance?**
   - Automatically inlines simple scalar functions into the main query
   - Enables set-based optimization instead of row-by-row execution
   - Works only for functions meeting specific criteria (no recursion, no side effects, etc.)
   - Can provide 10-100x performance improvement

4. **When would you choose a scalar function over a stored procedure?**
   - Scalar function: Need to use in SELECT/WHERE clauses, single return value, no side effects
   - Stored procedure: Complex operations, multiple result sets, output parameters, DML operations

5. **How can you refactor a slow scalar function?**
   - Rewrite as inline table-valued function
   - Move calculation to application code
   - Use computed column (persisted)
   - Enable Scalar UDF Inlining (SQL Server 2019+)
   - Replace with inline SQL expression

## 🎓 Try It Yourself

1. **Create a new scalar function** `fn_GetMemberFullName` that concatenates first and last name with proper handling of NULL values
2. **Measure performance**: Compare a query using the scalar function vs. inline calculation on 10,000 rows
3. **Rewrite as inline TVF**: Convert `fn_CalculateLateFee` to an inline table-valued function and compare performance
4. **Create a computed column**: Add a persisted computed column using the scalar function and observe the impact
5. **Test edge cases**: What happens if you pass NULL, negative, or very large numbers to the function?
6. **Implement a complex function**: Create `fn_CalculateMemberLoyaltyScore` based on loan history, return count, and late fees

---

**Previous**: [20 - Stored Procedures](20-stored-procedures.md)
