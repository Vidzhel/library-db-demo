# 20 - The Transaction Problem: Multi-Step Operations Without Transactions

> ⚠️ **WARNING**: This document explains an **ANTI-PATTERN**. The code in Commit 21 deliberately demonstrates incorrect transaction handling to illustrate the dangers. This will be fixed in Commit 22.

## 📖 What You'll Learn

- ACID properties and why they matter
- How multi-step database operations can fail partially
- The dangers of data inconsistency
- Real-world scenarios where missing transactions cause bugs
- Why transaction management is critical in business logic

## 🎯 Why This Matters

Imagine a bank transfer: money is deducted from your account but never added to the recipient's account due to a system crash. This is called **partial failure**, and it's exactly what happens when multi-step operations aren't wrapped in transactions.

In our library system, a similar problem occurs when:
1. A book's available copies are decremented (Step 1)
2. The system crashes before creating the loan record (Step 2)
3. Result: The book appears unavailable, but no loan exists

**Real-world impact:**
- **E-commerce**: Order created but inventory not decremented → overselling
- **Banking**: Money withdrawn but transfer not completed → lost funds
- **Healthcare**: Patient record updated but prescription not created → medication errors

## 🔍 Key Concepts

### ACID Properties

Transactions guarantee **ACID** properties:

| Property | Description | Example |
|----------|-------------|---------|
| **Atomicity** | All operations succeed or all fail (no partial completion) | Either both the book decrement AND loan creation happen, or neither happens |
| **Consistency** | Database moves from one valid state to another | Total books = available + on loan (always true) |
| **Isolation** | Concurrent transactions don't interfere | Two users can't borrow the last copy simultaneously |
| **Durability** | Committed changes persist even after crashes | Once a loan is confirmed, it survives server restarts |

### The Problem: Implicit Transactions

Without explicit transactions, each SQL statement runs in its own **auto-committed** transaction:

```csharp
// ⚠️ DANGER: Two separate transactions!
await _bookRepository.UpdateAsync(book);  // Transaction 1: COMMIT happens here
await _loanRepository.CreateAsync(loan);  // Transaction 2: COMMIT happens here
```

If the second line fails (network issue, constraint violation, server crash), the first transaction is already committed. **You can't roll it back.**

### What We're Demonstrating

The `LoanService` in Commit 21 has three problematic methods:

#### 1. CreateLoanAsync - Book Decremented, Loan Not Created

```csharp
// Step 3: Decrement available copies
book.BorrowCopy();
await _bookRepository.UpdateAsync(book, null, cancellationToken);  // ✅ COMMITTED

// Step 4: Create loan record
var loan = Loan.Create(memberId, bookId);
var createdLoan = await _loanRepository.CreateAsync(loan, null, cancellationToken);  // ❌ FAILS
```

**Problem**: If `CreateAsync` fails (duplicate key, disk full, connection timeout), the book's available copies have already been decremented. The book shows as unavailable, but no loan exists!

**Data inconsistency:**
```sql
-- Before operation:
Books.AvailableCopies = 5

-- After partial failure:
Books.AvailableCopies = 4  -- ❌ Decremented
Loans (no record)          -- ❌ Not created

-- Expected:
Books.AvailableCopies = 5  -- Should be unchanged
Loans (no record)          -- Correct
```

#### 2. ReturnLoanAsync - Loan Marked Returned, Book Not Incremented

```csharp
// Step 2: Mark loan as returned
loan.Return();
await _loanRepository.UpdateAsync(loan, null, cancellationToken);  // ✅ COMMITTED

// Step 3: Increment book available copies
var book = await _bookRepository.GetByIdAsync(loan.BookId, cancellationToken);
book.ReturnCopy();
await _bookRepository.UpdateAsync(book, null, cancellationToken);  // ❌ FAILS
```

**Problem**: If the book update fails, the loan is marked as returned, but the book's available copies aren't incremented. The system thinks the book is still on loan!

#### 3. Why This Is Hard to Debug

These bugs are **intermittent** and **hard to reproduce**:
- ✅ Works 99.9% of the time in testing (happy path)
- ❌ Fails randomly in production (network glitches, race conditions)
- 🔍 No error messages (first step succeeded, so no exception logged)
- 💰 Causes financial/inventory discrepancies over time

## ⚠️ Common Pitfalls

### Pitfall 1: "It Works in My Tests"

