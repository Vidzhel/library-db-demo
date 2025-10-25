# 03 - Domain Unit Tests

## 📖 What You'll Learn

- What unit tests are and why they're essential
- The AAA (Arrange-Act-Assert) pattern for test structure
- xUnit test framework fundamentals
- Theory tests with inline data for parameterized testing
- FluentAssertions for readable test assertions
- Testing constructors, validation, behavior methods, and computed properties
- How to test domain logic without touching the database

## 🎯 Why This Matters

**Unit testing** is a cornerstone of professional software development:

- **Confidence**: Ensures code works as expected
- **Documentation**: Tests show how code is meant to be used
- **Refactoring Safety**: Catch regressions when changing code
- **Design Feedback**: Hard-to-test code often indicates design problems
- **Fast Feedback**: Tests run in milliseconds without external dependencies

> "Code without tests is broken by design." - Jacob Kaplan-Moss

### Why Test Domain Logic?

Domain entities contain our **business rules**. Testing them ensures:
- Validation works correctly (e.g., ISBN format, email validation)
- Business rules are enforced (e.g., can't borrow when fees exceed threshold)
- Computed properties calculate correctly (e.g., Age, IsOverdue)
- Behavior methods maintain invariants (e.g., AvailableCopies ≤ TotalCopies)

**Best part**: Domain tests run **fast** - no database, no network, no external dependencies!

## 🔍 Key Concepts

### Unit Test vs Integration Test

| Unit Test | Integration Test |
|-----------|------------------|
| Tests single unit (class/method) in isolation | Tests multiple units working together |
| No external dependencies (DB, API, filesystem) | Uses real dependencies |
| Runs in milliseconds | Runs in seconds/minutes |
| Example: `Book.BorrowCopy()` decrements available copies | Example: Save book to database and verify row exists |

**Rule of thumb**: Write many unit tests, fewer integration tests.

### The AAA Pattern

Every test follows **Arrange-Act-Assert** structure:

```csharp
[Fact]
public void BorrowCopy_WhenAvailable_DecreasesAvailableCopies()
{
    // Arrange: Set up test data
    var book = new Book("978-0-13-468599-1", "Clean Code", 1, 5);

    // Act: Perform the operation
    book.BorrowCopy();

    // Assert: Verify the outcome
    book.AvailableCopies.Should().Be(4);
}
```

**Why AAA?**
- **Readability**: Clear what's being tested
- **Consistency**: All tests follow same pattern
- **Maintainability**: Easy to understand and modify

### xUnit Attributes

**`[Fact]`** - A single test case:
```csharp
[Fact]
public void Constructor_WithValidData_CreatesBook()
{
    // Test code
}
```

**`[Theory]`** - Multiple test cases with different data:
```csharp
[Theory]
[InlineData("978-0134685991")]  // ISBN-13
[InlineData("0134685997")]       // ISBN-10
public void Constructor_WithValidISBN_CreatesBook(string isbn)
{
    var book = new Book(isbn, "Clean Code", 1, 5);
    book.ISBN.Should().Be(isbn);
}
```

### FluentAssertions

Makes assertions **readable** and provides **clear error messages**:

**Before** (traditional):
```csharp
Assert.Equal(5, book.AvailableCopies);
// Error: "Expected 5 but was 3"
```

**After** (FluentAssertions):
```csharp
book.AvailableCopies.Should().Be(5);
// Error: "Expected book.AvailableCopies to be 5, but found 3"
```

Common assertions:
```csharp
value.Should().Be(expected);
value.Should().BeNull();
value.Should().BeGreaterThan(10);
value.Should().BeBetween(1, 10);
string.Should().Contain("substring");
collection.Should().HaveCount(5);
collection.Should().Contain(item);
date.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
```

### Test Naming Convention

We use: `MethodName_Scenario_ExpectedBehavior`

Examples:
- `Constructor_WithValidData_CreatesBook`
- `BorrowCopy_WhenNoAvailableCopies_ThrowsInvalidOperationException`
- `CanBorrowBooks_WhenFeesExceedThreshold_ReturnsFalse`

**Benefits**:
- Test name describes what's being tested
- Easy to identify failing tests
- Serves as documentation

## 📊 Test Coverage Overview

### CategoryTests (11 tests)
- ✅ Constructor with valid/invalid data
- ✅ Name validation (empty, too long, whitespace trimming)
- ✅ UpdateDetails behavior
- ✅ IsTopLevel computed property
- ✅ ToString formatting

### AuthorTests (15 tests)
- ✅ Constructor with valid/invalid data
- ✅ Name and email validation
- ✅ FullName computed property
- ✅ Age calculation (with/without birthday)
- ✅ UpdateDetails and UpdateBiography
- ✅ ToString formatting

### BookTests (30 tests)
- ✅ Constructor with valid/invalid ISBN and title
- ✅ ISBN validation (10-digit, 13-digit, with/without hyphens)
- ✅ BorrowCopy/ReturnCopy inventory management
- ✅ AddCopies functionality
- ✅ MarkAsDeleted with business rules
- ✅ IsAvailable computed property
- ✅ Update methods (Details, PublishingInfo, ShelfLocation)
- ✅ CopiesOnLoan calculation

### MemberTests (25 tests)
- ✅ Constructor with valid/invalid data
- ✅ Email validation and normalization
- ✅ FullName and Age computed properties
- ✅ IsMembershipValid with various scenarios
- ✅ ExtendMembership behavior
- ✅ Activate/Deactivate
- ✅ AddFee/PayFee with validation
- ✅ CanBorrowBooks business rule

### LoanTests (30 tests)
- ✅ Loan.Create factory method
- ✅ IsOverdue and DaysOverdue computed properties
- ✅ CanBeRenewed eligibility checking
- ✅ Renew with validation
- ✅ Return behavior (on-time vs late)
- ✅ CalculateLateFee logic
- ✅ MarkAsLost/MarkAsDamaged
- ✅ PayLateFee functionality
- ✅ ToString formatting for different states

### BookAuthorTests (8 tests)
- ✅ Constructor with valid data
- ✅ UpdateOrder with validation
- ✅ UpdateRole behavior
- ✅ ToString formatting

**Total: 119 unit tests** covering all domain logic!

## 🧪 Running The Tests

### Command Line

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run tests in a specific project
dotnet test tests/DbDemo.Domain.Tests

# Run tests matching a filter
dotnet test --filter "FullyQualifiedName~BookTests"

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Visual Studio / Rider

- **Test Explorer**: View > Test Explorer (or Ctrl+E, T)
- **Run All**: Click "Run All" button
- **Run Single Test**: Right-click test → Run
- **Debug Test**: Right-click test → Debug

### Expected Output

```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:   119, Skipped:     0, Total:   119, Duration: 245 ms
```

## 💡 Example Walkthrough

### Testing Constructor Validation

```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("   ")]
public void Constructor_WithInvalidName_ThrowsArgumentException(string? invalidName)
{
    // Arrange (data comes from InlineData)

    // Act
    Action act = () => new Category(invalidName!);

    // Assert
    act.Should().Throw<ArgumentException>()
        .WithMessage("*Category name cannot be empty*");
}
```

**What this tests**:
1. Category constructor validates name
2. `null`, empty string, and whitespace all throw `ArgumentException`
3. Error message contains expected text

**Why `Action act = () => ...`?**
- FluentAssertions needs a delegate to invoke and catch the exception
- Can't directly call constructor in assert - exception would be uncaught

### Testing Behavior Methods

```csharp
[Fact]
public void BorrowCopy_WhenAvailable_DecreasesAvailableCopies()
{
    // Arrange
    var book = new Book("978-0-13-468599-1", "Clean Code", 1, 5);

    // Act
    book.BorrowCopy();

    // Assert
    book.AvailableCopies.Should().Be(4);
    book.CopiesOnLoan.Should().Be(1);
}
```

**What this tests**:
- `BorrowCopy()` method decreases available copies
- Computed property `CopiesOnLoan` reflects the change

### Testing Computed Properties

```csharp
[Fact]
public void Age_CalculatesCorrectAge()
{
    // Arrange
    var dateOfBirth = DateTime.Today.AddYears(-25);
    var member = new Member("LIB-001", "John", "Doe", "john@example.com", dateOfBirth);

    // Act & Assert
    member.Age.Should().Be(25);
}

[Fact]
public void Age_BeforeBirthday_CalculatesCorrectAge()
{
    // Arrange
    var dateOfBirth = DateTime.Today.AddYears(-25).AddDays(1); // Birthday tomorrow

    var member = new Member("LIB-001", "John", "Doe", "john@example.com", dateOfBirth);

    // Act & Assert
    member.Age.Should().Be(24); // Not yet 25
}
```

**What this tests**:
- Age calculation handles birthday correctly
- Tests edge case (birthday hasn't happened yet this year)

### Testing Business Rules

```csharp
[Fact]
public void CanBorrowBooks_WhenFeesExceedThreshold_ReturnsFalse()
{
    // Arrange
    var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);
    member.AddFee(15.00m); // Over $10 threshold

    // Act & Assert
    member.CanBorrowBooks().Should().BeFalse();
}

[Fact]
public void CanBorrowBooks_WhenFeesAtThreshold_ReturnsTrue()
{
    // Arrange
    var member = new Member("LIB-001", "John", "Doe", "john@example.com", DateTime.Today);
    member.AddFee(10.00m); // Exactly at threshold

    // Act & Assert
    member.CanBorrowBooks().Should().BeTrue();
}
```

**What this tests**:
- Business rule: Members with fees > $10 cannot borrow
- Edge case: Exactly $10 is allowed (tests boundary)

## ⚠️ Common Pitfalls

### 1. Testing Implementation Instead of Behavior

**❌ Bad**:
```csharp
[Fact]
public void AddFee_CallsSetterForOutstandingFees()
{
    // Testing internal implementation detail
}
```

**✅ Good**:
```csharp
[Fact]
public void AddFee_IncreasesOutstandingFees()
{
    var member = new Member(...);
    member.AddFee(5.00m);
    member.OutstandingFees.Should().Be(5.00m);
}
```

### 2. Multiple Assertions Without Clear Focus

**❌ Bad**:
```csharp
[Fact]
public void Constructor_CreatesBook()
{
    var book = new Book(...);
    book.Title.Should().Be("Clean Code");
    book.ISBN.Should().NotBeNull();
    book.TotalCopies.Should().BeGreaterThan(0);
    // etc... testing everything
}
```

**✅ Good**:
Multiple focused tests, each with clear purpose.

### 3. Not Testing Edge Cases

Always test:
- Boundary values (0, -1, max)
- Null values
- Empty collections
- Edge cases (birthday, expiration dates)

### 4. Fragile Time-Based Tests

**❌ Bad**:
```csharp
loan.BorrowedAt.Should().Be(DateTime.UtcNow); // Fails if microseconds differ!
```

**✅ Good**:
```csharp
loan.BorrowedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
```

## ✅ Best Practices

### 1. Test One Thing Per Test

Each test should verify **one specific behavior**:
```csharp
[Fact]
public void BorrowCopy_DecreasesAvailableCopies() { ... }

[Fact]
public void BorrowCopy_WhenNoAvailableCopies_ThrowsException() { ... }
```

### 2. Make Tests Independent

Tests should **not depend on each other**:
- No shared mutable state
- Each test creates its own test data
- Tests can run in any order

### 3. Use Descriptive Names

Test name should describe:
- What's being tested
- Under what conditions
- What's expected

### 4. Test Negative Cases

Don't just test the "happy path":
```csharp
[Fact]
public void BorrowCopy_WhenAvailable_Succeeds() { ... }

[Fact]
public void BorrowCopy_WhenNoAvailableCopies_Throws() { ... }

[Fact]
public void BorrowCopy_WhenDeleted_Throws() { ... }
```

### 5. Use Theory for Similar Tests

Instead of multiple similar Fact tests:
```csharp
[Theory]
[InlineData("0134685997")]
[InlineData("9780134685991")]
[InlineData("978-0-13-468599-1")]
public void Constructor_WithValidISBN_CreatesBook(string isbn) { ... }
```

## 🔗 Learn More

### xUnit Documentation
- [xUnit.net](https://xunit.net/) - Official documentation
- [Getting Started](https://xunit.net/docs/getting-started/netcore/cmdline) - Tutorial
- [xUnit Attributes](https://xunit.net/docs/comparisons) - Fact vs Theory

### Fluent Assertions
- [FluentAssertions Documentation](https://fluentassertions.com/introduction) - Complete guide
- [Assertion Scope](https://fluentassertions.com/introduction#assertion-scopes) - Advanced usage
- [Tips and Tricks](https://fluentassertions.com/tips/) - Best practices

### Unit Testing Principles
- [Art of Unit Testing by Roy Osherove](https://www.artofunittesting.com/) - Comprehensive book
- [Unit Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices) - Microsoft guide
- [Test-Driven Development (TDD)](https://martinfowler.com/bliki/TestDrivenDevelopment.html) - Martin Fowler

### Video Tutorials
- [Unit Testing C# Code - IAmTimCorey](https://www.youtube.com/watch?v=HYrXogLj7vg)
- [xUnit Tutorial - Codecademy](https://www.youtube.com/results?search_query=xunit+tutorial)

## ❓ Discussion Questions

1. **Why write tests for domain logic instead of just integration tests?**
   - Think about: Speed, isolation, pinpointing failures

2. **When would a test with multiple assertions be acceptable?**
   - Consider: Testing object initialization, related properties

3. **Should you test private methods?**
   - Research: Why testing through public interface is better

4. **How do you decide what to test?**
   - Think about: Business rules, edge cases, validation

5. **What's the difference between Fact and Theory?**
   - When would you use each?

6. **Why use FluentAssertions instead of Assert.Equal?**
   - Consider: Readability, error messages, chaining

## 🎯 Test Coverage Goals

**What we've achieved**:
- ✅ 119 unit tests covering all domain entities
- ✅ Constructor validation tested
- ✅ All behavior methods tested
- ✅ Computed properties verified
- ✅ Business rules validated
- ✅ Edge cases covered

**Metrics**:
- **Fast**: All 119 tests run in ~250ms
- **Isolated**: No database or external dependencies
- **Maintainable**: Clear naming and AAA structure
- **Comprehensive**: All public methods and properties tested

## 🚀 Next Steps

Now that domain logic is thoroughly tested:

1. **Commit 5**: Integration test infrastructure setup
2. **Commit 6**: Database schema and initial migrations
3. **Commit 7**: Repository implementation with ADO.NET
4. **Commit 8**: Integration tests for repositories

The solid foundation of unit tests gives us confidence that domain logic works correctly. When we add database code, we'll know any issues are in the infrastructure layer, not the domain!

**Great work! Your domain is now bulletproof! 🎉**
