# 08 - Console Application Skeleton

## 📖 What You'll Learn

- How to structure a menu-driven console application
- User input validation patterns in C#
- Integration of repository pattern with presentation layer
- Error handling and user-friendly error messages
- Separation of concerns (UI logic vs. business logic)
- Best practices for console I/O operations

## 🎯 Why This Matters

Console applications are fundamental to understanding:
- **User interaction patterns**: The menu-driven approach is a classic pattern used in many systems
- **Input validation**: Real-world applications must handle invalid user input gracefully
- **Layer separation**: Keeping UI logic separate from data access is crucial for maintainability
- **CRUD operations**: Understanding how to implement Create, Read, Update, Delete is foundational

Even though modern applications typically use web or GUI interfaces, console applications:
- Are perfect for administrative tools and scripts
- Demonstrate core programming concepts without UI framework complexity
- Are excellent for learning and prototyping
- Are widely used in DevOps and automation scenarios

## 🔍 Key Concepts

### Menu-Driven Architecture

The application uses a **loop-based menu system**:

```csharp
while (running)
{
    DisplayMainMenu();
    var choice = GetUserChoice();

    switch (choice)
    {
        case 1: await ListBooksAsync(); break;
        case 2: await AddBookAsync(); break;
        // ... more cases
    }
}
```

**Benefits of this approach:**
- Clear user flow
- Easy to extend with new features
- Centralized error handling
- User can perform multiple operations without restarting

### Input Validation

The application uses **helper methods** to ensure valid input:

```csharp
private static int PromptForInt(string prompt)
{
    while (true)
    {
        Console.Write($"{prompt}: ");
        var input = Console.ReadLine();

        if (int.TryParse(input, out var value))
        {
            return value;
        }

        Console.WriteLine("❌ Please enter a valid number.");
    }
}
```

**Key validation patterns:**
- **Required fields**: Loop until valid input is provided
- **Optional fields**: Return `null` if user presses Enter
- **Type checking**: Use `TryParse` methods for safe conversion
- **User-friendly messages**: Clear feedback on what went wrong

### Repository Integration

The console app interacts with the database through the repository:

```csharp
// Initialize once
_bookRepository = new BookRepository(connectionString);

// Use throughout the application
var books = await _bookRepository.GetPagedAsync(pageNumber, pageSize);
```

**Benefits:**
- **Separation of concerns**: UI doesn't know about SQL
- **Testability**: Repository can be mocked for testing
- **Maintainability**: Database changes don't affect UI code
- **Reusability**: Same repository can be used by web API, CLI, etc.

### Display Formatting

The application uses different views for different scenarios:

**Summary view** (for lists):
```
[  42] The Great Gatsby                         | ISBN: 978-0-7432-7356-5 | ✓ Available (3/5)
```

**Detail view** (for single items):
```
╔════════════════════════════════════════════════════════════════════════════╗
  ID:              42
  Title:           The Great Gatsby
  ISBN:            978-0-7432-7356-5
  ...
╚════════════════════════════════════════════════════════════════════════════╝
```

This follows the **principle of appropriate detail** - show summaries in lists, full details when requested.

### Async/Await in Console Apps

All database operations use async methods:

```csharp
private static async Task AddBookAsync()
{
    // ... collect input
    var created = await _bookRepository.CreateAsync(book);
    // ... display result
}
```

**Why async in console apps?**
- Prepares code for future scaling (web apps require async)
- Doesn't block the thread during I/O operations
- Better resource utilization
- Consistent pattern across application layers

### Error Handling

The application uses **try-catch blocks** at appropriate levels:

```csharp
try
{
    var book = await _bookRepository.GetByIdAsync(id);
    // ... process book
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error retrieving book: {ex.Message}");
}
```