Tests rarely simulate failures between database operations. You need **fault injection** to test this:

```csharp
// Most tests only verify the happy path
var loan = await _loanService.CreateLoanAsync(memberId, bookId);
Assert.NotNull(loan);  // ✅ Passes

// But what if CreateAsync fails after UpdateAsync succeeds?
// Your test doesn't check for that!
```

### Pitfall 2: "I'll Add Error Handling"

Try-catch blocks **don't help** with partial failures:

```csharp
try
{
    await _bookRepository.UpdateAsync(book);  // ✅ Succeeds and commits
    await _loanRepository.CreateAsync(loan);  // ❌ Throws exception
}
catch (Exception ex)
{
    // Too late! Book update is already committed.
    // You can't "undo" it from here.
}
```

### Pitfall 3: "I'll Fix It With Compensating Actions"

Manually reversing changes is **error-prone**:

```csharp
try
{
    await _bookRepository.UpdateAsync(book);
    await _loanRepository.CreateAsync(loan);
}
catch (Exception)
{
    // Try to undo the book update
    book.ReturnCopy();
    await _bookRepository.UpdateAsync(book);  // ⚠️ This might also fail!
}
```

Problems:
- The compensating action might fail too
- Race conditions (another user modifies the book meanwhile)
- Complex logic (what if there were 5 steps?)

### Pitfall 4: "Databases Auto-Handle This"

**No.** Without explicit transactions, each statement auto-commits. The database has no idea your operations are related.

## ✅ Best Practices (Preview - Implemented in Commit 22)

### What We'll Fix in Commit 22

```csharp
// ✅ Proper transaction handling
await using var connection = new SqlConnection(_connectionString);
await connection.OpenAsync(cancellationToken);

await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
try
{
    // Both operations use the SAME transaction
    await _bookRepository.UpdateAsync(book, transaction, cancellationToken);
    await _loanRepository.CreateAsync(loan, transaction, cancellationToken);

    // If we reach here, both succeeded
    await transaction.CommitAsync(cancellationToken);
}
catch (Exception)
{
    // Rollback BOTH operations
    await transaction.RollbackAsync(cancellationToken);
    throw;
}
```

**Benefits:**
- **Atomicity**: Both operations succeed or both fail
- **No partial state**: If anything fails, the database rolls back all changes
- **Simpler error handling**: Just catch and rollback

### Transaction Propagation: Parameter Passing vs Dependency Injection

**In this project**, we propagate transactions via **method parameters**:

```csharp
// Repository methods accept optional SqlTransaction
Task<bool> UpdateAsync(Book book, SqlTransaction? transaction = null, ...);
```

**Why we chose this approach:**
- ✅ **Simple and explicit**: You can see exactly which operations share a transaction
- ✅ **Educational clarity**: Perfect for learning ADO.NET fundamentals
- ✅ **No framework dependencies**: Pure ADO.NET, no DI container required
- ✅ **Easy to understand**: Transaction lifetime is visible in code

**In production applications**, transactions are typically managed via **Dependency Injection**:

```csharp
// Production approach: Unit of Work pattern
public interface IUnitOfWork
{
    IBookRepository Books { get; }
    ILoanRepository Loans { get; }
    Task<int> CommitAsync();
    Task RollbackAsync();
}

// Service receives IUnitOfWork via constructor injection
public class LoanService
{
    private readonly IUnitOfWork _unitOfWork;

    public LoanService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Loan> CreateLoanAsync(int memberId, int bookId)
    {
        // Repositories automatically share the same transaction
        await _unitOfWork.Books.UpdateAsync(book);
        await _unitOfWork.Loans.CreateAsync(loan);

        // Commit all changes
        await _unitOfWork.CommitAsync();
    }
}
```

**Production advantages:**
- ✅ **Cleaner signatures**: No transaction parameters polluting method signatures
- ✅ **Automatic propagation**: All repositories in a scope share the same transaction
- ✅ **Framework integration**: Works seamlessly with ASP.NET Core DI, EF Core, Dapper
- ✅ **Testability**: Easy to mock `IUnitOfWork` for unit tests

