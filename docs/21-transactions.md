# Transaction Management and Concurrency Safety

This document explains how transactions are used in the Library Management System to ensure data integrity, prevent partial failures, and handle race conditions correctly.

## Table of Contents
1. [What Transactions Fix](#what-transactions-fix)
2. [What Transactions DON'T Fix](#what-transactions-dont-fix)
3. [Complete Solutions for Race Conditions](#complete-solutions-for-race-conditions)
4. [Implementation Patterns](#implementation-patterns)
5. [CHECK Constraints as Safety Nets](#check-constraints-as-safety-nets)

---

## What Transactions Fix

### Problem: Partial Failures

Without transactions, multi-step operations can fail partway through, leaving the database in an inconsistent state.

**Example: Borrowing a Book (❌ Without Transactions)**

```csharp
// Step 1: Create loan record
var loan = Loan.Create(memberId, bookId);
await _loanRepository.CreateAsync(loan);  // ✅ Succeeds

// Step 2: Decrement available copies
book.BorrowCopy();
await _bookRepository.UpdateAsync(book);  // ❌ FAILS (network error, constraint violation, etc.)

// Result: Loan record exists, but book availability wasn't updated!
// Database is now inconsistent.
```

**Solution: Wrap in Transaction (✅ With Transactions)**

```csharp
await using var connection = new SqlConnection(_connectionString);
await connection.OpenAsync();
await using var transaction = await connection.BeginTransactionAsync();

try
{
    // Step 1: Create loan record (within transaction)
    var loan = Loan.Create(memberId, bookId);
    await _loanRepository.CreateAsync(loan, transaction);

    // Step 2: Decrement available copies (within transaction)
    book.BorrowCopy();
    await _bookRepository.UpdateAsync(book, transaction);

    // ✅ BOTH steps succeeded - commit the transaction
    await transaction.CommitAsync();
}
catch (Exception)
{
    // ❌ Something failed - rollback ALL changes
    await transaction.RollbackAsync();
    throw;
}

// Result: Either BOTH operations succeed, or BOTH are rolled back.
// Database remains consistent.
```

### Benefits of Transactions

1. **Atomicity**: All operations within a transaction succeed together or fail together (all-or-nothing)
2. **Consistency**: Database moves from one valid state to another valid state
3. **Isolation**: Concurrent transactions don't interfere with each other
4. **Durability**: Committed changes persist even if the system crashes

---

## What Transactions DON'T Fix

### Problem: Time-of-Check to Time-of-Use (TOCTOU) Race Conditions

Even with transactions, checking a condition and then acting on it in separate operations allows race conditions.

**Example: Race Condition (❌ Even With Transactions)**

```csharp
await using var transaction = await connection.BeginTransactionAsync();

try
{
    // ⚠️ TOCTOU Race Condition: Check and Use are separate operations

    // Time of Check: Read book availability
    var book = await _bookRepository.GetByIdAsync(bookId, transaction);

    if (book.AvailableCopies > 0 && !book.IsDeleted)  // ✅ Check passes
    {
        // 🕐 RACE WINDOW: Another transaction might:
        //    - Mark book as deleted
        //    - Borrow the last copy
        //    - Set AvailableCopies to 0

        // Time of Use: Decrement availability
        book.BorrowCopy();  // Decrements AvailableCopies
        await _bookRepository.UpdateAsync(book, transaction);  // ❌ Updates with stale data

        // Create loan record
        var loan = Loan.Create(memberId, bookId);
        await _loanRepository.CreateAsync(loan, transaction);
    }

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}

// Result: Race condition allowed book to be borrowed when it shouldn't be!
// - Book might have been deleted between check and use
// - AvailableCopies might have gone negative
```

**Why Transactions Alone Don't Fix This:**

- Transactions provide **isolation** between different transactions
- But they don't prevent **race conditions within your application logic**
- The check (`if (book.AvailableCopies > 0)`) and the update (`BorrowCopy()`) are separate operations
- Between these two operations, another transaction can modify the data

---

## Complete Solutions for Race Conditions

### Solution 1: Atomic UPDATE with WHERE Clause (✅ RECOMMENDED)

Move the condition check into the WHERE clause of the UPDATE statement. The database performs the check and update **atomically** in a single operation.

**Implementation in BookRepository:**

```csharp
/// <summary>
/// Atomically decrements available copies for a book.
/// Checks availability and decrements in a SINGLE atomic operation.
/// </summary>
public async Task<bool> BorrowCopyAsync(int bookId, SqlTransaction transaction, CancellationToken cancellationToken = default)
{
    const string sql = @"
        UPDATE Books
        SET AvailableCopies = AvailableCopies - 1,
            UpdatedAt = @UpdatedAt
        WHERE Id = @Id
          AND AvailableCopies > 0    -- ✅ Check happens atomically with update
          AND IsDeleted = 0;          -- ✅ Prevent borrowing deleted books";

    var connection = transaction.Connection ?? throw new InvalidOperationException("Transaction has no associated connection");
    await using var command = new SqlCommand(sql, connection, transaction);
    command.Parameters.Add("@Id", SqlDbType.Int).Value = bookId;
    command.Parameters.Add("@UpdatedAt", SqlDbType.DateTime2).Value = DateTime.UtcNow;

    var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
    return rowsAffected > 0;  // ✅ Returns false if conditions weren't met
}
```

**Usage in LoanService:**

```csharp
public async Task<Loan> CreateLoanAsync(int memberId, int bookId, CancellationToken cancellationToken = default)
{
    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync(cancellationToken);
    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

    try
    {
        // Validate member eligibility (within transaction)
        var member = await _memberRepository.GetByIdAsync(memberId, transaction, cancellationToken);
        if (member == null || !member.CanBorrowBooks())
        {
            throw new InvalidOperationException("Member cannot borrow books");
        }

        // ✅ Atomically check AND decrement - prevents race condition
        var bookBorrowed = await _bookRepository.BorrowCopyAsync(bookId, transaction, cancellationToken);
        if (!bookBorrowed)
        {
            throw new InvalidOperationException("Book not available (no copies, deleted, or not found)");
        }

        // Create loan record
        var loan = Loan.Create(memberId, bookId);
        var createdLoan = await _loanRepository.CreateAsync(loan, transaction, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return createdLoan;
    }
    catch (Exception)
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}
```

**Why This Works:**

1. The `WHERE` clause checks conditions **atomically** (at the same time) with the `UPDATE`
2. SQL Server's locking ensures no other transaction can modify the row within single operation (between the check and update)
3. If conditions aren't met (book deleted, no copies available), `rowsAffected` is 0
4. The application can detect failure and handle it appropriately

### Solution 2: Pessimistic Locking with UPDLOCK

Force an exclusive lock on the row during the read, preventing other transactions from modifying it - very bad approach.

```csharp
// Read with UPDLOCK hint to acquire exclusive lock
const string sql = @"
    SELECT * FROM Books WITH (UPDLOCK, ROWLOCK)
    WHERE Id = @Id";

var book = await GetBookWithLock(id, transaction);

if (book.AvailableCopies > 0 && !book.IsDeleted)
{
    // ✅ No race condition: We hold exclusive lock
    book.BorrowCopy();
    await UpdateAsync(book, transaction);
}
```

**When to Use:**
- When you need to read and then perform complex business logic before updating
- When atomic UPDATE with WHERE clause isn't feasible
- **Tradeoff**: Reduces concurrency, can cause contention 

### Solution 3: Optimistic Concurrency Control

Use a version column or timestamp to detect if data changed since it was read.

```csharp
UPDATE Books
SET AvailableCopies = AvailableCopies - 1,
    Version = Version + 1,
    UpdatedAt = @UpdatedAt
WHERE Id = @Id
  AND Version = @OriginalVersion;  -- ✅ Fails if version changed

var rowsAffected = await command.ExecuteNonQueryAsync();
if (rowsAffected == 0)
{
    throw new ConcurrencyException("Book was modified by another transaction");
}
```

**When to Use:**
- In highly concurrent systems where conflicts are rare
- When you want to detect (rather than prevent) concurrent modifications
- **Tradeoff**: Requires retry logic, may frustrate users with frequent conflicts

---

## Implementation Patterns

### Pattern 1: Service Layer Manages Transactions (✅ Production Pattern)

The service layer creates and manages the transaction lifecycle. Repositories accept transactions as parameters.

**LoanService (Service Layer):**

```csharp
public class LoanService
{
    private readonly ILoanRepository _loanRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly string _connectionString;

    public async Task<Loan> CreateLoanAsync(int memberId, int bookId, CancellationToken cancellationToken = default)
    {
        // Service layer creates and manages transaction
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // All repository calls participate in the same transaction
            var member = await _memberRepository.GetByIdAsync(memberId, transaction, cancellationToken);
            var bookBorrowed = await _bookRepository.BorrowCopyAsync(bookId, transaction, cancellationToken);
            var loan = await _loanRepository.CreateAsync(Loan.Create(memberId, bookId), transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return loan;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

**Repository Interface:**

```csharp
public interface IBookRepository
{
    // All methods require SqlTransaction - not optional!
    Task<bool> BorrowCopyAsync(int bookId, SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<Book?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Book book, SqlTransaction transaction, CancellationToken cancellationToken = default);
}
```

**Benefits:**
- Clear ownership of transaction lifecycle
- Multiple repository calls can participate in single transaction
- Easy to understand and maintain
- Explicit transaction boundaries

## CHECK Constraints as Safety Nets

The `V005__add_check_constraints.sql` migration adds CHECK constraints to the Books table:

```sql
-- Prevent negative available copies
ALTER TABLE Books
ADD CONSTRAINT CHK_Books_AvailableCopies_NonNegative
CHECK (AvailableCopies >= 0);

-- Prevent available copies exceeding total copies
ALTER TABLE Books
ADD CONSTRAINT CHK_Books_AvailableCopies_LTE_TotalCopies
CHECK (AvailableCopies <= TotalCopies);
```

### Role of CHECK Constraints

**❌ NOT the Primary Defense:**
- CHECK constraints are a **last resort safety mechanism**
- They catch errors from:
  - Direct SQL manipulations bypassing application logic
  - Migration errors
  - Unexpected bugs

**✅ Primary Defense is Application Code:**
1. **Atomic operations** (BorrowCopyAsync with WHERE clause)
2. **Transaction management** (all-or-nothing operations)
3. **Business logic validation** (in service layer)

**Why CHECK Constraints Are Optional:**

If the application code is correct:
- Atomic operations prevent invalid states
- Transactions ensure consistency
- CHECK constraints should **never** be violated

However, they provide:
- Defense against manual SQL errors
- Peace of mind during development
- Clear documentation of data invariants

---

## Summary

### ✅ DO Use Transactions For:
- Multi-step operations that must succeed or fail together
- Any operation modifying multiple tables
- Coordinating repository calls

### ✅ DO Use Atomic Operations For:
- Preventing TOCTOU race conditions
- Checking conditions before updates
- High-concurrency scenarios

### ❌ DON'T Rely On:
- Transactions alone to prevent race conditions
- CHECK constraints as primary validation
- Separate check-then-update operations

### Best Practice Checklist:
- [ ] Service layer manages transaction lifecycle
- [ ] All repository methods accept SqlTransaction parameter
- [ ] Use atomic UPDATE with WHERE for concurrency-critical operations
- [ ] Proper try/catch with rollback handling
- [ ] CHECK constraints as safety net (optional but recommended)

---

*For implementation examples, see:*
- `src/DbDemo.ConsoleApp/Services/LoanService.cs` - Production transaction management
- `src/DbDemo.ConsoleApp/Infrastructure/Repositories/BookRepository.cs` - Atomic operations
- `src/DbDemo.ConsoleApp/Demos/DemoRunner.cs` - Demo helper patterns
