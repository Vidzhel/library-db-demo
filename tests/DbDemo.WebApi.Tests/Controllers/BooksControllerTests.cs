using System.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Moq;
using DbDemo.Application.Repositories;
using DbDemo.Domain.Entities;
using DbDemo.WebApi.Controllers;
using DbDemo.WebApi.DTOs;
using DbDemo.WebApi.Services;

namespace DbDemo.WebApi.Tests.Controllers;

public class BooksControllerTests
{
    private readonly Mock<IBookRepository> _mockBookRepository;
    private readonly Mock<ICategoryRepository> _mockCategoryRepository;
    private readonly Mock<ITransactionContext> _mockTransactionContext;
    private readonly Mock<ILogger<BooksController>> _mockLogger;
    private readonly BooksController _controller;

    public BooksControllerTests()
    {
        _mockBookRepository = new Mock<IBookRepository>();
        _mockCategoryRepository = new Mock<ICategoryRepository>();
        _mockTransactionContext = new Mock<ITransactionContext>();
        _mockLogger = new Mock<ILogger<BooksController>>();

        // Note: We don't need to setup Transaction property because repository mocks
        // use It.IsAny<SqlTransaction>() which works without a real transaction

        _controller = new BooksController(
            _mockBookRepository.Object,
            _mockCategoryRepository.Object,
            _mockTransactionContext.Object,
            _mockLogger.Object);
    }

    #region GetBooks Tests