**Why we didn't use it here:**
- ❌ Adds complexity (DI container, Unit of Work implementation)
- ❌ Obscures fundamentals (students don't see transaction creation)
- ❌ More code to explain (interfaces, registration, lifetime management)

**For learning ADO.NET**, parameter passing is ideal. **For production code**, use the Unit of Work pattern with DI.

## 🧪 Testing This Feature

### Integration Tests

The tests in `LoanServiceTests.cs` verify the **happy path** works:

```csharp
[Fact]
public async Task CreateLoanAsync_WithValidData_CreatesLoanSuccessfully()
{
    // This test passes, but it doesn't test partial failure!
    var loan = await _loanService.CreateLoanAsync(member.Id, book.Id);

    Assert.NotNull(loan);
    Assert.Equal(initialCopies - 1, updatedBook.AvailableCopies);
}
```

### What's Missing

To properly test the transaction problem, you'd need to:

1. **Simulate database failures** between operations
2. **Verify inconsistent state** occurs
3. **Check for data corruption**

Example (advanced):

```csharp
// Mock the loan repository to fail after book update succeeds
mockLoanRepo.Setup(x => x.CreateAsync(...))
    .ThrowsAsync(new SqlException("Constraint violation"));

// Try to create loan
await Assert.ThrowsAsync<SqlException>(
    () => _loanService.CreateLoanAsync(memberId, bookId));

// Verify the book was decremented (data inconsistency!)
var book = await _bookRepository.GetByIdAsync(bookId);
Assert.Equal(originalCopies - 1, book.AvailableCopies);  // ❌ Bug confirmed
```

In Commit 22, we'll add tests that verify rollback prevents this inconsistency.

## 🔗 Learn More

### Official Documentation
- [SQL Server Transactions](https://learn.microsoft.com/en-us/sql/t-sql/language-elements/transactions-transact-sql)
- [ADO.NET SqlTransaction Class](https://learn.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqltransaction)
- [ACID Properties Explained](https://learn.microsoft.com/en-us/sql/relational-databases/sql-server-transaction-locking-and-row-versioning-guide)

### Additional Reading
- [Martin Fowler - Unit of Work Pattern](https://martinfowler.com/eaaCatalog/unitOfWork.html)
- [Distributed Transactions and Two-Phase Commit](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/distributed-transactions)
- [CAP Theorem and Eventual Consistency](https://en.wikipedia.org/wiki/CAP_theorem)

### Related Patterns
- **Unit of Work**: Maintains list of objects affected by a business transaction
- **Repository Pattern**: Mediates between domain and data mapping layers
- **Saga Pattern**: Manages long-running transactions across distributed systems

## ❓ Discussion Questions

1. **Scenario**: An e-commerce site creates an order and decrements inventory in two separate operations. What could go wrong?

2. **Design**: Why doesn't the database automatically know that two operations should be treated as one transaction?

3. **Trade-offs**: Transactions provide safety but impact performance (locking). When might you accept the risk of no transactions?

4. **Real-world**: How would you detect and fix data inconsistencies that occurred from partial failures in production?

5. **Dependency Injection**: Why is the Unit of Work pattern preferred in production applications? What are the trade-offs compared to explicit parameter passing?

6. **Testing**: How would you design tests to catch transaction-related bugs before they reach production?

## 🔜 Next Steps

In **Commit 22**, we'll fix this anti-pattern by:
1. Adding `BeginTransactionAsync()` / `CommitAsync()` / `RollbackAsync()`
2. Passing the transaction to both repository calls
3. Writing tests that verify rollback behavior
4. Documenting proper transaction handling patterns

**Preview of the fix**:
```csharp
await using var connection = new SqlConnection(_connectionString);
await connection.OpenAsync();
await using var transaction = await connection.BeginTransactionAsync();

try
{
    await _bookRepository.UpdateAsync(book, transaction);
    await _loanRepository.CreateAsync(loan, transaction);
    await transaction.CommitAsync();  // ✅ Atomic!
}
catch
{
    await transaction.RollbackAsync();  // ✅ All-or-nothing!
    throw;
}
```

---

## Summary

- ⚠️ **The Problem**: Multi-step operations without transactions can fail partially
- 💥 **The Impact**: Data inconsistencies, lost data, business rule violations
- 🔒 **The Solution**: Wrap related operations in explicit transactions (Commit 22)
- 🏗️ **Production Approach**: Use Unit of Work pattern with dependency injection
- 📚 **Learning Approach**: Parameter passing for educational clarity
- 🧪 **Testing**: Happy path works, but fault injection needed to catch bugs

**Remember**: If you have multiple database operations that must all succeed or all fail together, you need an **explicit transaction**. The database won't figure it out for you!
