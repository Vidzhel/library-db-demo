# Entity Model Improvements - Rich Domain Model with Immutability

## 🎯 What Changed

All entity models have been completely rewritten to follow **true rich domain model** principles with proper encapsulation and immutability.

### Key Improvements

#### 1. Constructor-Based Initialization

**Before** (Anemic - anything can be set):
```csharp
var loan = new Loan();
loan.MemberId = 1;
loan.BookId = 2;
loan.BorrowedAt = DateTime.UtcNow;
loan.DueDate = DateTime.UtcNow.AddDays(14);
// Forgot to set Status? Now it's default(LoanStatus) - INVALID STATE!
```

**After** (Rich - enforced valid state):
```csharp
var loan = Loan.Create(memberId: 1, bookId: 2);
// All required fields automatically set with valid defaults
// Status = Active, RenewalCount = 0, MaxRenewals = 2, dates set correctly
```

#### 2. Private Setters - No Direct Property Modification

**Before**:
```csharp
loan.DueDate = loan.DueDate.AddDays(7); // Direct manipulation
loan.RenewalCount++;                     // Manual tracking
// What if someone forgets to increment RenewalCount?
```

**After**:
```csharp
loan.Renew(additionalDays: 7);  // Behavior method handles ALL state changes
// Automatically: updates DueDate, increments RenewalCount, validates CanBeRenewed
```

#### 3. Private Parameterless Constructors for ADO.NET

```csharp
private Loan() { }  // For infrastructure (ADO.NET/ORM) to materialize objects
```

**Why?**
- ADO.NET needs a parameterless constructor to create instances when reading from database
- Making it `private` prevents accidental creation of invalid objects in domain code
- Public constructors enforce valid initialization

#### 4. Factory Methods for Complex Creation

```csharp
public static Loan Create(int memberId, int bookId)
{
    var now = DateTime.UtcNow;
    return new Loan(memberId, bookId, now, now.AddDays(DefaultLoanPeriodDays));
}
```

**Benefits**:
- Encapsulates creation logic
- Ensures all business rules applied
- Clear intent ("Create a loan" vs "new Loan")

## 📋 Entity-by-Entity Changes

### Category

**Constructor**:
```csharp
public Category(string name, string? description = null, int? parentCategoryId = null)
```

**Behavior Methods**:
- `UpdateDetails(string name, string? description)` - Updates category info

