# 02 - Domain Entities

## 📖 What You'll Learn

- What domain entities are and why they're important
- How to model a real-world domain in C# classes
- The difference between entities, value objects, and DTOs
- Designing one-to-many and many-to-many relationships
- C# properties: auto-properties, computed properties, navigation properties
- Using enums for type-safe state management
- XML documentation comments for code clarity

## 🎯 Why This Matters

**Domain modeling** is the art of representing real-world concepts in code. However, it's important to understand that:

> **A model is always a simplification of reality.** We cannot capture the full complexity of the real world in our applications. Instead, we model specific **facets** of reality that are relevant to our problem domain.

**Example**: A real book is a physical object with weight, color, smell, specific wear patterns, etc. But in our library system, we only model the facets that matter for lending: title, author, ISBN, availability. We don't model the book's weight or color because those aren't relevant to library operations.

This selective modeling is intentional and necessary. Every model is created for a specific purpose, and including irrelevant details would make the system unnecessarily complex.

**Domain modeling** is the foundation of any application. Before writing database code or user interfaces, you must understand and model your problem domain:

- **Clarity**: Forces you to think clearly about business rules and relationships
- **Communication**: Domain models serve as a common language between developers and business stakeholders
- **Maintenance**: Well-modeled domains are easier to understand and modify
- **Testing**: Pure domain logic (no database) is easy to unit test

**Quote from Eric Evans (Domain-Driven Design):**
> "The heart of software is its ability to solve domain-related problems for its user."

## 🔍 Key Concepts

### Entities vs Value Objects vs DTOs

| Concept | Description | Identity | Mutability | Example |
|---------|-------------|----------|------------|---------|
| **Entity** | Object with a unique identity that persists over time | Has ID | Mutable | `Book`, `Member`, `Loan` |
| **Value Object** | Object defined by its attributes, not identity | No ID | Immutable | `Money`, `Address`, `ISBN` |
| **DTO** | Data Transfer Object - just a data container | No ID | Mutable | API request/response models |

### What Makes an Entity?

An **entity** has:
1. **Unique Identity** - A primary key (`Id`) that distinguishes it from other instances
2. **Lifecycle** - Created, modified, and (sometimes) deleted
3. **Business Meaning** - Represents a real-world concept in your domain

**Example**: Two books with the same ISBN, title, and author are **different entities** if they're different physical copies in your library. Each has a unique `Id`.

### Entity Design Principles

#### 1. Rich Domain Model

**❌ Anemic Domain Model** (just data bags with no validation or behavior):
```csharp
public class Member
{
    public int Id { get; set; }
    public string Name { get; set; }  // Can be set to null, empty, or whitespace!
    public DateTime MembershipExpires { get; set; }  // Can be set to any date, even the past!
}
```

Problems:
- No validation - invalid states are possible
- No behavior - just a data container
- Business logic scattered throughout the application

**✅ Rich Domain Model** (with encapsulation, validation, and behavior):
```csharp
public class Member
{
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;

    public int Id { get; set; }

    // Encapsulated properties with validation (invariants)
    public string FirstName
    {
        get => _firstName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("First name cannot be empty", nameof(FirstName));

            if (value.Length > 50)
                throw new ArgumentException("First name cannot exceed 50 characters", nameof(FirstName));

            _firstName = value.Trim();
        }
    }

    public string LastName
    {
        get => _lastName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Last name cannot be empty", nameof(LastName));

            if (value.Length > 50)
                throw new ArgumentException("Last name cannot exceed 50 characters", nameof(LastName));

            _lastName = value.Trim();
        }
    }

    public DateTime MembershipExpiresAt { get; set; }

    // Computed properties (derived state)
    public string FullName => $"{FirstName} {LastName}";
    public bool IsMembershipValid => MembershipExpiresAt > DateTime.UtcNow;

    // Methods (behavior)
    public void ExtendMembership(int months)
    {
        if (months <= 0)
            throw new ArgumentException("Months must be positive", nameof(months));

        MembershipExpiresAt = MembershipExpiresAt.AddMonths(months);
    }
}
```

**Key Characteristics of Rich Models**:
- **Invariants**: Rules that must always be true (e.g., name cannot be empty)
- **Validation**: Enforced in setters, constructor, or methods
- **Encapsulation**: Private fields with validated public properties
- **Behavior**: Methods that operate on the entity's data
- **Impossible States**: Invalid states cannot be created

