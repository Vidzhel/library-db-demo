# 11 - Automated Demo Scenarios

## 📖 What You'll Learn

- How to create pre-scripted demo scenarios for testing and presentations
- Difference between interactive testing and automated demonstrations
- Best practices for structuring demo code
- Organizing complex workflows into reusable scenarios
- Benefits of automated testing vs manual testing

## 🎯 Why This Matters

Automated demo scenarios serve multiple important purposes in software development:

1. **Demonstration**: Quickly show stakeholders how the system works without manual input
2. **Integration Testing**: Verify that all components work together correctly
3. **Onboarding**: Help new developers understand system capabilities
4. **Regression Testing**: Ensure new changes don't break existing functionality
5. **Documentation**: Live examples that demonstrate actual usage patterns

## 🔍 Key Concepts

### Demo vs Test

**Automated Demos** are designed to:
- Show complete end-to-end workflows
- Use realistic data that tells a story
- Include visual feedback and explanations
- Run with delays for presentation purposes
- Focus on happy paths and common scenarios

**Automated Tests** are designed to:
- Verify specific behaviors in isolation
- Use minimal data sufficient for verification
- Run as fast as possible (no delays)
- Assert expected outcomes
- Cover edge cases and error conditions

### The DemoRunner Pattern

The `DemoRunner` class follows several important design patterns:

```csharp
public class DemoRunner
{
    // Dependencies injected via constructor
    private readonly IBookRepository _bookRepository;
    private readonly IAuthorRepository _authorRepository;
    // ... other repositories

    // Configuration
    private readonly bool _withDelays;

    // Each scenario is a separate method
    public async Task RunScenario1_BasicBookManagementAsync() { }
    public async Task RunScenario2_AuthorManagementAsync() { }
    // ...
}
```

**Key Design Decisions:**
1. **Dependency Injection**: All repositories passed in via constructor (testable, flexible)
2. **Single Responsibility**: Each scenario method focuses on one workflow
3. **Clear Naming**: Method names describe what the scenario demonstrates
4. **Configurable Behavior**: `withDelays` parameter allows presentation vs testing mode
5. **Error Handling**: Try-catch blocks ensure one failure doesn't stop all demos

### Scenario Structure

Each demo scenario follows a consistent structure:

```csharp
public async Task RunScenario_ExampleAsync()
{
    PrintHeader("SCENARIO: Description");

    try
    {
        // Step 1: Setup
        PrintStep("Setting up test data...");
        // Create necessary objects
        await Delay();

        // Step 2: Action
        PrintStep("Performing main action...");
        // Execute the core functionality
        PrintSuccess("Action completed!");
        PrintInfo("  Details about what happened");
        await Delay();

        // Step 3: Verification
        PrintStep("Verifying results...");
        // Check that it worked correctly
        await Delay();

        PrintScenarioComplete("Scenario Name");
    }
    catch (Exception ex)
    {
        PrintError($"Scenario failed: {ex.Message}");
        throw;
    }
}
```

**Benefits of this structure:**
- **Predictable**: Every scenario follows the same pattern
- **Readable**: Clear visual separation between steps
- **Debuggable**: Easy to identify which step failed
- **Presentable**: Natural flow for live demonstrations

## 📋 Implemented Scenarios

### Scenario 1: Basic Book Management
**Purpose**: Demonstrates CRUD operations on books

**Flow**:
1. Get or create a category
2. Create a new book with full details
3. Create a second book
4. Search for books by title
5. Update book description
6. Display total book count

**Learning Points**:
- Repository Create/Read/Update operations
- Domain model usage (Book entity)
- Search functionality
- Data relationships (Book → Category)

### Scenario 2: Author Management
**Purpose**: Demonstrates author operations and data validation

**Flow**:
1. Create an author with biographical information
2. Create a second author
3. Search for authors by name
4. Update author biography
5. Display author statistics

