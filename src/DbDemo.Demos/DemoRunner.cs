using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient;

namespace DbDemo.Demos;

using DbDemo.Domain.Entities;
using DbDemo.Application.Repositories;
using DbDemo.Infrastructure.BulkOperations;

/// <summary>
/// Automated demo runner that executes pre-scripted scenarios
/// to demonstrate the library management system functionality
/// </summary>
public class DemoRunner
{
    private readonly IBookRepository _bookRepository;
    private readonly IAuthorRepository _authorRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IBookAuditRepository _bookAuditRepository;
    private readonly ISystemStatisticsRepository _systemStatisticsRepository;
    private readonly string _connectionString;
    private readonly bool _withDelays;

    public DemoRunner(
        IBookRepository bookRepository,
        IAuthorRepository authorRepository,
        IMemberRepository memberRepository,
        ILoanRepository loanRepository,
        ICategoryRepository categoryRepository,
        IBookAuditRepository bookAuditRepository,
        ISystemStatisticsRepository systemStatisticsRepository,
        string connectionString,
        bool withDelays = true)
    {
        _bookRepository = bookRepository;
        _authorRepository = authorRepository;
        _memberRepository = memberRepository;
        _loanRepository = loanRepository;
        _categoryRepository = categoryRepository;
        _bookAuditRepository = bookAuditRepository;
        _systemStatisticsRepository = systemStatisticsRepository;
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _withDelays = withDelays;
    }

    /// <summary>
    /// Runs all demo scenarios in sequence
    /// </summary>
    public async Task RunAllScenariosAsync()
    {
        PrintHeader("AUTOMATED DEMO - ALL SCENARIOS");

        await RunScenario1_BasicBookManagementAsync();
        await Delay(2000);

        await RunScenario2_AuthorManagementAsync();
        await Delay(2000);

        await RunScenario3_MemberManagementAsync();
        await Delay(2000);

        await RunScenario4_CompleteLoanWorkflowAsync();
        await Delay(2000);

        await RunScenario5_OverdueLoanScenarioAsync();
        await Delay(2000);

        await RunScenario6_LoanRenewalAsync();

        PrintSuccess("\n\n=== ALL DEMO SCENARIOS COMPLETED SUCCESSFULLY ===\n");
    }