**Error handling strategy:**
- Catch at operation level (each menu handler)
- Display user-friendly messages
- Allow application to continue (don't crash)
- Log errors for debugging (future enhancement)

## ⚠️ Common Pitfalls

### 1. **Not Validating User Input**

```csharp
// ❌ BAD - Will crash on invalid input
var id = int.Parse(Console.ReadLine()!);

// ✅ GOOD - Handles invalid input gracefully
var id = PromptForInt("Enter Book ID");
```

### 2. **Swallowing Exceptions**

```csharp
// ❌ BAD - Error is hidden from user
try { /* ... */ } catch { }

// ✅ GOOD - User knows something went wrong
try { /* ... */ }
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}
```

### 3. **Mixing Concerns**

```csharp
// ❌ BAD - SQL in UI layer
Console.Write("Enter title: ");
var title = Console.ReadLine();
var sql = $"INSERT INTO Books (Title) VALUES ('{title}')"; // SQL injection risk!

// ✅ GOOD - Repository handles data access
var book = new Book(isbn, title, categoryId, totalCopies);
await _bookRepository.CreateAsync(book);
```

### 4. **Not Using Async Properly**

```csharp
// ❌ BAD - Blocks the thread
var book = _bookRepository.GetByIdAsync(id).Result;

// ✅ GOOD - Properly awaits
var book = await _bookRepository.GetByIdAsync(id);
```

### 5. **Poor User Experience**

```csharp
// ❌ BAD - Unclear what user should enter
Console.Write("Enter data: ");

// ✅ GOOD - Clear prompt with expected format
var isbn = PromptForString("ISBN (10 or 13 digits, may include dashes)");
```

## ✅ Best Practices

### 1. **Use Regions to Organize Code**

Group related methods together:
```csharp
#region Interactive Menu
// Menu display and navigation
#endregion

#region Book CRUD Operations
// CRUD handlers
#endregion

#region Helper Methods
// Input and display helpers
#endregion
```

### 2. **Provide Clear Visual Feedback**

- Use symbols (✅ ❌ ⚠️  📚 🔍) to make messages scannable
- Use box drawing characters for visual structure
- Clear separators between sections

### 3. **Make Required vs. Optional Clear**

```csharp
// Required field - loops until provided
var title = PromptForString("Title");

// Optional field - nullable return type
var subtitle = PromptForOptionalString("Subtitle (optional)");
```

### 4. **Confirm Destructive Actions**

```csharp
Console.Write("Are you sure you want to delete this book? (y/n): ");
var confirmation = Console.ReadLine()?.Trim().ToLower();

if (confirmation != "y" && confirmation != "yes")
{
    Console.WriteLine("❌ Deletion cancelled.");
    return;
}
```

### 5. **Use String Interpolation**

```csharp
// ✅ GOOD - Readable and type-safe
Console.WriteLine($"Found {books.Count} book(s) matching '{searchTerm}'");

// ❌ BAD - Harder to read
Console.WriteLine("Found " + books.Count + " book(s) matching '" + searchTerm + "'");
```

## 🧪 Testing This Feature

### Manual Testing Checklist

Test each menu option thoroughly:

**1. List Books**
- [ ] Displays books when database has data
- [ ] Shows appropriate message when no books exist
- [ ] Displays correct count

**2. Add Book**
- [ ] Creates book with all required fields
- [ ] Creates book with optional fields included
- [ ] Handles invalid ISBN format
- [ ] Handles invalid category ID (foreign key constraint)
- [ ] Displays newly created book with correct ID

**3. View Book Details**
- [ ] Shows full details for valid book ID
- [ ] Shows error for non-existent book ID
- [ ] Handles non-numeric input gracefully

**4. Update Book**
- [ ] Shows current values before update
- [ ] Allows keeping existing values (press Enter)
- [ ] Updates only changed fields
- [ ] Shows updated book after save

**5. Delete Book**
- [ ] Shows confirmation prompt
- [ ] Cancels when user enters 'n'
- [ ] Soft deletes book when confirmed
- [ ] Prevents deletion if copies are on loan

**6. Search Books**
- [ ] Finds books with partial title match
- [ ] Shows message when no matches found
- [ ] Handles special characters in search term

**General Testing**
- [ ] Invalid menu choice shows error
- [ ] Can return to menu after each operation
- [ ] Exit (0) terminates application cleanly
- [ ] Error messages are user-friendly
- [ ] Application doesn't crash on any input

### Running the Application

```bash
# From the project root
dotnet run --project src/DbDemo.ConsoleApp

# Or with build first
dotnet build
cd src/DbDemo.ConsoleApp/bin/Debug/net9.0
./DbDemo.ConsoleApp
```

### Future Enhancement: Automated UI Tests

While console apps are harder to test automatically than APIs, you could:
- Create a test harness that supplies predefined inputs
- Capture console output and verify expected messages
- Use dependency injection to mock `Console` I/O

Example framework: [Spectre.Console.Testing](https://spectreconsole.net/testing/testing)

## 🔗 Learn More

### Official Documentation
- [Console Class - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.console)
- [Console.ReadLine - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.console.readline)
- [String Interpolation in C#](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/interpolated)

### Design Patterns
- [Repository Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [Separation of Concerns](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles#separation-of-concerns)

### Console UI Libraries (Optional)
- [Spectre.Console](https://spectreconsole.net/) - Beautiful console UIs
- [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui) - Cross-platform terminal UI toolkit
- [Colorful.Console](https://github.com/tomakita/Colorful.Console) - Styled console output

### Best Practices
- [C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Exception Handling Best Practices](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)

## ❓ Discussion Questions

1. **Architecture**: Why is it important to keep database access logic separate from console UI logic? What happens if you mix them?

2. **Error Handling**: What's the difference between catching exceptions at the operation level (as we do) vs. catching them at the top level (in `Main`)? What are the trade-offs?

3. **User Experience**: How could we improve the pagination in the "List Books" feature? What would you add to make it more user-friendly?

4. **Validation**: The application validates input format (e.g., "is this a number?") but not business rules (e.g., "does this category ID exist?"). Where should business rule validation happen - in the UI, in the model, or in the repository?

5. **Testing**: Why is it harder to write automated tests for console applications compared to web APIs? How would you design the application differently to make it more testable?

6. **Async/Await**: In a console application with a single user, what are the actual benefits of using async/await? Would synchronous code be simpler?

7. **Extension**: If you needed to add a new entity (e.g., Authors), what files would you need to modify? What does this tell you about the application's extensibility?

8. **State Management**: The current application doesn't maintain any state between operations. How would you add a "shopping cart" feature where users can add books to a temporary list before "checking out"?

9. **Security**: What security considerations should you think about even for a console application? (Hint: think about connection strings, logging, input validation)

10. **Refactoring**: The `Program.cs` file is getting quite large. How would you refactor this into multiple classes while maintaining clarity? What principles would guide your refactoring decisions?

---

**Next Steps**: In future commits, we'll add more repositories (Authors, Members, Loans) and create automated demo scenarios to showcase complex workflows without manual input.