**Learning Points**:
- Creating entities with optional properties
- Validation (email format)
- Calculated properties (Age from DateOfBirth)
- Search patterns

### Scenario 3: Member Management
**Purpose**: Demonstrates member lifecycle and business rules

**Flow**:
1. Register a new member
2. Register a second member
3. Update contact information
4. Extend membership
5. Display member statistics

**Learning Points**:
- Business rules (membership validity)
- Entity state changes (active/inactive)
- Membership expiration logic
- Contact information management

### Scenario 4: Complete Loan Workflow (Happy Path)
**Purpose**: Demonstrates a successful book loan from start to finish

**Flow**:
1. Setup: Get book and member
2. Verify member can borrow books
3. Check book availability
4. Create the loan
5. Update book availability (decrement)
6. Display active loans
7. Return the book
8. Restore book availability (increment)

**Learning Points**:
- Multi-step workflows
- Coordinating multiple entities
- State management across tables
- Transaction-like operations (create loan + update book)
- Business rule enforcement

### Scenario 5: Overdue Loan Scenario
**Purpose**: Demonstrates handling late returns and fees

**Flow**:
1. Setup test data
2. Create a loan (note: would be overdue in real scenario)
3. Check if loan is overdue
4. Calculate late fee (£0.50/day)
5. Return the book (captures late fee)
6. Process payment of late fee
7. Clear member's outstanding fees

**Learning Points**:
- Calculated properties (IsOverdue, DaysOverdue)
- Financial calculations
- State transitions (Active → ReturnedLate)
- Fee management

### Scenario 6: Loan Renewal
**Purpose**: Demonstrates renewal process and limits

**Flow**:
1. Create a loan
2. Renew the loan (1st time)
3. Renew again (2nd time)
4. Attempt 3rd renewal (blocked by limit)
5. Display loan statistics

**Learning Points**:
- Business rule enforcement (max renewals)
- Guard clauses (CanBeRenewed property)
- Exception handling (InvalidOperationException)
- Date calculations (extending due dates)

## 🛠 Code Organization

### Visual Feedback Helpers

The DemoRunner includes helper methods for consistent output:

```csharp
PrintHeader(string title)       // Section headers
PrintStep(string message)        // Action being performed
PrintSuccess(string message)     // Successful completion
PrintInfo(string message)        // Additional details
PrintWarning(string message)     // Non-critical issues
PrintError(string message)       // Errors
PrintScenarioComplete(string)    // Scenario completion banner
```

**Color Coding**:
- **Cyan**: Steps/actions
- **Green**: Success messages
- **Gray**: Informational details
- **Yellow**: Warnings
- **Red**: Errors

### Delay Mechanism

```csharp
private async Task Delay(int milliseconds = 1000)
{
    if (_withDelays)
    {
        await Task.Delay(milliseconds);
    }
}
```

**Why this is useful**:
- **Presentations**: Gives audience time to read output
- **Testing**: Set `withDelays: false` to run fast
- **Debugging**: Easier to follow execution flow
- **Configurable**: Adjustable per demo or globally

## 🧪 Integration with Console App

### Menu Integration

The demo scenarios are accessible via the main menu:

```
1. List All Books
2. Add New Book
...
7. Run Automated Demos  ← New option
0. Exit
```

Selecting option 7 opens a submenu:

```
1. Scenario 1: Basic Book Management
2. Scenario 2: Author Management
3. Scenario 3: Member Management
4. Scenario 4: Complete Loan Workflow (Happy Path)
5. Scenario 5: Overdue Loan Scenario
6. Scenario 6: Loan Renewal
7. Run ALL Scenarios  ← Runs all sequentially
0. Back to Main Menu
```

### Repository Initialization Check

Before running demos, the system verifies all repositories are initialized:

```csharp
private static bool AreAllRepositoriesInitialized()
{
    return _bookRepository != null &&
           _authorRepository != null &&
           _memberRepository != null &&
           _loanRepository != null &&
           _categoryRepository != null;
}
```