    /// <summary>
    /// Scenario 1: Basic Book Management
    /// Demonstrates creating, searching, and updating books
    /// </summary>
    public async Task RunScenario1_BasicBookManagementAsync()
    {
        PrintHeader("SCENARIO 1: Basic Book Management");

        try
        {
            await WithTransactionAsync(async tx =>
            {
                // Step 1: Get or create a category
                PrintStep("Getting a category for our books...");
                var categories = await _categoryRepository.GetAllAsync(tx);
                var category = categories.FirstOrDefault();

                if (category == null)
                {
                    PrintInfo("No categories found. Creating one...");
                    category = new Category("Fiction", "Fictional literature and novels");
                    category = await _categoryRepository.CreateAsync(category, tx);
                    PrintSuccess($"Created category: {category.Name} (ID: {category.Id})");
                }
                else
                {
                    PrintSuccess($"Using existing category: {category.Name} (ID: {category.Id})");
                }

                await Delay();

                // Step 2: Create a new book
                PrintStep("Creating a new book...");
                var book1 = new Book("978-0-545-01022-1", "Harry Potter and the Philosopher's Stone", category.Id, 5);
                book1.UpdateDetails(
                    "Harry Potter and the Philosopher's Stone",
                    null,
                    "The first book in the Harry Potter series by J.K. Rowling",
                    "Bloomsbury Publishing"
                );
                book1.UpdatePublishingInfo(new DateTime(1997, 6, 26), 223, "English");
                book1.UpdateShelfLocation("A-12-3");

                book1 = await _bookRepository.CreateAsync(book1, tx);
                PrintSuccess($"Created book: '{book1.Title}' (ID: {book1.Id})");
                PrintInfo($"  ISBN: {book1.ISBN}");
                PrintInfo($"  Total Copies: {book1.TotalCopies}, Available: {book1.AvailableCopies}");

                await Delay();

                // Step 3: Create another book
                PrintStep("Creating another book...");
                var book2 = new Book("978-0-7475-3849-9", "Harry Potter and the Chamber of Secrets", category.Id, 3);
                book2.UpdateDetails(
                    "Harry Potter and the Chamber of Secrets",
                    null,
                    "The second book in the Harry Potter series",
                    "Bloomsbury Publishing"
                );
                book2.UpdatePublishingInfo(new DateTime(1998, 7, 2), 251, "English");
                book2.UpdateShelfLocation("A-12-4");

                book2 = await _bookRepository.CreateAsync(book2, tx);
                PrintSuccess($"Created book: '{book2.Title}' (ID: {book2.Id})");

                await Delay();

                // Step 4: Search for books
                PrintStep("Searching for books with 'Harry Potter'...");
                var searchResults = await _bookRepository.SearchByTitleAsync("Harry Potter", tx);
                PrintSuccess($"Found {searchResults.Count} book(s):");
                foreach (var book in searchResults)
                {
                    PrintInfo($"  - [{book.Id}] {book.Title} (ISBN: {book.ISBN})");
                }

                await Delay();

                // Step 5: Update a book
                PrintStep("Updating the first book's description...");
                book1.UpdateDetails(
                    book1.Title,
                    book1.Subtitle,
                    "The Boy Who Lived - A young wizard begins his magical education at Hogwarts School of Witchcraft and Wizardry.",
                    book1.Publisher
                );
                await _bookRepository.UpdateAsync(book1, tx);

                var updatedBook = await _bookRepository.GetByIdAsync(book1.Id, tx);
                PrintSuccess("Book updated successfully!");
                PrintInfo($"  New description: {updatedBook?.Description}");

                await Delay();

                // Step 6: Display book count
                PrintStep("Counting total books in library...");
                var totalBooks = await _bookRepository.GetCountAsync(false, tx);
                PrintSuccess($"Total books in library: {totalBooks}");
            });

            PrintScenarioComplete("Scenario 1");
        }
        catch (Exception ex)
        {
            PrintError($"Scenario 1 failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Scenario 2: Author Management
    /// Demonstrates creating, searching, and updating authors
    /// </summary>
    public async Task RunScenario2_AuthorManagementAsync()
    {
        PrintHeader("SCENARIO 2: Author Management");

        try
        {
            await WithTransactionAsync(async tx =>
            {
                // Step 1: Create an author
                PrintStep("Creating author J.K. Rowling...");
                var author1 = new Author("Joanne", "Rowling", "jk.rowling@example.com");
                author1.UpdateBiography(
                    "British author, best known for writing the Harry Potter fantasy series",
                    new DateTime(1965, 7, 31),
                    "British"
                );

                author1 = await _authorRepository.CreateAsync(author1, tx);
                PrintSuccess($"Created author: {author1.FullName} (ID: {author1.Id})");
                PrintInfo($"  Email: {author1.Email}");
                PrintInfo($"  Age: {author1.Age} years");
                PrintInfo($"  Nationality: {author1.Nationality}");

                await Delay();

                // Step 2: Create another author
                PrintStep("Creating author George R.R. Martin...");
                var author2 = new Author("George", "Martin", "grrm@example.com");
                author2.UpdateBiography(
                    "American novelist and short story writer, screenwriter, and television producer",
                    new DateTime(1948, 9, 20),
                    "American"
                );

                author2 = await _authorRepository.CreateAsync(author2, tx);
                PrintSuccess($"Created author: {author2.FullName} (ID: {author2.Id})");

                await Delay();

                // Step 3: Search for authors
                PrintStep("Searching for authors with 'Martin'...");
                var searchResults = await _authorRepository.SearchByNameAsync("Martin", tx);
                PrintSuccess($"Found {searchResults.Count} author(s):");
                foreach (var author in searchResults)
                {
                    PrintInfo($"  - [{author.Id}] {author.FullName} - {author.Nationality}");
                }

                await Delay();

                // Step 4: Update author information
                PrintStep("Updating author biography...");
                author1.UpdateBiography(
                    "British author, philanthropist, film producer, and screenwriter, best known for writing the Harry Potter fantasy series. The books have won multiple awards and sold more than 500 million copies.",
                    author1.DateOfBirth,
                    author1.Nationality
                );
                await _authorRepository.UpdateAsync(author1, tx);

                var updatedAuthor = await _authorRepository.GetByIdAsync(author1.Id, tx);
                PrintSuccess("Author updated successfully!");
                PrintInfo($"  Biography: {updatedAuthor?.Biography?.Substring(0, Math.Min(80, updatedAuthor.Biography.Length))}...");

                await Delay();

                // Step 5: Display author count
                PrintStep("Counting total authors...");
                var totalAuthors = await _authorRepository.GetCountAsync(tx);
                PrintSuccess($"Total authors in database: {totalAuthors}");
            });

            PrintScenarioComplete("Scenario 2");
        }
        catch (Exception ex)
        {
            PrintError($"Scenario 2 failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Scenario 3: Member Management
    /// Demonstrates creating, updating, and managing library members
    /// </summary>
    public async Task RunScenario3_MemberManagementAsync()
    {
        PrintHeader("SCENARIO 3: Member Management");

        try
        {
            await WithTransactionAsync(async tx =>
            {
                // Step 1: Register a new member
                PrintStep("Registering new member John Smith...");
                var member1 = new Member(
                    "MEM-2024-001",
                    "John",
                    "Smith",
                    "john.smith@example.com",
                    new DateTime(1990, 5, 15)
                );
                member1.UpdateContactInfo("john.smith@example.com", "+44-20-1234-5678", "123 London Street, London, UK");

                member1 = await _memberRepository.CreateAsync(member1, tx);
                PrintSuccess($"Registered member: {member1.FullName} (ID: {member1.Id})");
                PrintInfo($"  Membership Number: {member1.MembershipNumber}");
                PrintInfo($"  Email: {member1.Email}");
                PrintInfo($"  Age: {member1.Age} years");
                PrintInfo($"  Membership Valid: {(member1.IsMembershipValid ? "Yes" : "No")}");
                PrintInfo($"  Expires: {member1.MembershipExpiresAt:yyyy-MM-dd}");
                PrintInfo($"  Can Borrow Books: {(member1.CanBorrowBooks() ? "Yes" : "No")}");

                await Delay();

                // Step 2: Register another member
                PrintStep("Registering new member Emma Watson...");
                var member2 = new Member(
                    "MEM-2024-002",
                    "Emma",
                    "Watson",
                    "emma.watson@example.com",
                    new DateTime(1995, 8, 22)
                );
                member2.UpdateContactInfo("emma.watson@example.com", "+44-20-9876-5432", "456 Baker Street, London, UK");

                member2 = await _memberRepository.CreateAsync(member2, tx);
                PrintSuccess($"Registered member: {member2.FullName} (ID: {member2.Id})");

                await Delay();

                // Step 3: Update member contact info
                PrintStep("Updating member contact information...");
                member1.UpdateContactInfo("john.smith.new@example.com", "+44-20-1111-2222", "789 Oxford Street, London, UK");
                await _memberRepository.UpdateAsync(member1, tx);

                var updatedMember = await _memberRepository.GetByIdAsync(member1.Id, tx);
                PrintSuccess("Member contact info updated!");
                PrintInfo($"  New Email: {updatedMember?.Email}");
                PrintInfo($"  New Phone: {updatedMember?.PhoneNumber}");
                PrintInfo($"  New Address: {updatedMember?.Address}");

                await Delay();

                // Step 4: Extend membership
                PrintStep("Extending membership by 6 months...");
                var originalExpiry = member1.MembershipExpiresAt;
                member1.ExtendMembership(6);
                await _memberRepository.UpdateAsync(member1, tx);

                PrintSuccess("Membership extended!");
                PrintInfo($"  Old Expiry: {originalExpiry:yyyy-MM-dd}");
                PrintInfo($"  New Expiry: {member1.MembershipExpiresAt:yyyy-MM-dd}");

                await Delay();

                // Step 5: Display member statistics
                PrintStep("Displaying member statistics...");
                var totalMembers = await _memberRepository.GetCountAsync(activeOnly: false, tx);
                var activeMembers = await _memberRepository.GetCountAsync(activeOnly: true, tx);
                PrintSuccess($"Member Statistics:");
                PrintInfo($"  Total Members: {totalMembers}");
                PrintInfo($"  Active Members: {activeMembers}");
            });

            PrintScenarioComplete("Scenario 3");
        }
        catch (Exception ex)
        {
            PrintError($"Scenario 3 failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Scenario 4: Complete Loan Workflow (Happy Path)
    /// Demonstrates the full loan lifecycle from creation to return
    /// </summary>
    public async Task RunScenario4_CompleteLoanWorkflowAsync()
    {
        PrintHeader("SCENARIO 4: Complete Loan Workflow (Happy Path)");

        try
        {
            await WithTransactionAsync(async tx =>
            {
                // Step 1: Get or create a book
                PrintStep("Setting up: Getting a book...");
                var books = await _bookRepository.GetPagedAsync(1, 1, false, tx);
                var book = books.FirstOrDefault();

                if (book == null)
                {
                    var categories = await _categoryRepository.GetAllAsync(tx);
                    var category = categories.First();
                    book = new Book("978-0-316-76948-0", "The Catcher in the Rye", category.Id, 3);
                    book = await _bookRepository.CreateAsync(book, tx);
                }

                PrintSuccess($"Using book: '{book.Title}' (ID: {book.Id})");
                PrintInfo($"  Available Copies: {book.AvailableCopies}/{book.TotalCopies}");

                await Delay();

                // Step 2: Get or create a member
                PrintStep("Setting up: Getting a member...");
                var members = await _memberRepository.GetPagedAsync(1, 1, activeOnly: true, tx);
                var member = members.FirstOrDefault();

                if (member == null)
                {
                    member = new Member("MEM-DEMO-001", "Alice", "Johnson", "alice@example.com", new DateTime(1992, 3, 10));
                    member = await _memberRepository.CreateAsync(member, tx);
                }

                PrintSuccess($"Using member: {member.FullName} (ID: {member.Id})");
                PrintInfo($"  Membership Valid: {(member.IsMembershipValid ? "Yes" : "No")}");
                PrintInfo($"  Can Borrow: {(member.CanBorrowBooks() ? "Yes" : "No")}");

                await Delay();

                // Step 3: Check if member can borrow
                PrintStep("Checking if member can borrow books...");
                if (!member.CanBorrowBooks())
                {
                    PrintWarning("Member cannot borrow books (inactive or has outstanding fees)");
                    return;
                }
                PrintSuccess("Member is eligible to borrow books!");

                await Delay();

                // Step 4: Check book availability
                PrintStep("Checking book availability...");
                if (!book.IsAvailable)
                {
                    PrintWarning($"Book is not available. Available: {book.AvailableCopies}/{book.TotalCopies}");
                    return;
                }
                PrintSuccess($"Book is available! {book.AvailableCopies} copies available.");

                await Delay();

                // Step 5: Create the loan
                PrintStep("Creating loan...");
                var loan = Loan.Create(member.Id, book.Id);
                loan = await _loanRepository.CreateAsync(loan, tx);

                PrintSuccess($"Loan created successfully! (Loan ID: {loan.Id})");
                PrintInfo($"  Member: {member.FullName}");
                PrintInfo($"  Book: {book.Title}");
                PrintInfo($"  Borrowed: {loan.BorrowedAt:yyyy-MM-dd HH:mm}");
                PrintInfo($"  Due Date: {loan.DueDate:yyyy-MM-dd HH:mm}");
                PrintInfo($"  Status: {loan.Status}");

                await Delay();

                // Step 6: Update book availability
                PrintStep("Updating book availability...");
                book.BorrowCopy();
                await _bookRepository.UpdateAsync(book, tx);

                var updatedBook = await _bookRepository.GetByIdAsync(book.Id, tx);
                PrintSuccess("Book availability updated!");
                PrintInfo($"  Available Copies: {updatedBook?.AvailableCopies}/{updatedBook?.TotalCopies}");

                await Delay();

                // Step 7: Check active loans for member
                PrintStep("Checking member's active loans...");
                var activeLoans = await _loanRepository.GetActiveLoansByMemberIdAsync(member.Id, tx);
                PrintSuccess($"Member has {activeLoans.Count} active loan(s):");
                foreach (var activeLoan in activeLoans)
                {
                    PrintInfo($"  - Loan #{activeLoan.Id}: Book {activeLoan.BookId} - Due: {activeLoan.DueDate:yyyy-MM-dd}");
                }

                await Delay();

                // Step 8: Return the book
                PrintStep("Returning the book...");
                loan.Return();
                await _loanRepository.UpdateAsync(loan, tx);

                PrintSuccess("Book returned successfully!");
                PrintInfo($"  Returned At: {loan.ReturnedAt:yyyy-MM-dd HH:mm}");
                PrintInfo($"  Status: {loan.Status}");
                PrintInfo($"  Late Fee: £{loan.LateFee:F2}");

                await Delay();

                // Step 9: Update book availability after return
                PrintStep("Restoring book availability...");
                book.ReturnCopy();
                await _bookRepository.UpdateAsync(book, tx);

                updatedBook = await _bookRepository.GetByIdAsync(book.Id, tx);
                PrintSuccess("Book availability restored!");
                PrintInfo($"  Available Copies: {updatedBook?.AvailableCopies}/{updatedBook?.TotalCopies}");
            });

            PrintScenarioComplete("Scenario 4");
        }
        catch (Exception ex)
        {
            PrintError($"Scenario 4 failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Scenario 5: Overdue Loan Scenario
    /// Demonstrates handling overdue loans and late fees
    /// </summary>
    public async Task RunScenario5_OverdueLoanScenarioAsync()
    {
        PrintHeader("SCENARIO 5: Overdue Loan Scenario");

        try
        {
            await WithTransactionAsync(async tx =>
            {
                // Step 1: Setup - Get book and member
                PrintStep("Setting up test data...");
                var books = await _bookRepository.GetPagedAsync(1, 1, false, tx);
                var book = books.FirstOrDefault();

                if (book == null)
                {
                    PrintWarning("No books available for demo");
                    return;
                }

                var members = await _memberRepository.GetPagedAsync(1, 1, activeOnly: true, tx);
                var member = members.FirstOrDefault();

                if (member == null)
                {
                    PrintWarning("No active members available for demo");
                    return;
                }

                PrintSuccess($"Using book: {book.Title} and member: {member.FullName}");

                await Delay();

                // Step 2: Create a loan with past due date (simulating overdue)
                PrintStep("Creating a loan that's already overdue...");
                var loan = Loan.Create(member.Id, book.Id);
                loan = await _loanRepository.CreateAsync(loan, tx);

                // Note: In a real scenario, we'd need to manipulate the database directly to set a past due date
                // For demo purposes, we're showing what would happen
                PrintSuccess($"Loan created (ID: {loan.Id})");
                PrintInfo($"  Due Date: {loan.DueDate:yyyy-MM-dd}");
                PrintInfo($"  Current Status: {loan.Status}");

                await Delay();

                // Step 3: Check if loan is overdue
                PrintStep("Checking if loan is overdue...");
                var isOverdue = loan.IsOverdue;
                var daysOverdue = loan.DaysOverdue;

                if (isOverdue)
                {
                    PrintWarning($"Loan is OVERDUE by {daysOverdue} day(s)!");
                }
                else
                {
                    PrintSuccess("Loan is not yet overdue.");
                    PrintInfo($"  Note: For a real overdue demo, the due date would need to be in the past.");
                }

                await Delay();

                // Step 4: Calculate late fee
                PrintStep("Calculating late fee...");
                var lateFee = loan.CalculateLateFee();

                if (lateFee > 0)
                {
                    PrintWarning($"Late Fee: £{lateFee:F2} (£0.50 per day)");
                }
                else
                {
                    PrintSuccess("No late fee (loan not overdue)");
                }

                await Delay();

                // Step 5: Return the book (will calculate final late fee if overdue)
                PrintStep("Returning the book...");
                loan.Return();
                await _loanRepository.UpdateAsync(loan, tx);

                PrintSuccess("Book returned!");
                PrintInfo($"  Return Status: {loan.Status}");
                PrintInfo($"  Final Late Fee: £{loan.LateFee:F2}");
                PrintInfo($"  Fee Paid: {(loan.IsFeePaid ? "Yes" : "No")}");

                await Delay();

                // Step 6: Pay the late fee if there is one
                if (loan.LateFee.HasValue && loan.LateFee > 0 && !loan.IsFeePaid)
                {
                    PrintStep("Processing late fee payment...");

                    // Add fee to member account
                    member.AddFee(loan.LateFee.Value);
                    await _memberRepository.UpdateAsync(member, tx);

                    // Mark fee as paid on loan
                    loan.PayLateFee();
                    await _loanRepository.UpdateAsync(loan, tx);

                    PrintSuccess("Late fee processed!");
                    PrintInfo($"  Amount: £{loan.LateFee:F2}");
                    PrintInfo($"  Member Outstanding Fees: £{member.OutstandingFees:F2}");

                    await Delay();

                    // Pay off the fee
                    PrintStep("Member paying the late fee...");
                    member.PayFee(member.OutstandingFees);
                    await _memberRepository.UpdateAsync(member, tx);

                    PrintSuccess("Late fee paid!");
                    PrintInfo($"  Member Outstanding Fees: £{member.OutstandingFees:F2}");
                }
            });

            PrintScenarioComplete("Scenario 5");
        }
        catch (Exception ex)
        {
            PrintError($"Scenario 5 failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Scenario 6: Loan Renewal
    /// Demonstrates renewing loans and renewal limits
    /// </summary>
    public async Task RunScenario6_LoanRenewalAsync()
    {
        PrintHeader("SCENARIO 6: Loan Renewal");

        try
        {
            await WithTransactionAsync(async tx =>
            {
                // Step 1: Setup
                PrintStep("Setting up test data...");
                var books = await _bookRepository.GetPagedAsync(1, 1, false, tx);
                var book = books.FirstOrDefault();

                if (book == null)
                {
                    PrintWarning("No books available for demo");
                    return;
                }

                var members = await _memberRepository.GetPagedAsync(1, 1, activeOnly: true, tx);
                var member = members.FirstOrDefault();

                if (member == null)
                {
                    PrintWarning("No active members available for demo");
                    return;
                }

                PrintSuccess($"Using book: {book.Title} and member: {member.FullName}");

                await Delay();

                // Step 2: Create a loan
                PrintStep("Creating a new loan...");
                var loan = Loan.Create(member.Id, book.Id);
                loan = await _loanRepository.CreateAsync(loan, tx);

                var originalDueDate = loan.DueDate;
                PrintSuccess($"Loan created (ID: {loan.Id})");
                PrintInfo($"  Due Date: {loan.DueDate:yyyy-MM-dd}");
                PrintInfo($"  Renewals: {loan.RenewalCount}/{loan.MaxRenewalsAllowed}");
                PrintInfo($"  Can Be Renewed: {(loan.CanBeRenewed ? "Yes" : "No")}");

                await Delay();

                // Step 3: First renewal
                PrintStep("Renewing loan (1st renewal)...");

                if (loan.CanBeRenewed)
                {
                    loan.Renew();
                    await _loanRepository.UpdateAsync(loan, tx);

                    PrintSuccess("Loan renewed successfully!");
                    PrintInfo($"  Old Due Date: {originalDueDate:yyyy-MM-dd}");
                    PrintInfo($"  New Due Date: {loan.DueDate:yyyy-MM-dd}");
                    PrintInfo($"  Renewals: {loan.RenewalCount}/{loan.MaxRenewalsAllowed}");
                    PrintInfo($"  Can Be Renewed Again: {(loan.CanBeRenewed ? "Yes" : "No")}");
                }
                else
                {
                    PrintWarning("Loan cannot be renewed!");
                }

                await Delay();

                // Step 4: Second renewal
                PrintStep("Attempting 2nd renewal...");

                if (loan.CanBeRenewed)
                {
                    var secondDueDate = loan.DueDate;
                    loan.Renew();
                    await _loanRepository.UpdateAsync(loan, tx);

                    PrintSuccess("Loan renewed again!");
                    PrintInfo($"  Old Due Date: {secondDueDate:yyyy-MM-dd}");
                    PrintInfo($"  New Due Date: {loan.DueDate:yyyy-MM-dd}");
                    PrintInfo($"  Renewals: {loan.RenewalCount}/{loan.MaxRenewalsAllowed}");
                    PrintInfo($"  Can Be Renewed Again: {(loan.CanBeRenewed ? "Yes" : "No")}");
                }
                else
                {
                    PrintWarning("Loan cannot be renewed!");
                }

                await Delay();

                // Step 5: Try to exceed renewal limit
                PrintStep("Attempting 3rd renewal (should fail - exceeds limit)...");

                if (loan.CanBeRenewed)
                {
                    PrintWarning("Unexpected: Loan can still be renewed!");
                }
                else
                {
                    PrintSuccess("Renewal correctly blocked!");
                    PrintInfo($"  Reason: Maximum renewals ({loan.MaxRenewalsAllowed}) reached");
                    PrintInfo($"  Current Renewals: {loan.RenewalCount}");

                    try
                    {
                        loan.Renew();
                        PrintWarning("ERROR: Renewal should have thrown an exception!");
                    }
                    catch (InvalidOperationException)
                    {
                        PrintSuccess("Exception correctly thrown when attempting to exceed renewal limit");
                    }
                }

                await Delay();

                // Step 6: Show loan statistics
                PrintStep("Displaying loan statistics...");
                var totalLoans = await _loanRepository.GetCountAsync(null, tx);
                var activeLoansCount = await _loanRepository.GetCountAsync(LoanStatus.Active, tx);

                PrintSuccess("Loan Statistics:");
                PrintInfo($"  Total Loans: {totalLoans}");
                PrintInfo($"  Active Loans: {activeLoansCount}");
            });

            PrintScenarioComplete("Scenario 6");
        }
        catch (Exception ex)
        {
            PrintError($"Scenario 6 failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Scenario 10: Book Audit Trail
    /// Demonstrates database trigger-based audit logging for book changes
    /// </summary>
    public async Task RunScenario10_BookAuditTrailAsync()
    {
        PrintHeader("SCENARIO 10: Book Audit Trail");

        try
        {
            await WithTransactionAsync(async tx =>
            {
                // Step 1: Get or create a category
                PrintStep("Preparing test data (category)...");
                var categories = await _categoryRepository.GetAllAsync(tx);
                var category = categories.FirstOrDefault();

                if (category == null)
                {
                    category = new Category("Fiction", "Fictional literature and novels");
                    category = await _categoryRepository.CreateAsync(category, tx);
                    PrintSuccess($"Created category: {category.Name} (ID: {category.Id})");
                }
                else
                {
                    PrintSuccess($"Using existing category: {category.Name} (ID: {category.Id})");
                }

                await Delay();

                // Step 2: Create a new book (INSERT trigger)
                PrintStep("Creating a new book (INSERT will be audited)...");
                var book = new Book("978-1-234-56789-0", "Audit Trail Demo Book", category.Id, 5);
                book.UpdateDetails(
                    title: "Audit Trail Demo Book",
                    subtitle: "A Book to Test Database Triggers",
                    description: "This book is created to demonstrate the automatic audit trail functionality.",
                    publisher: "Demo Press"
                );
                book.UpdatePublishingInfo(
                    publishedDate: new DateTime(2024, 1, 1),
                    pageCount: 350,
                    language: "English"
                );
                book = await _bookRepository.CreateAsync(book, tx);

                PrintSuccess($"Book created! (ID: {book.Id})");
                PrintInfo($"  ISBN: {book.ISBN}");
                PrintInfo($"  Title: {book.Title}");
                PrintInfo($"  Available Copies: {book.AvailableCopies}");

                await Delay();

                // Step 3: View audit trail after INSERT
                PrintStep("Viewing audit trail (should show INSERT operation)...");
                var auditHistory = await _bookAuditRepository.GetAuditHistoryAsync(book.Id, tx);

                PrintSuccess($"Found {auditHistory.Count} audit record(s):");
                foreach (var audit in auditHistory)
                {
                    PrintInfo($"  {audit}");
                }

                await Delay();

                // Step 4: Update the book (UPDATE trigger)
                PrintStep("Updating book title and copies (UPDATE will be audited)...");
                book.UpdateDetails(
                    title: book.Title,
                    subtitle: "A Book to Test Database Triggers - UPDATED",
                    description: book.Description,
                    publisher: book.Publisher
                );
                book.AddCopies(3);  // Total copies: 5 + 3 = 8
                await _bookRepository.UpdateAsync(book, tx);

                PrintSuccess("Book updated!");
                PrintInfo($"  New Subtitle: {book.Subtitle}");
                PrintInfo($"  New Total Copies: {book.TotalCopies}");

                await Delay();

                // Step 5: View audit trail after UPDATE
                PrintStep("Viewing updated audit trail (should show INSERT + UPDATE)...");
                auditHistory = await _bookAuditRepository.GetAuditHistoryAsync(book.Id, tx);

                PrintSuccess($"Found {auditHistory.Count} audit record(s):");
                foreach (var audit in auditHistory)
                {
                    PrintInfo($"  {audit}");
                }

                await Delay();

                // Step 6: Borrow a copy (another UPDATE)
                PrintStep("Borrowing a copy (will trigger another UPDATE audit)...");
                book.BorrowCopy();
                await _bookRepository.UpdateAsync(book, tx);

                PrintSuccess("Copy borrowed!");
                PrintInfo($"  Available Copies: {book.AvailableCopies}/{book.TotalCopies}");

                await Delay();

                // Step 7: View complete audit trail
                PrintStep("Viewing complete audit trail...");
                auditHistory = await _bookAuditRepository.GetAuditHistoryAsync(book.Id, tx);

                PrintSuccess($"Complete audit trail ({auditHistory.Count} records):");
                for (int i = 0; i < auditHistory.Count; i++)
                {
                    var audit = auditHistory[i];
                    PrintInfo($"\n  Record #{i + 1}:");
                    PrintInfo($"    Action: {audit.Action}");
                    PrintInfo($"    Changed At: {audit.ChangedAt:yyyy-MM-dd HH:mm:ss} UTC");
                    PrintInfo($"    Changed By: {audit.ChangedBy}");

                    if (audit.Action == "UPDATE")
                    {
                        if (audit.OldTitle != audit.NewTitle)
                            PrintInfo($"    Title: '{audit.OldTitle}' → '{audit.NewTitle}'");
                        if (audit.OldAvailableCopies != audit.NewAvailableCopies)
                            PrintInfo($"    Available: {audit.OldAvailableCopies} → {audit.NewAvailableCopies}");
                        if (audit.OldTotalCopies != audit.NewTotalCopies)
                            PrintInfo($"    Total: {audit.OldTotalCopies} → {audit.NewTotalCopies}");
                    }
                }

                await Delay();

                // Step 8: Demonstrate querying all audit records
                PrintStep("Querying recent audit activity across all books...");
                var recentAudits = await _bookAuditRepository.GetAllAuditRecordsAsync(
                    action: null,  // All actions
                    limit: 10,
                    transaction: tx
                );

                PrintSuccess($"Recent audit activity ({recentAudits.Count} records):");
                foreach (var audit in recentAudits.Take(5))
                {
                    PrintInfo($"  BookId {audit.BookId}: {audit.GetChangeDescription()}");
                }

                if (recentAudits.Count > 5)
                {
                    PrintInfo($"  ... and {recentAudits.Count - 5} more");
                }

                await Delay();

                // Step 9: Delete the test book (DELETE trigger)
                PrintStep("Deleting the test book (DELETE will be audited)...");
                await _bookRepository.DeleteAsync(book.Id, tx);

                PrintSuccess("Book deleted (soft delete - marked as IsDeleted = true)");

                await Delay();

                // Step 10: Final audit trail view
                PrintStep("Viewing final audit trail...");
                auditHistory = await _bookAuditRepository.GetAuditHistoryAsync(book.Id, tx);

                PrintSuccess($"Final audit trail ({auditHistory.Count} records):");
                PrintInfo($"  Total INSERT operations: {auditHistory.Count(a => a.Action == "INSERT")}");
                PrintInfo($"  Total UPDATE operations: {auditHistory.Count(a => a.Action == "UPDATE")}");
                PrintInfo($"  Total DELETE operations: {auditHistory.Count(a => a.Action == "DELETE")}");
            });

            PrintScenarioComplete("Scenario 10");
        }
        catch (Exception ex)
        {
            PrintError($"Scenario 10 failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Scenario 11: Overdue Loans Report
    /// Demonstrates calling a stored procedure with output parameters
    /// </summary>
    public async Task RunScenario11_OverdueLoansReportAsync()
    {
        PrintHeader("SCENARIO 11: Overdue Loans Report (Stored Procedure)");

        try
        {
            await WithTransactionAsync(async tx =>
            {
                // Step 1: Check current overdue loans
                PrintStep("Querying overdue loans using sp_GetOverdueLoans stored procedure...");

                var (overdueLoans, totalCount) = await _loanRepository.GetOverdueLoansReportAsync(
                    asOfDate: null,  // Use current UTC time
                    minDaysOverdue: 0,
                    transaction: tx
                );

                PrintSuccess($"Stored procedure executed successfully!");
                PrintInfo($"  Total overdue loans (from OUTPUT parameter): {totalCount}");
                PrintInfo($"  Loans returned in result set: {overdueLoans.Count}");

                await Delay();

                // Step 2: Display overdue loans if any exist
                if (overdueLoans.Count > 0)
                {
                    PrintStep("Displaying overdue loans (top 5)...");

                    foreach (var loan in overdueLoans.Take(5))
                    {
                        PrintWarning($"\n  {loan}");
                        PrintInfo($"    Member: {loan.MemberName} ({loan.MemberEmail})");
                        PrintInfo($"    Book: \"{loan.BookTitle}\" (ISBN: {loan.ISBN})");
                        PrintInfo($"    Due: {loan.DueDate:yyyy-MM-dd} | Borrowed: {loan.BorrowedAt:yyyy-MM-dd}");
                        PrintInfo($"    Status: {loan.Status} | Fee: £{loan.CalculatedLateFee:F2}");
                    }

                    if (overdueLoans.Count > 5)
                    {
                        PrintInfo($"\n  ... and {overdueLoans.Count - 5} more overdue loan(s)");
                    }
                }
                else
                {
                    PrintSuccess("No overdue loans found - all members are up to date!");
                }

                await Delay();

                // Step 3: Filter by minimum days overdue
                PrintStep("Filtering for loans overdue by 7+ days...");

                var (seriouslyOverdue, seriousCount) = await _loanRepository.GetOverdueLoansReportAsync(
                    asOfDate: null,
                    minDaysOverdue: 7,
                    transaction: tx
                );

                PrintSuccess($"Found {seriousCount} loan(s) overdue by 7+ days");

                if (seriouslyOverdue.Count > 0)
                {
                    foreach (var loan in seriouslyOverdue.Take(3))
                    {
                        PrintWarning($"  {loan.MemberName}: {loan.DaysOverdue} days overdue (£{loan.CalculatedLateFee:F2})");
                    }
                }

                await Delay();

                // Step 4: Demonstrate using custom AsOfDate
                PrintStep("Checking what would have been overdue 30 days ago...");

                var historicalDate = DateTime.UtcNow.AddDays(-30);
                var (historicalOverdue, historicalCount) = await _loanRepository.GetOverdueLoansReportAsync(
                    asOfDate: historicalDate,
                    minDaysOverdue: 0,
                    transaction: tx
                );

                PrintSuccess($"As of {historicalDate:yyyy-MM-dd}, there were {historicalCount} overdue loan(s)");

                await Delay();

                // Step 5: Show summary statistics
                PrintStep("Calculating overdue loan statistics...");

                if (overdueLoans.Count > 0)
                {
                    var totalFees = overdueLoans.Sum(l => l.CalculatedLateFee);
                    var avgDaysOverdue = overdueLoans.Average(l => l.DaysOverdue);
                    var mostOverdue = overdueLoans.OrderByDescending(l => l.DaysOverdue).First();

                    PrintSuccess("Overdue Loan Statistics:");
                    PrintInfo($"  Total Overdue: {totalCount}");
                    PrintInfo($"  Total Late Fees: £{totalFees:F2}");
                    PrintInfo($"  Average Days Overdue: {avgDaysOverdue:F1}");
                    PrintInfo($"  Most Overdue: {mostOverdue.DaysOverdue} days ({mostOverdue.MemberName} - \"{mostOverdue.BookTitle}\")");
                }
                else
                {
                    PrintInfo("  No overdue loans to analyze");
                }

                await Delay();

                // Step 6: Demonstrate output parameter usage
                PrintStep("Demonstrating OUTPUT parameter vs result set count...");

                var (allOverdue, outputTotal) = await _loanRepository.GetOverdueLoansReportAsync(
                    asOfDate: null,
                    minDaysOverdue: 0,
                    transaction: tx
                );

                PrintSuccess("Comparison:");
                PrintInfo($"  Result set Count property: {allOverdue.Count}");
                PrintInfo($"  OUTPUT parameter value: {outputTotal}");
                PrintInfo($"  Match: {(allOverdue.Count == outputTotal ? "✓ Yes" : "✗ No (BUG!)")}");

                PrintInfo("\nNote: OUTPUT parameters are useful for returning metadata without");
                PrintInfo("needing to process the entire result set (e.g., pagination total count).");

                await Delay();

                // Step 7: Demonstrate scalar function for late fee calculation
                PrintStep("Demonstrating fn_CalculateLateFee scalar function...");

                if (overdueLoans.Count > 0)
                {
                    var firstLoan = overdueLoans[0];

                    // Calculate late fee using scalar function
                    var calculatedFee = await _loanRepository.CalculateLateFeeAsync(firstLoan.LoanId, tx);

                    PrintSuccess($"Late fee calculation for Loan #{firstLoan.LoanId}:");
                    PrintInfo($"  Member: {firstLoan.MemberName}");
                    PrintInfo($"  Book: \"{firstLoan.BookTitle}\"");
                    PrintInfo($"  Days Overdue: {firstLoan.DaysOverdue}");
                    PrintInfo($"  Stored Procedure calculated: £{firstLoan.CalculatedLateFee:F2}");
                    PrintInfo($"  Scalar Function calculated:  £{calculatedFee:F2}");
                    PrintInfo($"  Match: {(firstLoan.CalculatedLateFee == calculatedFee ? "✓ Yes" : "✗ No (BUG!)")}");

                    PrintInfo("\nNote: Scalar functions can be called from C# or used in SQL queries.");
                    PrintInfo("      Stored procedures are better for complex multi-table operations.");
                }
                else
                {
                    PrintInfo("No overdue loans to demonstrate scalar function calculation.");
                }
            });

            PrintScenarioComplete("Scenario 11");
        }
        catch (Exception ex)
        {
            PrintError($"Scenario 11 failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Scenario 12: Statistics & Analytics
    /// Demonstrates time-series analytics with SQL Server advanced features:
    /// - Moving averages and window functions
    /// - Percentile analysis (P50, P95, P99)
    /// - Anomaly detection using Z-scores
    /// - Trend analysis with growth rates
    /// </summary>
    public async Task RunScenario12_StatisticsAnalyticsAsync()
    {
        PrintHeader("SCENARIO 12: Statistics & Analytics");

        try
        {
            await WithTransactionAsync(async tx =>
            {
                // Step 1: Show recent statistics data
                PrintStep("Retrieving recent statistics data...");
                var recentStats = await _systemStatisticsRepository.GetRecentAsync(10, tx);

                if (recentStats.Count > 0)
                {
                    PrintSuccess($"Found {recentStats.Count} recent statistics records:");
                    foreach (var stat in recentStats.Take(5))
                    {
                        PrintInfo($"  {stat.GetSummary()}");
                    }
                }
                else
                {
                    PrintWarning("No statistics data found. The seed data might not have been generated.");
                }

                await Delay();

                // Step 2: Hourly aggregations
                PrintStep("Analyzing hourly aggregated statistics...");
                var hourlyStats = await _systemStatisticsRepository.GetHourlyStatisticsAsync(tx);

                if (hourlyStats.Count > 0)
                {
                    PrintSuccess($"Found {hourlyStats.Count} hourly aggregations:");
                    PrintInfo("\n  Latest 5 hours:");
                    PrintInfo($"  {"Hour",-20} {"Samples",-10} {"Avg Loans",-12} {"Avg CPU%",-12} {"Avg Mem%",-12}");
                    PrintInfo($"  {new string('-', 70)}");

                    foreach (var stat in hourlyStats.Take(5))
                    {
                        PrintInfo($"  {stat.HourBucket:yyyy-MM-dd HH:mm}   {stat.SampleCount,-10} {stat.AvgActiveLoans,-12:F1} {stat.AvgCPUUsage,-12:F1} {stat.AvgMemoryUsage,-12:F1}");
                    }
                }

                await Delay();

                // Step 3: Daily aggregations
                PrintStep("Analyzing daily aggregated statistics...");
                var dailyStats = await _systemStatisticsRepository.GetDailyStatisticsAsync(tx);

                if (dailyStats.Count > 0)
                {
                    PrintSuccess($"Found {dailyStats.Count} daily aggregations:");
                    PrintInfo("\n  Latest 7 days:");
                    PrintInfo($"  {"Date",-12} {"Samples",-10} {"New Loans",-12} {"Avg CPU%",-12} {"StdDev CPU",-12}");
                    PrintInfo($"  {new string('-', 65)}");

                    foreach (var stat in dailyStats.Take(7))
                    {
                        PrintInfo($"  {stat.DayDate:yyyy-MM-dd}   {stat.SampleCount,-10} {stat.TotalNewLoans,-12} {stat.AvgCPUUsage,-12:F1} {stat.StdDevCPUUsage,-12:F2}");
                    }
                }

                await Delay();

                // Step 4: Moving averages with window functions
                PrintStep("Calculating 7-period moving averages...");
                var startDate = DateTime.UtcNow.AddDays(-7);
                var endDate = DateTime.UtcNow;
                var movingAvgs = await _systemStatisticsRepository.GetMovingAveragesAsync(startDate, endDate, 7, tx);

                if (movingAvgs.Count > 0)
                {
                    PrintSuccess($"Calculated moving averages for {movingAvgs.Count} data points:");
                    PrintInfo("\n  Sample (showing every 360th record ~ hourly for last 6 hours):");
                    PrintInfo($"  {"Time",-20} {"Actual Loans",-15} {"7-Period MA",-15} {"Actual CPU%",-12} {"7-Period MA",-12}");
                    PrintInfo($"  {new string('-', 80)}");

                    var sample = movingAvgs.Where((x, i) => i % 360 == 0).Take(6);
                    foreach (var ma in sample)
                    {
                        PrintInfo($"  {ma.RecordedAt:yyyy-MM-dd HH:mm}   {ma.ActiveLoansCount,-15} {ma.MovingAvgActiveLoans,-15:F1} {ma.CPUUsagePercent,-12:F1} {ma.MovingAvgCPU,-12:F1}");
                    }

                    PrintInfo("\n  Note: Moving averages smooth out short-term fluctuations and highlight trends.");
                }

                await Delay();

                // Step 5: Percentile analysis
                PrintStep("Calculating percentile distributions (P50, P95, P99)...");
                var analysisStart = DateTime.UtcNow.AddDays(-30);
                var analysisEnd = DateTime.UtcNow;
                var percentiles = await _systemStatisticsRepository.GetPercentilesAsync(analysisStart, analysisEnd, tx);

                if (percentiles.Count > 0)
                {
                    PrintSuccess($"Percentile analysis for {percentiles.Count} metrics:");
                    PrintInfo($"\n  {"Metric",-25} {"Min",-10} {"P50",-10} {"P95",-10} {"P99",-10} {"Max",-10}");
                    PrintInfo($"  {new string('-', 75)}");

                    foreach (var p in percentiles)
                    {
                        PrintInfo($"  {p.MetricName,-25} {p.MinValue,-10:F1} {p.P50_Median,-10:F1} {p.P95,-10:F1} {p.P99,-10:F1} {p.MaxValue,-10:F1}");
                    }

                    PrintInfo("\n  Note: P95 and P99 are useful for SLA monitoring and capacity planning.");
                }

                await Delay();

                // Step 6: Anomaly detection
                PrintStep("Detecting anomalies using Z-score method (2 standard deviations)...");
                var anomalies = await _systemStatisticsRepository.DetectAnomaliesAsync(analysisStart, analysisEnd, 2.0, tx);

                if (anomalies.Count > 0)
                {
                    PrintSuccess($"Detected {anomalies.Count} anomalous data points:");
                    PrintInfo("\n  Latest 5 anomalies:");
                    PrintInfo($"  {"Time",-20} {"CPU%",-10} {"Anomaly",-10} {"Z-Score",-10} {"Memory%",-10} {"Anomaly",-10}");
                    PrintInfo($"  {new string('-', 70)}");

                    foreach (var anomaly in anomalies.Take(5))
                    {
                        var cpuColor = anomaly.CPUAnomaly == "HIGH" ? ConsoleColor.Red : ConsoleColor.Yellow;
                        var memColor = anomaly.MemoryAnomaly == "HIGH" ? ConsoleColor.Red : ConsoleColor.Yellow;

                        Console.Write($"  {anomaly.RecordedAt:yyyy-MM-dd HH:mm}   ");
                        Console.Write($"{anomaly.CPUUsagePercent,-10:F1} ");

                        Console.ForegroundColor = cpuColor;
                        Console.Write($"{anomaly.CPUAnomaly ?? "---",-10} ");
                        Console.ResetColor();

                        Console.Write($"{anomaly.CPUZScore,-10:F2} {anomaly.MemoryUsagePercent,-10:F1} ");

                        Console.ForegroundColor = memColor;
                        Console.Write($"{anomaly.MemoryAnomaly ?? "---",-10}");
                        Console.ResetColor();

                        Console.WriteLine();
                    }

                    PrintInfo("\n  Note: Anomalies are values >2σ from the mean. Useful for alerting.");
                }
                else
                {
                    PrintInfo("No anomalies detected in the dataset.");
                }

                await Delay();

                // Step 7: Trend analysis
                PrintStep("Performing trend analysis with growth rates and rankings...");
                var trends = await _systemStatisticsRepository.GetTrendAnalysisAsync(analysisStart, analysisEnd, tx);

                if (trends.Count > 0)
                {
                    PrintSuccess($"Trend analysis for {trends.Count} days:");
                    PrintInfo("\n  Latest 7 days:");
                    PrintInfo($"  {"Date",-12} {"Avg Loans",-12} {"DoD Change",-12} {"DoD %",-10} {"7-Day MA",-12} {"Rank",-6}");
                    PrintInfo($"  {new string('-', 70)}");

                    foreach (var trend in trends.Take(7))
                    {
                        var changeSymbol = trend.DayOverDayLoansChange >= 0 ? "+" : "";
                        PrintInfo($"  {trend.DayDate:yyyy-MM-dd}   {trend.AvgActiveLoans,-12:F1} {changeSymbol}{trend.DayOverDayLoansChange,-12:F1} {trend.DayOverDayLoansChangePercent,-10:F1}% {trend.SevenDayAvgLoans,-12:F1} #{trend.RankByActiveLoans,-5}");
                    }

                    PrintInfo("\n  Note: Growth rates help identify trends; rankings identify peak days.");
                }

                await Delay();

                // Summary
                PrintStep("Analytics demonstration complete!");
                PrintInfo("\nThis scenario demonstrated:");
                PrintInfo("  ✓ Time-window aggregations (hourly, daily)");
                PrintInfo("  ✓ Window functions (OVER, PARTITION BY) for moving averages");
                PrintInfo("  ✓ Statistical functions (PERCENTILE_CONT, STDEV)");
                PrintInfo("  ✓ Anomaly detection using Z-score method");
                PrintInfo("  ✓ Trend analysis with growth rates and rankings");
                PrintInfo("\nThese patterns are essential for:");
                PrintInfo("  • Performance monitoring and capacity planning");
                PrintInfo("  • SLA tracking (P95, P99 response times)");
                PrintInfo("  • Alerting on unusual system behavior");
                PrintInfo("  • Business intelligence and forecasting");
            });

            PrintScenarioComplete("Scenario 12");
        }
        catch (Exception ex)
        {
            PrintError($"Scenario 12 failed: {ex.Message}");
            throw;
        }
    }

    #region Helper Methods

    /// <summary>
    /// Executes a repository operation within a transaction
    /// </summary>
    private async Task<T> WithTransactionAsync<T>(Func<SqlTransaction, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(transaction);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Executes a repository operation within a transaction (no return value)
    /// </summary>
    private async Task WithTransactionAsync(Func<SqlTransaction, Task> operation, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await operation(transaction);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task Delay(int milliseconds = 1000)
    {
        if (_withDelays)
        {
            await Task.Delay(milliseconds);
        }
    }

    private void PrintHeader(string title)
    {
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════");
        Console.WriteLine($"  {title}");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    private void PrintStep(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n▶ {message}");
        Console.ResetColor();
    }

    private void PrintSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ {message}");
        Console.ResetColor();
    }

    private void PrintInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"  {message}");
        Console.ResetColor();
    }

    private void PrintWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠ {message}");
        Console.ResetColor();
    }

    private void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ {message}");
        Console.ResetColor();
    }

    private void PrintScenarioComplete(string scenarioName)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"═══════════════════════════════════════════════════════════════════════════");
        Console.WriteLine($"  ✓ {scenarioName} COMPLETED SUCCESSFULLY");
        Console.WriteLine($"═══════════════════════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();
    }

    #endregion
}
