using DbDemo.ConsoleApp.Infrastructure.Repositories;
using DbDemo.ConsoleApp.Models;

namespace DbDemo.ConsoleApp.Demos;

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
    private readonly bool _withDelays;

    public DemoRunner(
        IBookRepository bookRepository,
        IAuthorRepository authorRepository,
        IMemberRepository memberRepository,
        ILoanRepository loanRepository,
        ICategoryRepository categoryRepository,
        bool withDelays = true)
    {
        _bookRepository = bookRepository;
        _authorRepository = authorRepository;
        _memberRepository = memberRepository;
        _loanRepository = loanRepository;
        _categoryRepository = categoryRepository;
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
            // Step 1: Get or create a category
            PrintStep("Getting a category for our books...");
            var categories = await _categoryRepository.GetAllAsync();
            var category = categories.FirstOrDefault();

            if (category == null)
            {
                PrintInfo("No categories found. Creating one...");
                category = new Category("Fiction", "Fictional literature and novels");
                category = await _categoryRepository.CreateAsync(category);
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

            book1 = await _bookRepository.CreateAsync(book1);
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

            book2 = await _bookRepository.CreateAsync(book2);
            PrintSuccess($"Created book: '{book2.Title}' (ID: {book2.Id})");

            await Delay();

            // Step 4: Search for books
            PrintStep("Searching for books with 'Harry Potter'...");
            var searchResults = await _bookRepository.SearchByTitleAsync("Harry Potter");
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
            await _bookRepository.UpdateAsync(book1);

            var updatedBook = await _bookRepository.GetByIdAsync(book1.Id);
            PrintSuccess("Book updated successfully!");
            PrintInfo($"  New description: {updatedBook?.Description}");

            await Delay();

            // Step 6: Display book count
            PrintStep("Counting total books in library...");
            var totalBooks = await _bookRepository.GetCountAsync();
            PrintSuccess($"Total books in library: {totalBooks}");

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
            // Step 1: Create an author
            PrintStep("Creating author J.K. Rowling...");
            var author1 = new Author("Joanne", "Rowling", "jk.rowling@example.com");
            author1.UpdateBiography(
                "British author, best known for writing the Harry Potter fantasy series",
                new DateTime(1965, 7, 31),
                "British"
            );

            author1 = await _authorRepository.CreateAsync(author1);
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

            author2 = await _authorRepository.CreateAsync(author2);
            PrintSuccess($"Created author: {author2.FullName} (ID: {author2.Id})");

            await Delay();

            // Step 3: Search for authors
            PrintStep("Searching for authors with 'Martin'...");
            var searchResults = await _authorRepository.SearchByNameAsync("Martin");
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
            await _authorRepository.UpdateAsync(author1);

            var updatedAuthor = await _authorRepository.GetByIdAsync(author1.Id);
            PrintSuccess("Author updated successfully!");
            PrintInfo($"  Biography: {updatedAuthor?.Biography?.Substring(0, Math.Min(80, updatedAuthor.Biography.Length))}...");

            await Delay();

            // Step 5: Display author count
            PrintStep("Counting total authors...");
            var totalAuthors = await _authorRepository.GetCountAsync();
            PrintSuccess($"Total authors in database: {totalAuthors}");

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

            member1 = await _memberRepository.CreateAsync(member1);
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

            member2 = await _memberRepository.CreateAsync(member2);
            PrintSuccess($"Registered member: {member2.FullName} (ID: {member2.Id})");

            await Delay();

            // Step 3: Update member contact info
            PrintStep("Updating member contact information...");
            member1.UpdateContactInfo("john.smith.new@example.com", "+44-20-1111-2222", "789 Oxford Street, London, UK");
            await _memberRepository.UpdateAsync(member1);

            var updatedMember = await _memberRepository.GetByIdAsync(member1.Id);
            PrintSuccess("Member contact info updated!");
            PrintInfo($"  New Email: {updatedMember?.Email}");
            PrintInfo($"  New Phone: {updatedMember?.PhoneNumber}");
            PrintInfo($"  New Address: {updatedMember?.Address}");

            await Delay();

            // Step 4: Extend membership
            PrintStep("Extending membership by 6 months...");
            var originalExpiry = member1.MembershipExpiresAt;
            member1.ExtendMembership(6);
            await _memberRepository.UpdateAsync(member1);

            PrintSuccess("Membership extended!");
            PrintInfo($"  Old Expiry: {originalExpiry:yyyy-MM-dd}");
            PrintInfo($"  New Expiry: {member1.MembershipExpiresAt:yyyy-MM-dd}");

            await Delay();

            // Step 5: Display member statistics
            PrintStep("Displaying member statistics...");
            var totalMembers = await _memberRepository.GetCountAsync(activeOnly: false);
            var activeMembers = await _memberRepository.GetCountAsync(activeOnly: true);
            PrintSuccess($"Member Statistics:");
            PrintInfo($"  Total Members: {totalMembers}");
            PrintInfo($"  Active Members: {activeMembers}");

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
            // Step 1: Get or create a book
            PrintStep("Setting up: Getting a book...");
            var books = await _bookRepository.GetPagedAsync(1, 1);
            var book = books.FirstOrDefault();

            if (book == null)
            {
                var categories = await _categoryRepository.GetAllAsync();
                var category = categories.First();
                book = new Book("978-0-316-76948-0", "The Catcher in the Rye", category.Id, 3);
                book = await _bookRepository.CreateAsync(book);
            }

            PrintSuccess($"Using book: '{book.Title}' (ID: {book.Id})");
            PrintInfo($"  Available Copies: {book.AvailableCopies}/{book.TotalCopies}");

            await Delay();

            // Step 2: Get or create a member
            PrintStep("Setting up: Getting a member...");
            var members = await _memberRepository.GetPagedAsync(1, 1, activeOnly: true);
            var member = members.FirstOrDefault();

            if (member == null)
            {
                member = new Member("MEM-DEMO-001", "Alice", "Johnson", "alice@example.com", new DateTime(1992, 3, 10));
                member = await _memberRepository.CreateAsync(member);
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
            loan = await _loanRepository.CreateAsync(loan);

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
            await _bookRepository.UpdateAsync(book);

            var updatedBook = await _bookRepository.GetByIdAsync(book.Id);
            PrintSuccess("Book availability updated!");
            PrintInfo($"  Available Copies: {updatedBook?.AvailableCopies}/{updatedBook?.TotalCopies}");

            await Delay();

            // Step 7: Check active loans for member
            PrintStep("Checking member's active loans...");
            var activeLoans = await _loanRepository.GetActiveLoansByMemberIdAsync(member.Id);
            PrintSuccess($"Member has {activeLoans.Count} active loan(s):");
            foreach (var activeLoan in activeLoans)
            {
                PrintInfo($"  - Loan #{activeLoan.Id}: Book {activeLoan.BookId} - Due: {activeLoan.DueDate:yyyy-MM-dd}");
            }

            await Delay();

            // Step 8: Return the book
            PrintStep("Returning the book...");
            loan.Return();
            await _loanRepository.UpdateAsync(loan);

            PrintSuccess("Book returned successfully!");
            PrintInfo($"  Returned At: {loan.ReturnedAt:yyyy-MM-dd HH:mm}");
            PrintInfo($"  Status: {loan.Status}");
            PrintInfo($"  Late Fee: £{loan.LateFee:F2}");

            await Delay();

            // Step 9: Update book availability after return
            PrintStep("Restoring book availability...");
            book.ReturnCopy();
            await _bookRepository.UpdateAsync(book);

            updatedBook = await _bookRepository.GetByIdAsync(book.Id);
            PrintSuccess("Book availability restored!");
            PrintInfo($"  Available Copies: {updatedBook?.AvailableCopies}/{updatedBook?.TotalCopies}");

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
            // Step 1: Setup - Get book and member
            PrintStep("Setting up test data...");
            var books = await _bookRepository.GetPagedAsync(1, 1);
            var book = books.FirstOrDefault();

            if (book == null)
            {
                PrintWarning("No books available for demo");
                return;
            }

            var members = await _memberRepository.GetPagedAsync(1, 1, activeOnly: true);
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
            loan = await _loanRepository.CreateAsync(loan);

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
            await _loanRepository.UpdateAsync(loan);

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
                await _memberRepository.UpdateAsync(member);

                // Mark fee as paid on loan
                loan.PayLateFee();
                await _loanRepository.UpdateAsync(loan);

                PrintSuccess("Late fee processed!");
                PrintInfo($"  Amount: £{loan.LateFee:F2}");
                PrintInfo($"  Member Outstanding Fees: £{member.OutstandingFees:F2}");

                await Delay();

                // Pay off the fee
                PrintStep("Member paying the late fee...");
                member.PayFee(member.OutstandingFees);
                await _memberRepository.UpdateAsync(member);

                PrintSuccess("Late fee paid!");
                PrintInfo($"  Member Outstanding Fees: £{member.OutstandingFees:F2}");
            }

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
            // Step 1: Setup
            PrintStep("Setting up test data...");
            var books = await _bookRepository.GetPagedAsync(1, 1);
            var book = books.FirstOrDefault();

            if (book == null)
            {
                PrintWarning("No books available for demo");
                return;
            }

            var members = await _memberRepository.GetPagedAsync(1, 1, activeOnly: true);
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
            loan = await _loanRepository.CreateAsync(loan);

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
                await _loanRepository.UpdateAsync(loan);

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
                await _loanRepository.UpdateAsync(loan);

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
            var totalLoans = await _loanRepository.GetCountAsync();
            var activeLoansCount = await _loanRepository.GetCountAsync(LoanStatus.Active);

            PrintSuccess("Loan Statistics:");
            PrintInfo($"  Total Loans: {totalLoans}");
            PrintInfo($"  Active Loans: {activeLoansCount}");

            PrintScenarioComplete("Scenario 6");
        }
        catch (Exception ex)
        {
            PrintError($"Scenario 6 failed: {ex.Message}");
            throw;
        }
    }

    #region Helper Methods

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