This prevents null reference exceptions and provides clear error messages.

## ⚠️ Common Pitfalls

### 1. Data Dependencies
**Problem**: Demos fail because required data doesn't exist
```csharp
// Bad: Assumes category exists
var category = await _categoryRepository.GetByIdAsync(1);
book.CategoryId = category.Id;  // NullReferenceException if category doesn't exist
```

**Solution**: Check for existence and create if needed
```csharp
// Good: Get or create
var categories = await _categoryRepository.GetPagedAsync(1, 1);
var category = categories.FirstOrDefault();

if (category == null)
{
    category = new Category("Fiction", "Fictional literature");
    category = await _categoryRepository.CreateAsync(category);
}
```

### 2. State Pollution
**Problem**: One scenario affects another
```csharp
// Scenario 1 creates "Test Book"
// Scenario 2 also tries to create "Test Book"
// Result: Duplicate key violation or unexpected results
```

**Solution**: Use unique identifiers or clean up
```csharp
// Use timestamps or GUIDs for uniqueness
var uniqueIsbn = $"978-0-{DateTime.Now.Ticks}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";
var book = new Book(uniqueIsbn, "Test Book", categoryId, 1);
```

### 3. Assuming Database State
**Problem**: Demo expects specific IDs or data
```csharp
// Bad: Hard-coded ID
var book = await _bookRepository.GetByIdAsync(42);
```

**Solution**: Query for data or create it
```csharp
// Good: Search or create
var books = await _bookRepository.SearchByTitleAsync("Harry Potter");
var book = books.FirstOrDefault() ?? await CreateTestBookAsync();
```

### 4. Not Handling Failures Gracefully
**Problem**: One failed demo stops all subsequent demos
```csharp
// Bad: No error handling
public async Task RunAllScenariosAsync()
{
    await RunScenario1Async();
    await RunScenario2Async();  // Never runs if Scenario1 throws
}
```

**Solution**: Catch and log errors
```csharp
// Good: Isolate failures
public async Task RunAllScenariosAsync()
{
    await SafeRunAsync(RunScenario1Async);
    await SafeRunAsync(RunScenario2Async);  // Runs even if Scenario1 fails
}

private async Task SafeRunAsync(Func<Task> scenario)
{
    try
    {
        await scenario();
    }
    catch (Exception ex)
    {
        PrintError($"Scenario failed: {ex.Message}");
    }
}
```

## ✅ Best Practices

### 1. Make Demos Idempotent
Demos should be runnable multiple times without side effects:
```csharp
// Check if data already exists before creating
var existingMember = await _memberRepository.GetByMembershipNumberAsync("MEM-DEMO-001");
if (existingMember == null)
{
    member = await _memberRepository.CreateAsync(member);
}
else
{
    member = existingMember;
}
```

### 2. Use Realistic Data
Use real-looking data that tells a story:
```csharp
// Bad: Generic data
var book = new Book("123", "Book", 1, 1);

// Good: Realistic data
var book = new Book(
    "978-0-545-01022-1",
    "Harry Potter and the Philosopher's Stone",
    fictionCategoryId,
    5
);
book.UpdatePublishingInfo(new DateTime(1997, 6, 26), 223, "English");
```

### 3. Separate Setup from Demonstration
```csharp
// Clear separation between setup and demo
PrintStep("Setting up test data...");
var book = await CreateTestBook();
var member = await CreateTestMember();
await Delay();

PrintStep("Now demonstrating the loan workflow...");
var loan = Loan.Create(member.Id, book.Id);
// ... actual demo logic
```

### 4. Include Verification Steps
Show that operations succeeded:
```csharp
PrintStep("Creating book...");
var book = await _bookRepository.CreateAsync(book);
PrintSuccess($"Book created with ID: {book.Id}");

// Verify by reading back
var verifiedBook = await _bookRepository.GetByIdAsync(book.Id);
PrintInfo($"  Verification: Book retrieved successfully");
```

