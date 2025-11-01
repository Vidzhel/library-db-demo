using DbDemo.ConsoleApp.Infrastructure.Repositories;
using DbDemo.ConsoleApp.Models;
using Xunit;

namespace DbDemo.Integration.Tests;

/// <summary>
/// Integration tests for JSON support in SQL Server 2016+.
/// Tests JSON_VALUE(), JSON_QUERY(), OPENJSON() and related JSON functions.
/// Demonstrates storing and querying flexible metadata in JSON format.
/// </summary>
[Collection("Database")]
public class JsonSupportTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly BookRepository _bookRepository;
    private readonly CategoryRepository _categoryRepository;

    public JsonSupportTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _bookRepository = new BookRepository();
        _categoryRepository = new CategoryRepository();
    }

    public async Task InitializeAsync()
    {
        // Cleanup before each test
        await _fixture.CleanupTableAsync("Books");
        await _fixture.CleanupTableAsync("Categories");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateBook_WithMetadata_ShouldStoreAndRetrieveJson()
    {
        // Arrange
        var category = await CreateTestCategory("Science Fiction");

        var metadata = BookMetadata.Create(
            genre: "Science Fiction",
            tags: new List<string> { "sci-fi", "space", "adventure" },
            series: "Foundation",
            seriesNumber: 1,
            originalLanguage: "English",
            awards: "Hugo Award",
            rating: 4.5m
        );

        var book = new Book("1234567890123", "Foundation", category.Id, 5);

        // Act
        var createdBook = await _fixture.WithTransactionAsync(async tx =>
        {
            book.UpdateMetadata(metadata);
            return await _bookRepository.CreateAsync(book, tx);
        });

        // Assert
        var retrievedBook = await _fixture.WithTransactionAsync(tx =>
            _bookRepository.GetByIdAsync(createdBook.Id, tx));

        Assert.NotNull(retrievedBook);
        Assert.NotNull(retrievedBook.Metadata);
        Assert.Equal("Science Fiction", retrievedBook.Metadata.Genre);
        Assert.Equal("Foundation", retrievedBook.Metadata.Series);
        Assert.Equal(1, retrievedBook.Metadata.SeriesNumber);
        Assert.Equal(4.5m, retrievedBook.Metadata.Rating);
        Assert.Contains("sci-fi", retrievedBook.Metadata.Tags!);
        Assert.Contains("space", retrievedBook.Metadata.Tags!);
        Assert.Equal(3, retrievedBook.Metadata.Tags!.Count);
    }

    [Fact]
    public async Task UpdateMetadata_ShouldModifyExistingJson()
    {
        // Arrange
        var category = await CreateTestCategory("Fantasy");
        var book = await CreateTestBookWithMetadata(
            "2345678901234",
            "The Fellowship of the Ring",
            category.Id,
            BookMetadata.Create(
                genre: "Fantasy",
                tags: new List<string> { "fantasy", "magic" }
            )
        );

        // Act
        var updatedMetadata = BookMetadata.Create(
            genre: "Epic Fantasy",
            tags: new List<string> { "fantasy", "magic", "epic", "adventure" },
            series: "The Lord of the Rings",
            seriesNumber: 1,
            rating: 5.0m
        );

        await _fixture.WithTransactionAsync(async tx =>
        {
            book.UpdateMetadata(updatedMetadata);
            await _bookRepository.UpdateAsync(book, tx);
        });

        // Assert
        var retrievedBook = await _fixture.WithTransactionAsync(tx =>
            _bookRepository.GetByIdAsync(book.Id, tx));

        Assert.NotNull(retrievedBook?.Metadata);
        Assert.Equal("Epic Fantasy", retrievedBook.Metadata.Genre);
        Assert.Equal("The Lord of the Rings", retrievedBook.Metadata.Series);
        Assert.Equal(1, retrievedBook.Metadata.SeriesNumber);
        Assert.Equal(5.0m, retrievedBook.Metadata.Rating);
        Assert.Equal(4, retrievedBook.Metadata.Tags!.Count);
    }

    [Fact]
    public async Task SearchByMetadataValue_WithGenre_ShouldReturnMatchingBooks()
    {
        // Arrange
        var category = await CreateTestCategory("Mixed");

        await CreateTestBookWithMetadata(
            "3456789012345",
            "Dune",
            category.Id,
            BookMetadata.Create(genre: "Science Fiction")
        );

        await CreateTestBookWithMetadata(
            "4567890123456",
            "The Hobbit",
            category.Id,
            BookMetadata.Create(genre: "Fantasy")
        );

        await CreateTestBookWithMetadata(
            "5678901234567",
            "Neuromancer",
            category.Id,
            BookMetadata.Create(genre: "Science Fiction")
        );

        // Act
        var sciFiBooks = await _fixture.WithTransactionAsync(tx =>
            _bookRepository.SearchByMetadataValueAsync("$.genre", "Science Fiction", tx));

        // Assert
        Assert.Equal(2, sciFiBooks.Count);
        Assert.All(sciFiBooks, book =>
        {
            Assert.NotNull(book.Metadata);
            Assert.Equal("Science Fiction", book.Metadata.Genre);
        });
        Assert.Contains(sciFiBooks, b => b.Title == "Dune");
        Assert.Contains(sciFiBooks, b => b.Title == "Neuromancer");
    }

    [Fact]
    public async Task SearchByMetadataValue_WithSeries_ShouldReturnBooksInSeries()
    {
        // Arrange
        var category = await CreateTestCategory("Fantasy");

        await CreateTestBookWithMetadata(
            "6789012345678",
            "The Fellowship of the Ring",
            category.Id,
            BookMetadata.Create(
                genre: "Fantasy",
                series: "The Lord of the Rings",
                seriesNumber: 1
            )
        );

        await CreateTestBookWithMetadata(
            "7890123456789",
            "The Two Towers",
            category.Id,
            BookMetadata.Create(
                genre: "Fantasy",
                series: "The Lord of the Rings",
                seriesNumber: 2
            )
        );

        await CreateTestBookWithMetadata(
            "8901234567890",
            "The Hobbit",
            category.Id,
            BookMetadata.Create(
                genre: "Fantasy",
                series: "Middle-earth",
                seriesNumber: 1
            )
        );

        // Act
        var lotrBooks = await _fixture.WithTransactionAsync(tx =>
            _bookRepository.SearchByMetadataValueAsync("$.series", "The Lord of the Rings", tx));

        // Assert
        Assert.Equal(2, lotrBooks.Count);
        Assert.All(lotrBooks, book =>
        {
            Assert.NotNull(book.Metadata);
            Assert.Equal("The Lord of the Rings", book.Metadata.Series);
        });
    }

    [Fact]
    public async Task GetBooksByTag_ShouldReturnBooksWithSpecificTag()
    {
        // Arrange
        var category = await CreateTestCategory("Mixed");

        await CreateTestBookWithMetadata(
            "9012345678901",
            "Foundation",
            category.Id,
            BookMetadata.Create(
                genre: "Science Fiction",
                tags: new List<string> { "sci-fi", "space", "classic" }
            )
        );

        await CreateTestBookWithMetadata(
            "0123456789012",
            "The Hobbit",
            category.Id,
            BookMetadata.Create(
                genre: "Fantasy",
                tags: new List<string> { "fantasy", "adventure", "classic" }
            )
        );

        await CreateTestBookWithMetadata(
            "1234567890124",
            "Neuromancer",
            category.Id,
            BookMetadata.Create(
                genre: "Science Fiction",
                tags: new List<string> { "sci-fi", "cyberpunk" }
            )
        );

        // Act
        var classicBooks = await _fixture.WithTransactionAsync(tx =>
            _bookRepository.GetBooksByTagAsync("classic", tx));

        var sciFiBooks = await _fixture.WithTransactionAsync(tx =>
            _bookRepository.GetBooksByTagAsync("sci-fi", tx));

        // Assert
        Assert.Equal(2, classicBooks.Count);
        Assert.Contains(classicBooks, b => b.Title == "Foundation");
        Assert.Contains(classicBooks, b => b.Title == "The Hobbit");

        Assert.Equal(2, sciFiBooks.Count);
        Assert.Contains(sciFiBooks, b => b.Title == "Foundation");
        Assert.Contains(sciFiBooks, b => b.Title == "Neuromancer");
    }

    [Fact]
    public async Task GetBooksWithMetadata_ShouldReturnOnlyBooksWithMetadata()
    {
        // Arrange
        var category = await CreateTestCategory("Mixed");

        // Books with metadata
        await CreateTestBookWithMetadata(
            "2345678901235",
            "Book With Metadata 1",
            category.Id,
            BookMetadata.Create(genre: "Fiction")
        );

        await CreateTestBookWithMetadata(
            "3456789012346",
            "Book With Metadata 2",
            category.Id,
            BookMetadata.Create(genre: "Non-Fiction")
        );

        // Book without metadata
        await CreateTestBook("4567890123457", "Book Without Metadata", category.Id);

        // Act
        var booksWithMetadata = await _fixture.WithTransactionAsync(tx =>
            _bookRepository.GetBooksWithMetadataAsync(tx));

        // Assert
        Assert.Equal(2, booksWithMetadata.Count);
        Assert.All(booksWithMetadata, book => Assert.NotNull(book.Metadata));
        Assert.DoesNotContain(booksWithMetadata, b => b.Title == "Book Without Metadata");
    }

    [Fact]
    public async Task Metadata_WithNullValue_ShouldHandleGracefully()
    {
        // Arrange
        var category = await CreateTestCategory("Test");
        var book = new Book("5678901234568", "Book Without Metadata", category.Id, 3);

        // Act
        var createdBook = await _fixture.WithTransactionAsync(tx =>
            _bookRepository.CreateAsync(book, tx));

        // Assert
        var retrievedBook = await _fixture.WithTransactionAsync(tx =>
            _bookRepository.GetByIdAsync(createdBook.Id, tx));

        Assert.NotNull(retrievedBook);
        Assert.Null(retrievedBook.Metadata);
    }

    [Fact]
    public async Task Metadata_WithComplexStructure_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var category = await CreateTestCategory("Technical");

        var metadata = BookMetadata.Create(
            genre: "Computer Science",
            tags: new List<string> { "programming", "algorithms", "data-structures" },
            originalLanguage: "English",
            rating: 4.7m,
            customFields: new Dictionary<string, string>
            {
                { "difficulty", "Advanced" },
                { "edition", "3rd" },
                { "isbn-10", "0262033844" }
            }
        );

        var book = new Book("6789012345679", "Introduction to Algorithms", category.Id, 2);

        // Act
        var createdBook = await _fixture.WithTransactionAsync(async tx =>
        {
            book.UpdateMetadata(metadata);
            return await _bookRepository.CreateAsync(book, tx);
        });

        // Assert
        var retrievedBook = await _fixture.WithTransactionAsync(tx =>
            _bookRepository.GetByIdAsync(createdBook.Id, tx));

        Assert.NotNull(retrievedBook?.Metadata);
        Assert.Equal("Computer Science", retrievedBook.Metadata.Genre);
        Assert.Equal(3, retrievedBook.Metadata.Tags!.Count);
        Assert.Equal(4.7m, retrievedBook.Metadata.Rating);
        Assert.NotNull(retrievedBook.Metadata.CustomFields);
        Assert.Equal("Advanced", retrievedBook.Metadata.CustomFields["difficulty"]);
        Assert.Equal("3rd", retrievedBook.Metadata.CustomFields["edition"]);
    }

    [Fact]
    public async Task BookMetadata_ToJson_ShouldProduceValidJson()
    {
        // Arrange
        var metadata = BookMetadata.Create(
            genre: "Mystery",
            tags: new List<string> { "mystery", "detective" },
            rating: 4.0m
        );

        // Act
        var json = metadata.ToJson();

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"genre\":\"Mystery\"", json);
        Assert.Contains("\"tags\":[\"mystery\",\"detective\"]", json);
        Assert.Contains("\"rating\":4.0", json);
    }

    [Fact]
    public async Task BookMetadata_FromJson_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = "{\"genre\":\"Horror\",\"tags\":[\"horror\",\"thriller\"],\"rating\":3.5}";

        // Act
        var metadata = BookMetadata.FromJson(json);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Horror", metadata.Genre);
        Assert.Equal(2, metadata.Tags!.Count);
        Assert.Contains("horror", metadata.Tags);
        Assert.Contains("thriller", metadata.Tags);
        Assert.Equal(3.5m, metadata.Rating);
    }

    // Helper methods
    private async Task<Category> CreateTestCategory(string name)
    {
        return await _fixture.WithTransactionAsync(tx =>
            _categoryRepository.CreateAsync(new Category(name), tx));
    }

    private async Task<Book> CreateTestBook(string isbn, string title, int categoryId)
    {
        return await _fixture.WithTransactionAsync(tx =>
            _bookRepository.CreateAsync(new Book(isbn, title, categoryId, 5), tx));
    }

    private async Task<Book> CreateTestBookWithMetadata(
        string isbn,
        string title,
        int categoryId,
        BookMetadata metadata)
    {
        var book = new Book(isbn, title, categoryId, 5);
        book.UpdateMetadata(metadata);

        return await _fixture.WithTransactionAsync(tx =>
            _bookRepository.CreateAsync(book, tx));
    }
}