    [Fact]
    public async Task GetBooks_WithValidParameters_ReturnsOkResultWithPaginatedBooks()
    {
        // Arrange
        var books = new List<Book>
        {
            CreateTestBook(1, "ISBN1234567890", "Test Book 1"),
            CreateTestBook(2, "ISBN0987654321", "Test Book 2")
        };
        var category = CreateTestCategory(1, "Fiction");

        _mockBookRepository.Setup(r => r.GetPagedAsync(1, 10, false, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(books);
        _mockBookRepository.Setup(r => r.GetCountAsync(false, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(2);
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(category);

        // Act
        var result = await _controller.GetBooks(1, 10, null, default);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as PaginatedResponse<BookDto>;
        response.Should().NotBeNull();
        response!.Data.Should().HaveCount(2);
        response.TotalCount.Should().Be(2);
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetBooks_WithInvalidPageNumber_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetBooks(0, 10, null, default);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badResult = result.Result as BadRequestObjectResult;
        var response = badResult!.Value as ApiResponse<object>;
        response!.Success.Should().BeFalse();
        response.Message.Should().Contain("Page number must be at least 1");
    }

    [Fact]
    public async Task GetBooks_WithInvalidPageSize_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetBooks(1, 0, null, default);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetBooks_WithPageSizeAboveMax_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetBooks(1, 101, null, default);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badResult = result.Result as BadRequestObjectResult;
        var response = badResult!.Value as ApiResponse<object>;
        response!.Message.Should().Contain("Page size must be between 1 and 100");
    }

    [Fact]
    public async Task GetBooks_WithCategoryFilter_ReturnsFilteredBooks()
    {
        // Arrange
        var books = new List<Book>
        {
            CreateTestBook(1, "ISBN1234567890", "Fiction Book", categoryId: 5)
        };
        var category = CreateTestCategory(5, "Fiction");

        _mockBookRepository.Setup(r => r.GetByCategoryAsync(5, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(books);
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(category);

        // Act
        var result = await _controller.GetBooks(1, 10, 5, default);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as PaginatedResponse<BookDto>;
        response.Should().NotBeNull();
        response!.Data.Should().HaveCount(1);
        response.Data.First().CategoryId.Should().Be(5);
    }

    #endregion

    #region GetBook Tests

    [Fact]
    public async Task GetBook_WithValidId_ReturnsOkResultWithBook()
    {
        // Arrange
        var book = CreateTestBook(1, "ISBN1234567890", "Test Book");
        var category = CreateTestCategory(1, "Fiction");

        _mockBookRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(book);
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(category);

        // Act
        var result = await _controller.GetBook(1, default);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<BookDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(1);
        response.Data.Title.Should().Be("Test Book");
    }

    [Fact]
    public async Task GetBook_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        _mockBookRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync((Book?)null);

        // Act
        var result = await _controller.GetBook(999, default);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<BookDto>;
        response!.Success.Should().BeFalse();
        response.Message.Should().Contain("Book with ID 999 not found");
    }

    #endregion

    #region SearchBooks Tests

    [Fact]
    public async Task SearchBooks_WithValidQuery_ReturnsMatchingBooks()
    {
        // Arrange
        var books = new List<Book>
        {
            CreateTestBook(1, "ISBN1234567890", "Harry Potter"),
            CreateTestBook(2, "ISBN0987654321", "Harry Dresden")
        };
        var category = CreateTestCategory(1, "Fiction");

        _mockBookRepository.Setup(r => r.SearchByTitleAsync("Harry", It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(books);
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(category);

        // Act
        var result = await _controller.SearchBooks("Harry", default);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<List<BookDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().HaveCount(2);
        response.Message.Should().Contain("Found 2 book(s)");
    }

    [Fact]
    public async Task SearchBooks_WithEmptyQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchBooks("", default);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badResult = result.Result as BadRequestObjectResult;
        var response = badResult!.Value as ApiResponse<List<BookDto>>;
        response!.Success.Should().BeFalse();
        response.Message.Should().Contain("Search query is required");
    }

    [Fact]
    public async Task SearchBooks_WithWhitespaceQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchBooks("   ", default);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region CreateBook Tests

    [Fact]
    public async Task CreateBook_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateBookRequest
        {
            ISBN = "1234567890",
            Title = "New Book",
            CategoryId = 1,
            TotalCopies = 5
        };
        var category = CreateTestCategory(1, "Fiction");
        var createdBook = CreateTestBook(1, request.ISBN, request.Title);

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(category);
        _mockBookRepository.Setup(r => r.GetByIsbnAsync(request.ISBN, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync((Book?)null);
        _mockBookRepository.Setup(r => r.CreateAsync(It.IsAny<Book>(), It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(createdBook);

        // Act
        var result = await _controller.CreateBook(request, default);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        var response = createdResult!.Value as ApiResponse<BookDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Message.Should().Contain("Book created successfully");
    }

    [Fact]
    public async Task CreateBook_WithNonExistentCategory_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateBookRequest
        {
            ISBN = "1234567890",
            Title = "New Book",
            CategoryId = 999,
            TotalCopies = 5
        };

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _controller.CreateBook(request, default);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badResult = result.Result as BadRequestObjectResult;
        var response = badResult!.Value as ApiResponse<BookDto>;
        response!.Success.Should().BeFalse();
        response.Message.Should().Contain("Category with ID 999 not found");
    }

    [Fact]
    public async Task CreateBook_WithDuplicateISBN_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateBookRequest
        {
            ISBN = "1234567890",
            Title = "New Book",
            CategoryId = 1,
            TotalCopies = 5
        };
        var category = CreateTestCategory(1, "Fiction");
        var existingBook = CreateTestBook(5, request.ISBN, "Existing Book");

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(category);
        _mockBookRepository.Setup(r => r.GetByIsbnAsync(request.ISBN, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(existingBook);

        // Act
        var result = await _controller.CreateBook(request, default);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badResult = result.Result as BadRequestObjectResult;
        var response = badResult!.Value as ApiResponse<BookDto>;
        response!.Success.Should().BeFalse();
        response.Message.Should().Contain($"Book with ISBN {request.ISBN} already exists");
    }

    #endregion

    #region UpdateBook Tests

    [Fact]
    public async Task UpdateBook_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var book = CreateTestBook(1, "ISBN1234567890", "Original Title");
        var category = CreateTestCategory(1, "Fiction");
        var request = new UpdateBookRequest
        {
            Title = "Updated Title"
        };

        _mockBookRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(book);
        _mockBookRepository.Setup(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(true);
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(category);

        // Act
        var result = await _controller.UpdateBook(1, request, default);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<BookDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Contain("Book updated successfully");
    }

    [Fact]
    public async Task UpdateBook_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateBookRequest { Title = "Updated Title" };

        _mockBookRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync((Book?)null);

        // Act
        var result = await _controller.UpdateBook(999, request, default);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<BookDto>;
        response!.Success.Should().BeFalse();
        response.Message.Should().Contain("Book with ID 999 not found");
    }

    [Fact]
    public async Task UpdateBook_WithNonExistentCategory_ReturnsBadRequest()
    {
        // Arrange
        var book = CreateTestBook(1, "ISBN1234567890", "Original Title");
        var request = new UpdateBookRequest { CategoryId = 999 };

        _mockBookRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(book);
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _controller.UpdateBook(1, request, default);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badResult = result.Result as BadRequestObjectResult;
        var response = badResult!.Value as ApiResponse<BookDto>;
        response!.Success.Should().BeFalse();
        response.Message.Should().Contain("Category with ID 999 not found");
    }

    #endregion

    #region DeleteBook Tests

    [Fact]
    public async Task DeleteBook_WithValidId_ReturnsOkResult()
    {
        // Arrange
        _mockBookRepository.Setup(r => r.DeleteAsync(1, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteBook(1, default);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<object>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Contain("Book deleted successfully");
    }

    [Fact]
    public async Task DeleteBook_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        _mockBookRepository.Setup(r => r.DeleteAsync(999, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteBook(999, default);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<object>;
        response!.Success.Should().BeFalse();
        response.Message.Should().Contain("Book with ID 999 not found");
    }

    #endregion

    #region GetBookCategory Tests

    [Fact]
    public async Task GetBookCategory_WithValidBookId_ReturnsCategory()
    {
        // Arrange
        var book = CreateTestBook(1, "ISBN1234567890", "Test Book");
        var category = CreateTestCategory(1, "Fiction");

        _mockBookRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(book);
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(category);

        // Act
        var result = await _controller.GetBookCategory(1, default);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<CategoryDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be("Fiction");
    }

    [Fact]
    public async Task GetBookCategory_WithNonExistentBook_ReturnsNotFound()
    {
        // Arrange
        _mockBookRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync((Book?)null);

        // Act
        var result = await _controller.GetBookCategory(999, default);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<CategoryDto>;
        response!.Success.Should().BeFalse();
        response.Message.Should().Contain("Book with ID 999 not found");
    }

    #endregion

    #region Helper Methods

    private static Book CreateTestBook(int id, string isbn, string title, int categoryId = 1)
    {
        var book = new Book(isbn, title, categoryId, totalCopies: 5);
        // Use reflection to set the Id property
        typeof(Book).GetProperty("Id")!.SetValue(book, id);
        return book;
    }

    private static Category CreateTestCategory(int id, string name)
    {
        var category = new Category(name, "Test Description");
        // Use reflection to set the Id property
        typeof(Category).GetProperty("Id")!.SetValue(category, id);
        return category;
    }

    #endregion
}