**Note**: In this demo, we use simpler auto-properties for readability, but in Commit 4 we'll add proper validation through separate validator classes and methods. True rich domain models would have validation in setters or use constructor-based initialization.

Our entities include computed properties like `IsOverdue`, `CanBeRenewed`, `Age` that encapsulate business logic, which is a step toward a richer model.

#### 2. Relationships

**One-to-Many**:
- One `Category` has many `Books`
- One `Member` has many `Loans`
- One `Book` has many `Loans`

**Many-to-Many**:
- Many `Books` have many `Authors` (via `BookAuthor` join table)

**Self-Referencing** (Hierarchical):
- One `Category` can have many child `Categories` (via `ParentCategoryId`)

#### 3. Naming Conventions

- **Entities**: Singular nouns (`Book`, not `Books`)
- **Collections**: Plural (`List<Book> Books`)
- **IDs**: `{EntityName}Id` (e.g., `CategoryId`, `MemberId`)
- **Dates**: Descriptive suffixes (`CreatedAt`, `UpdatedAt`, `BorrowedAt`)
- **Booleans**: Prefixed with `Is`, `Has`, `Can` (`IsActive`, `HasFee`, `CanRenew`)

## 📚 Introducing Our Domain: Library Management

Before diving into technical details, let's understand what we're modeling.

### The Real-World Library System

Our application models a **small community library** that needs to:

**Primary Operations**:
1. **Catalog books** - Track what books the library owns, who wrote them, and how to categorize them
2. **Manage members** - Keep track of who can borrow books, when memberships expire, and borrowing limits
3. **Handle circulation** - Check out books, track due dates, process returns, calculate late fees
4. **Organize content** - Group books into categories and sub-categories for easy discovery

**Business Rules** (that our model must support):
- Members can borrow up to 5 books simultaneously
- Loans are typically for 14 days
- Late fees accumulate for overdue books
- Memberships expire and must be renewed
- Some books may have multiple authors
- Categories can be hierarchical (Science → Physics, Science → Chemistry)

### Domain Model Flexibility: A Key Insight

**There is no single "correct" domain model.** Different requirements lead to different models:

**Small Library** (our case):
- Track books as titles with copy counts (`TotalCopies`, `AvailableCopies`)
- Simpler model, fewer entities

**Large Library**:
- Separate `Book` (bibliographic record) from `BookCopy` (physical item)
- Each copy has a barcode, condition rating, acquisition date
- More complex but more precise

**Digital Library**:
- No physical copies, different entities entirely
- `DigitalBook`, `License`, `DownloadLimit`

This flexibility is a core principle of **Domain-Driven Design**: model what matters for your specific use case, not a "universal" solution.

### Our Domain Entities (Preview)

We'll create six entities:
1. **Category** - Book categories/genres (hierarchical)
2. **Author** - People who write books
3. **Book** - Books in our collection
4. **Member** - Library patrons who borrow books
5. **Loan** - A borrowing transaction
6. **BookAuthor** - Links books to authors (many-to-many)

Each entity represents a concept from our domain, with properties and behaviors relevant to library operations.

### C# Property Types

#### Auto-Properties

```csharp
public string Title { get; set; } = string.Empty;
```

- Compiler generates backing field
- `= string.Empty` is the default value (required for non-nullable reference types)

#### Computed Properties (Expression-Bodied)

```csharp
public string FullName => $"{FirstName} {LastName}".Trim();
```

- No setter (read-only)
- Calculated on-the-fly each time accessed
- Not stored in database

#### Navigation Properties

```csharp
public Category? Category { get; set; }
public List<Loan> Loans { get; set; } = new();
```

- Represent relationships between entities
- Nullable (`?`) for optional relationships
- Collections initialized to empty list (`= new()`)

**Important**: These are for ORM use (Entity Framework Core). With ADO.NET, we populate these manually or use joins.

#### Nullable Properties

```csharp
public string? Description { get; set; }  // Can be NULL
public DateTime? ReturnedAt { get; set; } // Can be NULL
```

- `?` suffix means nullable
- Used for optional data

### Enums for Type Safety

Instead of storing strings like "Active", "Returned", "Overdue" in the database, we use **enums**:

```csharp
public enum LoanStatus
{
    Active = 0,
    Returned = 1,
    Overdue = 2,
    ReturnedLate = 3,
    Lost = 4,
    Damaged = 5,
    Cancelled = 6
}

public LoanStatus Status { get; set; }
```

