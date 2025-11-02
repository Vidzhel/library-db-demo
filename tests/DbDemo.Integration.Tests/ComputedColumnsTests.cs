using DbDemo.ConsoleApp.Infrastructure.Repositories;
using DbDemo.ConsoleApp.Models;
using Xunit;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Integration tests for computed columns (both persisted and non-persisted).
/// Tests derived values automatically calculated from existing columns without
/// requiring application logic or redundant storage.
///
/// Note: Computed columns are database-level features added by migration V019.
/// They don't exist in the C# models, so we verify them through raw SQL queries.
/// </summary>
[Collection("Database")]
public class ComputedColumnsTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly AuthorRepository _authorRepository;
    private readonly BookRepository _bookRepository;
    private readonly MemberRepository _memberRepository;
    private readonly LoanRepository _loanRepository;
    private readonly CategoryRepository _categoryRepository;

    public ComputedColumnsTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _authorRepository = new AuthorRepository();
        _bookRepository = new BookRepository();
        _memberRepository = new MemberRepository();
        _loanRepository = new LoanRepository();
        _categoryRepository = new CategoryRepository();
    }

    public async Task InitializeAsync()
    {
        // Cleanup before each test
        await _fixture.CleanupTableAsync("Loans");
        await _fixture.CleanupTableAsync("BookAuthors");
        await _fixture.CleanupTableAsync("Books");
        await _fixture.CleanupTableAsync("Authors");
        await _fixture.CleanupTableAsync("Members");
        await _fixture.CleanupTableAsync("Categories");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AuthorFullName_ShouldBeComputedFromFirstAndLastName()
    {
        // Arrange & Act
        var author = await CreateTestAuthor("Isaac", "Asimov");

        // Assert - Query computed column directly via SQL
        var fullName = await _fixture.ExecuteScalarAsync<string>(
            "SELECT FullName FROM Authors WHERE Id = @Id",
            ("@Id", author.Id));

        Assert.Equal("Isaac Asimov", fullName);
    }

    [Fact]
    public async Task AuthorFullName_CanBeUsedInWhereClause()
    {
        // Arrange
        await CreateTestAuthor("Isaac", "Asimov");
        await CreateTestAuthor("Arthur", "Clarke");
        await CreateTestAuthor("Robert", "Heinlein");

        // Act - Search using computed column (should use index)
        var authors = await _fixture.ExecuteQueryAsync<(int Id, string FullName)>(
            "SELECT Id, FullName FROM Authors WHERE FullName LIKE @Pattern ORDER BY FullName",
            ("@Pattern", "%Clarke%"));

        // Assert
        Assert.Single(authors);
        Assert.Equal("Arthur Clarke", authors[0].FullName);
    }

    [Fact]
    public async Task BookYearPublished_ShouldBeComputedFromPublishedDate()
    {
        // Arrange
        var category = await CreateTestCategory("Science Fiction");
        var publishedDate = new DateTime(1951, 6, 1);

        var book = new Book("1234567890123", "Foundation", category.Id, 5);
        book.UpdatePublishingInfo(publishedDate, 255, "English");

        // Act
        var createdBook = await _fixture.WithTransactionAsync(tx =>
            _bookRepository.CreateAsync(book, tx));

        // Assert - Query computed column directly via SQL
        var year = await _fixture.ExecuteScalarAsync<int?>(
            "SELECT YearPublished FROM Books WHERE Id = @Id",
            ("@Id", createdBook.Id));

        Assert.Equal(1951, year);
    }

    [Fact]
    public async Task BookPublishedDecade_ShouldGroupByDecade()
    {
        // Arrange
        var category = await CreateTestCategory("Fiction");

        await CreateTestBookWithDate("Book 1950s", category.Id, new DateTime(1955, 1, 1));
        await CreateTestBookWithDate("Book 1960s", category.Id, new DateTime(1965, 1, 1));
        await CreateTestBookWithDate("Book 1970s A", category.Id, new DateTime(1970, 1, 1));
        await CreateTestBookWithDate("Book 1970s B", category.Id, new DateTime(1979, 12, 31));
        await CreateTestBookWithDate("Book 2000s", category.Id, new DateTime(2005, 1, 1));

        // Act - Group by computed decade column
        var decadeCounts = await _fixture.ExecuteQueryAsync<(int Decade, int Count)>(@"
            SELECT PublishedDecade AS Decade, COUNT(*) AS Count
            FROM Books
            WHERE PublishedDecade IS NOT NULL
            GROUP BY PublishedDecade
            ORDER BY PublishedDecade");

        // Assert
        Assert.Equal(4, decadeCounts.Count);
        Assert.Contains(decadeCounts, d => d.Decade == 1950 && d.Count == 1);
        Assert.Contains(decadeCounts, d => d.Decade == 1960 && d.Count == 1);
        Assert.Contains(decadeCounts, d => d.Decade == 1970 && d.Count == 2);
        Assert.Contains(decadeCounts, d => d.Decade == 2000 && d.Count == 1);
    }

    [Fact]
    public async Task MemberAge_ShouldBeComputedFromDateOfBirth()
    {
        // Arrange
        var dateOfBirth = new DateTime(1990, 6, 15);
        var member = new Member(
            "M001",  // membershipNumber
            "John",
            "Doe",
            "john.doe@example.com",
            dateOfBirth);

        // Act
        var createdMember = await _fixture.WithTransactionAsync(tx =>
            _memberRepository.CreateAsync(member, tx));

        // Assert - Query computed age directly via SQL
        var age = await _fixture.ExecuteScalarAsync<int>(
            "SELECT Age FROM Members WHERE Id = @Id",
            ("@Id", createdMember.Id));

        var expectedAge = DateTime.Now.Year - 1990;
        // Account for birthday not yet occurred this year
        if (DateTime.Now < new DateTime(DateTime.Now.Year, 6, 15))
            expectedAge--;

        Assert.Equal(expectedAge, age);
    }

    [Fact]
    public async Task LoanDaysOverdue_ShouldCalculateCorrectlyForOverdueLoans()
    {
        // Arrange
        var category = await CreateTestCategory("Fiction");
        var book = await CreateTestBook("Overdue Book", category.Id);
        var member = await CreateTestMember("Jane", "Smith");

        // Create loan using factory method
        var loan = await _fixture.WithTransactionAsync(async tx =>
        {
            var newLoan = Loan.Create(member.Id, book.Id);
            return await _loanRepository.CreateAsync(newLoan, tx);
        });

        // Update due date to 10 days ago using raw SQL
        // Note: Need to disable CHECK constraint temporarily to set past due date
        var dueDate = DateTime.UtcNow.AddDays(-10).Date;
        await _fixture.ExecuteNonQueryAsync(
            "ALTER TABLE Loans NOCHECK CONSTRAINT CK_Loans_DueDate",
            Array.Empty<(string, object)>());
        await _fixture.ExecuteNonQueryAsync(
            "UPDATE Loans SET DueDate = @DueDate WHERE Id = @Id",
            ("@DueDate", dueDate),
            ("@Id", loan.Id));
        await _fixture.ExecuteNonQueryAsync(
            "ALTER TABLE Loans CHECK CONSTRAINT CK_Loans_DueDate",
            Array.Empty<(string, object)>());

        // Act - Query computed DaysOverdue directly via SQL
        var daysOverdue = await _fixture.ExecuteScalarAsync<int>(
            "SELECT DaysOverdue FROM Loans WHERE Id = @Id",
            ("@Id", loan.Id));

        // Assert - Should be approximately 10 days (might be 9-11 depending on time of day)
        Assert.InRange(daysOverdue, 9, 11);
    }

    [Fact]
    public async Task LoanDaysOverdue_ShouldBeZeroForReturnedLoans()
    {
        // Arrange
        var category = await CreateTestCategory("Fiction");
        var book = await CreateTestBook("Returned Book", category.Id);
        var member = await CreateTestMember("Bob", "Johnson");

        var loan = await _fixture.WithTransactionAsync(async tx =>
        {
            var newLoan = Loan.Create(member.Id, book.Id);
            var created = await _loanRepository.CreateAsync(newLoan, tx);

            // Return the loan
            created.Return();
            await _loanRepository.UpdateAsync(created, tx);

            return created;
        });

        // Act - Query computed DaysOverdue
        var daysOverdue = await _fixture.ExecuteScalarAsync<int>(
            "SELECT DaysOverdue FROM Loans WHERE Id = @Id",
            ("@Id", loan.Id));

        // Assert - Should be 0 because loan was returned
        Assert.Equal(0, daysOverdue);
    }

    [Fact]
    public async Task LoanDaysOverdue_ShouldBeZeroForNotYetDueLoans()
    {
        // Arrange
        var category = await CreateTestCategory("Fiction");
        var book = await CreateTestBook("Future Due Book", category.Id);
        var member = await CreateTestMember("Alice", "Williams");

        var loan = await _fixture.WithTransactionAsync(async tx =>
        {
            var newLoan = Loan.Create(member.Id, book.Id);
            return await _loanRepository.CreateAsync(newLoan, tx);
        });

        // Loan should have default 14-day period, so not yet due

        // Act - Query computed DaysOverdue
        var daysOverdue = await _fixture.ExecuteScalarAsync<int>(
            "SELECT DaysOverdue FROM Loans WHERE Id = @Id",
            ("@Id", loan.Id));

        // Assert - Should be 0 because not yet due
        Assert.Equal(0, daysOverdue);
    }

    [Fact]
    public async Task ComputedColumns_CanBeUsedInOrderBy()
    {
        // Arrange
        await CreateTestAuthor("Zebra", "Author");
        await CreateTestAuthor("Alpha", "Writer");
        await CreateTestAuthor("Middle", "Person");

        // Act - Order by computed FullName column
        var authors = await _fixture.ExecuteQueryAsync<string>(
            "SELECT FullName FROM Authors ORDER BY FullName");

        // Assert - Should be in alphabetical order by full name
        Assert.Equal(3, authors.Count);
        Assert.Equal("Alpha Writer", authors[0]);
        Assert.Equal("Middle Person", authors[1]);
        Assert.Equal("Zebra Author", authors[2]);
    }

    [Fact]
    public async Task PersistedComputedColumn_IndexExists()
    {
        // Act - Verify index exists on FullName
        var indexExists = await _fixture.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*)
            FROM sys.indexes i
            INNER JOIN sys.objects o ON i.object_id = o.object_id
            WHERE o.name = 'Authors' AND i.name = 'IX_Authors_FullName'");

        // Assert
        Assert.Equal(1, indexExists);
    }

    [Fact]
    public async Task ComputedColumns_UpdateAutomaticallyWhenSourceChanges()
    {
        // Arrange
        var author = await CreateTestAuthor("John", "Doe");

        var originalFullName = await _fixture.ExecuteScalarAsync<string>(
            "SELECT FullName FROM Authors WHERE Id = @Id",
            ("@Id", author.Id));

        Assert.Equal("John Doe", originalFullName);

        // Act - Update source columns via SQL
        await _fixture.ExecuteNonQueryAsync(
            "UPDATE Authors SET FirstName = @First, LastName = @Last WHERE Id = @Id",
            ("@First", "Jane"),
            ("@Last", "Smith"),
            ("@Id", author.Id));

        // Assert - Computed column should update automatically
        var updatedFullName = await _fixture.ExecuteScalarAsync<string>(
            "SELECT FullName FROM Authors WHERE Id = @Id",
            ("@Id", author.Id));

        Assert.Equal("Jane Smith", updatedFullName);
    }

    // Helper methods
    private async Task<Author> CreateTestAuthor(string firstName, string lastName)
    {
        var author = new Author(firstName, lastName);
        return await _fixture.WithTransactionAsync(tx =>
            _authorRepository.CreateAsync(author, tx));
    }

    private async Task<Category> CreateTestCategory(string name)
    {
        return await _fixture.WithTransactionAsync(tx =>
            _categoryRepository.CreateAsync(new Category(name), tx));
    }

    private async Task<Book> CreateTestBook(string title, int categoryId)
    {
        var isbn = $"{new Random().Next(1000000000, 2000000000)}123";
        return await _fixture.WithTransactionAsync(tx =>
            _bookRepository.CreateAsync(new Book(isbn, title, categoryId, 5), tx));
    }

    private async Task<Book> CreateTestBookWithDate(string title, int categoryId, DateTime publishedDate)
    {
        var isbn = $"{new Random().Next(1000000000, 2000000000)}123";
        var book = new Book(isbn, title, categoryId, 5);
        book.UpdatePublishingInfo(publishedDate, 200, "English");

        return await _fixture.WithTransactionAsync(tx =>
            _bookRepository.CreateAsync(book, tx));
    }

    private async Task<Member> CreateTestMember(string firstName, string lastName)
    {
        var member = new Member(
            $"M{new Random().Next(1000, 9999)}",  // membershipNumber
            firstName,
            lastName,
            $"{firstName.ToLower()}.{lastName.ToLower()}@example.com",
            DateTime.UtcNow.AddYears(-30));

        return await _fixture.WithTransactionAsync(tx =>
            _memberRepository.CreateAsync(member, tx));
    }
}