**Immutable After Creation**:
- `Id` (set by database)
- `CreatedAt`
- `ParentCategoryId` (structural - shouldn't change)

### Author

**Constructor**:
```csharp
public Author(string firstName, string lastName, string? email = null)
```

**Behavior Methods**:
- `UpdateDetails(string firstName, string lastName, string? email)` - Updates core info
- `UpdateBiography(string? biography, DateTime? dateOfBirth, string? nationality)` - Updates biographical info

**Computed Properties**:
- `FullName` - Derived from FirstName + LastName
- `Age` - Calculated from DateOfBirth

### Book

**Constructor**:
```csharp
public Book(string isbn, string title, int categoryId, int totalCopies)
```

**Behavior Methods**:
- `UpdateDetails(...)` - Updates book metadata
- `UpdatePublishingInfo(...)` - Updates publishing details
- `UpdateShelfLocation(string location)` - Changes location
- `AddCopies(int count)` - Increases inventory
- `BorrowCopy()` - Decrements available copies with validation
- `ReturnCopy()` - Increments available copies with validation
- `MarkAsDeleted()` - Soft delete (only if no copies on loan)

**Immutable After Creation**:
- `ISBN` - Book identifier never changes
- `CategoryId` - Structural (would require new book record to change)

**Why these methods?**
- `BorrowCopy()` / `ReturnCopy()` ensure AvailableCopies never exceeds TotalCopies or goes negative
- Encapsulates inventory management logic

### Member

**Constructor**:
```csharp
public Member(string membershipNumber, string firstName, string lastName, string email, DateTime dateOfBirth)
```

**Behavior Methods**:
- `UpdateContactInfo(string email, string? phoneNumber, string? address)` - Updates contact details
- `ExtendMembership(int months)` - Extends expiration date
- `Activate()` / `Deactivate()` - Membership status
- `AddFee(decimal amount)` - Adds late fee
- `PayFee(decimal amount)` - Records payment
- `CanBorrowBooks()` - Business rule check

**Immutable After Creation**:
- `MembershipNumber` - Member identifier
- `DateOfBirth` - Never changes
- `MemberSince` - Historical record

**Why these methods?**
- `PayFee()` validates payment doesn't exceed outstanding amount
- `ExtendMembership()` extends from current expiration (not from today)
- `CanBorrowBooks()` encapsulates eligibility logic

### Loan

**Factory Method** (instead of public constructor):
```csharp
public static Loan Create(int memberId, int bookId)
```

**Behavior Methods**:
- `Renew(int additionalDays)` - Extends loan with validation
- `Return()` - Processes return, calculates fees, updates status
- `CalculateLateFee()` - Calculates late fee amount
- `MarkAsLost()` - Changes status to Lost
- `MarkAsDamaged(string notes)` - Changes status to Damaged with notes
- `PayLateFee()` - Records fee payment

**COMPLETELY IMMUTABLE After Creation**:
- `MemberId` - Who borrowed it never changes
- `BookId` - What was borrowed never changes
- `BorrowedAt` - When it was borrowed never changes
- `DueDate` - Can only be extended via `Renew()`, not set directly
- `ReturnedAt` - Set once via `Return()`, then permanent

**Why?**
- A loan represents a historical transaction
- You don't "change" a loan, you perform actions on it
- `Renew()` extends due date AND increments renewal count atomically
- `Return()` sets return date, calculates fee, updates status - all together

### BookAuthor

**Constructor**:
```csharp
public BookAuthor(int bookId, int authorId, int authorOrder, string? role = null)
```

**Behavior Methods**:
- `UpdateOrder(int newOrder)` - Changes author display order
- `UpdateRole(string? role)` - Updates author role

**Immutable After Creation**:
- `BookId` - The relationship is between specific book and author
- `AuthorId` - Changing this would mean a different relationship

## 🔒 Principles Demonstrated

### 1. Make Illegal States Unrepresentable

**Impossible States Prevented**:
- ❌ Loan with no MemberId/BookId
- ❌ Book with more AvailableCopies than TotalCopies
- ❌ Member with negative OutstandingFees
- ❌ Loan returned but still Active status

### 2. Tell, Don't Ask

**Before** (Ask):
```csharp
if (loan.RenewalCount < loan.MaxRenewalsAllowed && !loan.IsOverdue)
{
    loan.DueDate = loan.DueDate.AddDays(14);
    loan.RenewalCount++;
}
```

**After** (Tell):
```csharp
loan.Renew();  // Loan knows how to renew itself
```

### 3. Single Responsibility

Each behavior method has ONE job:
- `Loan.Return()` - Handles EVERYTHING about returning: set date, calculate fee, update status
- `Book.BorrowCopy()` - Decrements available copies with validation
- `Member.PayFee()` - Records payment with validation

### 4. Encapsulation

**Private setters** mean:
- Properties can only be changed through behavior methods
- Business rules enforced at the method level
- Impossible to create invalid state

## 🎓 Learning Points

### For Students

1. **Constructors enforce valid initial state**
   - Can't create a Book without ISBN and Title
   - Can't create a Loan without Member and Book

2. **Factory methods provide clear intent**
   - `Loan.Create()` is clearer than `new Loan(...)`
   - Encapsulates complex initialization logic

3. **Private setters prevent bypassing business rules**
   - Can't set `loan.DueDate` directly
   - Must use `loan.Renew()` which validates and updates related state

4. **Behavior methods encapsulate business logic**
   - `Return()` handles fee calculation, status update, return date - all together
   - Logic isn't scattered across application

5. **Computed properties derive state**
   - `IsOverdue` calculated from current time and DueDate
   - `FullName` derived from FirstName + LastName
   - No duplication or sync issues

### With ADO.NET

**How does ADO.NET work with private constructors?**

```csharp
// Infrastructure code (Repository layer)
private static Loan MaterializeLoan(SqlDataReader reader)
{
    // Create instance using Activator (bypasses constructor)
    var loan = (Loan)Activator.CreateInstance(typeof(Loan), nonPublic: true)!;

    // Set properties using reflection
    typeof(Loan).GetProperty("Id")!.SetValue(loan, reader.GetInt32(0));
    typeof(Loan).GetProperty("MemberId")!.SetValue(loan, reader.GetInt32(1));
    // ... set all properties from database

    return loan;
}
```

Or use `FormatterServices.GetUninitializedObject()` for better performance.

**Key Point**: Infrastructure code (repositories) can use reflection to materialize objects from database, while domain code uses constructors and methods.

## ✅ Checklist of Improvements

- [x] All entities have public constructors with required parameters
- [x] All entities have private parameterless constructors for ADO.NET
- [x] All properties use `private set` (no public setters)
- [x] Behavior methods for all state transitions
- [x] Factory methods where appropriate (`Loan.Create()`)
- [x] Validation in private setters (still enforced even from behavior methods)
- [x] Computed properties for derived state
- [x] `UpdatedAt` automatically set in behavior methods
- [x] Impossible states prevented

## 📚 Resources

- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [Effective Aggregate Design - Vaughn Vernon](https://www.dddcommunity.org/library/vernon_2011/)
- [Always Valid Domain Model](https://enterprisecraftsmanship.com/posts/always-valid-domain-model/)
- [Encapsulation and SOLID](https://enterprisecraftsmanship.com/posts/encapsulation-revisited/)

---

**These improvements transform our entities from simple data containers to true rich domain objects that encapsulate behavior and enforce business rules!**