**Benefits**:
- Type-safe (can't misspell "Active" as "Activ")
- IntelliSense support
- Easy to refactor
- Stored as integers in database (efficient)

## 📋 Our Domain Model

### Library Management System Entities

```
Category
└── Books (one-to-many)
    └── BookAuthors (many-to-many join)
        └── Authors (many-to-many)
    └── Loans (one-to-many)
        └── Members (many-to-one)

Category (hierarchical)
└── ParentCategory (self-referencing)
    └── ChildCategories (self-referencing)
```

### Entity Descriptions

#### 1. Category

Represents book categories/genres (Fiction, Science, History, etc.).

**Key Features**:
- Hierarchical structure (parent/child categories)
- Example: "Science Fiction" → parent: "Fiction"

**Properties**:
- `Id` - Primary key
- `Name` - Category name
- `ParentCategoryId` - For hierarchy (nullable)
- `Books` - Navigation property

#### 2. Author

Represents book authors.

**Key Features**:
- Computed `FullName` property
- Stores biography, nationality, birth date

**Properties**:
- `Id` - Primary key
- `FirstName`, `LastName`
- `FullName` - Computed (not in DB)
- `Books` - Many-to-many via BookAuthor

#### 3. Book

The core entity representing books in the library.

**Key Features**:
- ISBN for uniqueness
- Track total vs available copies
- Soft delete support (`IsDeleted`)
- Rich metadata (publisher, page count, language)

**Properties**:
- `Id` - Primary key
- `ISBN` - Unique identifier
- `Title`, `Subtitle`, `Description`
- `CategoryId` - Foreign key
- `TotalCopies`, `AvailableCopies`
- `IsAvailable` - Computed property

#### 4. Member

Represents library members who can borrow books.

**Key Features**:
- Membership expiration tracking
- Borrowing limits
- Outstanding fees tracking
- Age calculation

**Properties**:
- `Id` - Primary key
- `MembershipNumber` - Unique card number
- `Email` - Must be unique
- `MembershipExpiresAt` - Expiration date
- `MaxBooksAllowed` - Borrowing limit
- `OutstandingFees` - Late fees owed
- `IsMembershipValid` - Computed
- `Age` - Computed from `DateOfBirth`

#### 5. Loan

Represents a book borrowing transaction.

**Key Features**:
- Links Member to Book
- Tracks due dates, returns, late fees
- Renewal support
- Multiple statuses (enum)

**Properties**:
- `Id` - Primary key
- `MemberId`, `BookId` - Foreign keys
- `BorrowedAt`, `DueDate`, `ReturnedAt`
- `Status` - Enum (Active, Returned, Overdue, etc.)
- `LateFee`, `IsFeePaid`
- `RenewalCount`, `MaxRenewalsAllowed`
- `IsOverdue` - Computed
- `DaysOverdue` - Computed
- `CanBeRenewed` - Computed

#### 6. BookAuthor

Represents the relationship between Books and Authors.

**Why needed?**
- A book can have multiple authors (co-authored works)
- An author can write multiple books
- We need to track additional information about the relationship (author order, role)

**Note on persistence**: While this is conceptually a "many-to-many relationship", at the domain model level we're just expressing that Books have Authors and Authors have Books. How we persist this relationship (join tables, document databases, etc.) is an infrastructure concern we'll address when designing the database schema. For now, we focus on the domain behavior.

**Properties**:
- `BookId`, `AuthorId` - Composite primary key
- `AuthorOrder` - Display order
- `Role` - "Primary Author", "Editor", etc.

### Audit Fields

All entities have:
- `CreatedAt` - When the record was created
- `UpdatedAt` - When the record was last modified

These are useful for:
- Auditing and compliance
- Troubleshooting
- Data analysis (how long do memberships last?)

## 🏗️ Design Decisions

### Understanding Our Domain: Library Management

Before we dive into specific entities, let's understand what our library system needs to do:

**Core Features We Want to Support**:
1. **Catalog Management**: Track books, their authors, and categories
2. **Membership Management**: Manage library members, their memberships, and borrowing limits
3. **Circulation**: Handle book checkouts, returns, and renewals
4. **Fee Management**: Calculate and track late fees
5. **Reporting**: Generate statistics on popular books, overdue items, member activity

**Important Concept: The Domain Model Is Not Fixed**

There's no single "correct" domain model - it depends on your use cases and context. This is a core principle of **Domain-Driven Design (DDD)**.

**Example**: Consider how we might model books differently based on context:

**Context 1: Cataloging System**
- Entity: `Book` (focuses on bibliographic information)
- Properties: ISBN, Title, Authors, Publisher
- Purpose: Searching and discovering books

**Context 2: Inventory System**
- Entity: `PhysicalBookCopy` (focuses on individual copies)
- Properties: CopyId, Barcode, Condition, Location
- Purpose: Tracking specific physical items

**Context 3: Digital Library**
- Entity: `DigitalBook`
- Properties: FileFormat, FileSize, DRM_License
- Purpose: Managing digital content

**Our Choice**: We combine cataloging and basic inventory (via `TotalCopies`/`AvailableCopies`) because our use case is a small library where this simplification works. A larger library might need separate `Book` and `BookCopy` entities.

### The DDD Perspective: Bounded Contexts

In DDD, you might have:
- An `Author` entity in the **Cataloging Context**
- A `Contributor` entity in the **Royalty Payment Context**
- A `Creator` entity in the **Digital Rights Management Context**

These could all refer to the same person but modeled differently based on what matters in each context. This is called working within **Bounded Contexts**.

**Further Reading on DDD Modeling**:
- [Effective Aggregate Design Part I](https://www.dddcommunity.org/wp-content/uploads/files/pdf_articles/Vernon_2011_1.pdf) - Vaughn Vernon
- [Effective Aggregate Design Part II](https://www.dddcommunity.org/wp-content/uploads/files/pdf_articles/Vernon_2011_2.pdf) - Vaughn Vernon
- [Effective Aggregate Design Part III](https://www.dddcommunity.org/wp-content/uploads/files/pdf_articles/Vernon_2011_3.pdf) - Vaughn Vernon

### Why These Entities for Our Use Case?

Given our specific requirements (a small library management system for learning ADO.NET), this model covers:

1. **Catalog Management**: Books, Authors, Categories
2. **Membership Management**: Members with expiration and fees
3. **Circulation**: Loans with due dates and renewals
4. **Reporting**: Relationships enable queries like:
   - "Most borrowed books"
   - "Members with overdue loans"
   - "Books by author"
   - "Category popularity"

### What's NOT in This Model (Simplified)

Real library systems might also have:
- **Reservations** - Hold a book for future pickup
- **Fines/Payments** - Separate payment tracking
- **Staff** - Librarians vs administrators
- **Locations** - Multi-branch libraries
- **Physical Items** - Track individual book copies
- **Reviews/Ratings** - Member feedback

We keep it simple for learning ADO.NET fundamentals!

### Soft Delete vs Hard Delete

Notice `Book.IsDeleted`? This is **soft delete**:

```csharp
public bool IsDeleted { get; set; }
```

**Hard Delete**: `DELETE FROM Books WHERE Id = 1` (gone forever)
**Soft Delete**: `UPDATE Books SET IsDeleted = 1 WHERE Id = 1` (still in database)

**Benefits of Soft Delete**:
- Can restore deleted items
- Preserves historical data (loan records still valid)
- Audit trail intact

**Drawbacks**:
- Must filter `WHERE IsDeleted = 0` in queries
- Database grows larger

## 💡 Code Walkthrough

### Computed Properties Example

From `Loan.cs`:

```csharp
public bool IsOverdue
{
    get
    {
        if (ReturnedAt.HasValue)
            return false; // Already returned

        return DateTime.UtcNow > DueDate;
    }
}
```

**Why this is good**:
- Business logic lives in the entity (not scattered in queries)
- Testable without a database
- Consistent across the application

### Navigation Properties Example

From `Book.cs`:

```csharp
public Category? Category { get; set; }
public List<Author> Authors { get; set; } = new();
public List<Loan> Loans { get; set; } = new();
```

**With ADO.NET**:
- We populate these manually using JOINs
- Or lazy-load them when needed

**With Entity Framework**:
- Framework automatically loads related data
- Can eager-load with `.Include()`

### Nullable Reference Types

C# 9+ has nullable reference types enabled:

```csharp
public string Title { get; set; } = string.Empty;  // Required
public string? Subtitle { get; set; }              // Optional
```

**Benefits**:
- Compiler warns about potential null reference exceptions
- Documents intent (required vs optional)
- Safer code

## ⚠️ Common Pitfalls

### 1. Forgetting to Initialize Collections

**❌ Bad**:
```csharp
public List<Book> Books { get; set; }  // NULL by default!
```

Later:
```csharp
category.Books.Add(book);  // NullReferenceException!
```

**✅ Good**:
```csharp
public List<Book> Books { get; set; } = new();
```

### 2. Mutable Computed Properties

**❌ Bad**:
```csharp
public string FullName { get; set; }  // Can be changed!
```

**✅ Good**:
```csharp
public string FullName => $"{FirstName} {LastName}";  // Read-only
```

### 3. Not Using Enums for States

**❌ Bad**:
```csharp
public string Status { get; set; }  // "Active", "Overdue", "Retuned" <- typo!
```

**✅ Good**:
```csharp
public LoanStatus Status { get; set; }  // Type-safe enum
```

### 4. Anemic Domain Model

**❌ Bad**:
```csharp
public class Member
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

// Somewhere else in code:
var fullName = member.FirstName + " " + member.LastName;
```

**✅ Good**:
```csharp
public class Member
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
}

// Usage:
var fullName = member.FullName;  // Encapsulated!
```

## ✅ Checklist

Before moving to the next commit:

- [ ] All entity classes created in `Models/` folder
- [ ] Each entity has XML documentation comments
- [ ] Primary keys (`Id`) defined
- [ ] Foreign keys defined (`CategoryId`, `MemberId`, etc.)
- [ ] Navigation properties added
- [ ] Computed properties make sense
- [ ] Collections initialized (`= new()`)
- [ ] Nullable properties marked with `?`
- [ ] Enums defined where appropriate
- [ ] `ToString()` overrides for debugging

## 🔗 Learn More

### Domain-Driven Design

- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/) - The original book
- [DDD Fundamentals - Pluralsight](https://www.pluralsight.com/courses/domain-driven-design-fundamentals)
- [DDD Reference](https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf) - Free summary PDF

### C# Language Features

- [Properties in C#](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties)
- [Auto-Implemented Properties](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/auto-implemented-properties)
- [Expression-Bodied Members](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/expression-bodied-members)
- [Nullable Reference Types](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references)
- [Enums](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum)

### Database Design

- [Database Normalization](https://learn.microsoft.com/en-us/office/troubleshoot/access/database-normalization-description) - Understanding normal forms
- [Many-to-Many Relationships](https://www.essentialsql.com/many-many-relationships/) - Why join tables?
- [Primary Keys and Foreign Keys](https://www.essentialsql.com/what-is-the-difference-between-a-primary-key-and-a-foreign-key/)

### Patterns and Practices

- [Anemic Domain Model (Anti-pattern)](https://martinfowler.com/bliki/AnemicDomainModel.html) - Martin Fowler
- [Rich Domain Model](https://enterprisecraftsmanship.com/posts/domain-model-purity-lazy-loading/) - Vladimir Khorikov
- [Soft Delete Pattern](https://www.mikealche.com/software-development/a-soft-delete-implementation-using-hibernate-and-jpa)

## ❓ Discussion Questions

1. **Why separate `FirstName` and `LastName` instead of just `Name`?**
   - Think about sorting, searching, internationalization

2. **When would you use an entity vs a value object?**
   - Consider: Does it have an identity that matters over time?

3. **Why have both `TotalCopies` and `AvailableCopies` on Book?**
   - Could you compute one from the other?
   - What are the trade-offs (performance vs data consistency)?

4. **Should business logic go in entities or in separate service classes?**
   - Research: Anemic vs Rich domain models
   - When is each appropriate?

5. **Why use enums stored as integers instead of strings in the database?**
   - Consider: Performance, storage, type-safety, refactoring

6. **What would happen if you removed the `BookAuthor` join table?**
   - How would you model the many-to-many relationship?
   - Why doesn't SQL support many-to-many directly?

## 🎯 Next Steps

Now that we have our domain entities defined, we'll:

1. **Commit 4**: Add validation logic and business rules
   - ISBN validation
   - Email format validation
   - Business rule enforcement (max books allowed, etc.)

2. **Commit 5**: Write unit tests for our domain logic
   - Test computed properties
   - Test validation rules
   - Test without touching the database!

These entities are **pure C# classes** with no database dependencies. This is good design - domain logic should not depend on infrastructure!

**Excellent progress! Your domain model is taking shape! 🏗️**