### 5. Document What Each Scenario Teaches
```csharp
/// <summary>
/// Scenario 4: Complete Loan Workflow (Happy Path)
/// Demonstrates the full loan lifecycle from creation to return
///
/// Teaching Points:
/// - Multi-step workflows spanning multiple entities
/// - Business rule enforcement (member eligibility)
/// - State management (book availability)
/// - Coordinating repository operations
/// </summary>
public async Task RunScenario4_CompleteLoanWorkflowAsync()
{
    // Implementation
}
```

## 🔗 Learn More

### Official Documentation
- [xUnit Test Patterns](http://xunitpatterns.com/) - Test organization patterns
- [C# Async/Await Best Practices](https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID) - Relevant to demo organization

### Testing Philosophy
- [Arrange-Act-Assert Pattern](https://automationpanda.com/2020/07/07/arrange-act-assert-a-pattern-for-writing-good-tests/)
- [Test Automation Pyramid](https://martinfowler.com/articles/practical-test-pyramid.html)
- [Integration Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/integration-testing)

### Demo Techniques
- [Effective Software Demonstrations](https://www.mountaingoatsoftware.com/blog/how-to-deliver-an-impressive-sprint-demo)
- [Scriptable Demos](https://www.joelonsoftware.com/2001/01/29/top-five-wrong-reasons-you-dont-have-testers/)

## ❓ Discussion Questions

1. **When would you choose an automated demo over manual testing?**
   - Consider: presentation, regression testing, onboarding

2. **How would you modify the DemoRunner to support testing mode?**
   - Hint: Remove delays, capture output, assert results

3. **What happens if Scenario 4 creates a loan but fails to update book availability?**
   - Consider: data consistency, transactions (next chapter!)

4. **How could you make the demos safe to run on production data?**
   - Hint: Read-only queries, dry-run mode, separate test database

5. **What would change if you needed to demo the system to a non-technical audience?**
   - Consider: terminology, visual elements, pacing

6. **How would you track which scenarios have been run during a session?**
   - Consider: logging, state tracking, scenario history

7. **Should demo data be cleaned up after each scenario?**
   - Pros and cons of persistent vs ephemeral demo data

## 🎓 Exercises

### Exercise 1: Add a New Scenario
Create `Scenario 7: Damaged Book Processing` that demonstrates:
1. Creating a loan
2. Returning the book
3. Marking it as damaged with notes
4. Applying a damage fee

### Exercise 2: Add Failure Scenarios
Create scenarios that demonstrate error handling:
- Member tries to borrow when inactive
- Attempting to borrow an unavailable book
- Trying to renew an overdue loan

### Exercise 3: Add Performance Metrics
Modify DemoRunner to track and report:
- Duration of each scenario
- Number of database operations
- Success/failure counts

### Exercise 4: Create a Test Mode
Implement a test mode where:
- Delays are disabled
- Results are asserted instead of printed
- Failures throw exceptions for CI/CD integration

### Exercise 5: Add Scenario Dependencies
Implement a system where:
- Scenario 4 requires Scenario 1 and 3 to run first
- Clear error messages if dependencies aren't met
- Option to auto-run dependencies

## 📊 Measuring Success

Your automated demos are successful when:
- ✅ All scenarios complete without errors
- ✅ Output is clear and educational
- ✅ Each scenario is independent (can run alone)
- ✅ Scenarios can be run multiple times
- ✅ New team members can understand the system by running demos
- ✅ Demos catch regressions when code changes

## 🚀 Next Steps

In the next commits, we'll explore:
- **Transactions** (Commit 13): Making multi-step operations atomic
- **Isolation Levels** (Commit 14): Managing concurrent access
- **Performance Optimization** (Commits 16-18): Bulk operations and connection pooling

The demo scenarios you've created will help verify these advanced features work correctly!
