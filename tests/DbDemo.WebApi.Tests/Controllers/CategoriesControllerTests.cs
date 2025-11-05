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

public class CategoriesControllerTests
{
    private readonly Mock<ICategoryRepository> _mockCategoryRepository;
    private readonly Mock<ITransactionContext> _mockTransactionContext;
    private readonly Mock<ILogger<CategoriesController>> _mockLogger;
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _mockCategoryRepository = new Mock<ICategoryRepository>();
        _mockTransactionContext = new Mock<ITransactionContext>();
        _mockLogger = new Mock<ILogger<CategoriesController>>();

        // Note: We don't need to setup Transaction property because repository mocks
        // use It.IsAny<SqlTransaction>() which works without a real transaction

        _controller = new CategoriesController(
            _mockCategoryRepository.Object,
            _mockTransactionContext.Object,
            _mockLogger.Object);
    }

    #region GetCategories Tests

    [Fact]
    public async Task GetCategories_ReturnsOkResultWithAllCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            CreateTestCategory(1, "Fiction"),
            CreateTestCategory(2, "Non-Fiction"),
            CreateTestCategory(3, "Science")
        };

        _mockCategoryRepository.Setup(r => r.GetAllAsync(It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(categories);

        // Act
        var result = await _controller.GetCategories(default);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<List<CategoryDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().HaveCount(3);
        response.Message.Should().Contain("Retrieved 3 categories");
    }

    [Fact]
    public async Task GetCategories_WithNoCategories_ReturnsEmptyList()
    {
        // Arrange
        _mockCategoryRepository.Setup(r => r.GetAllAsync(It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(new List<Category>());

        // Act
        var result = await _controller.GetCategories(default);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<List<CategoryDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().BeEmpty();
        response.Message.Should().Contain("Retrieved 0 categories");
    }

    #endregion

    #region GetCategory Tests

    [Fact]
    public async Task GetCategory_WithValidId_ReturnsOkResultWithCategory()
    {
        // Arrange
        var category = CreateTestCategory(1, "Fiction");

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(category);

        // Act
        var result = await _controller.GetCategory(1, default);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<CategoryDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(1);
        response.Data.Name.Should().Be("Fiction");
    }

    [Fact]
    public async Task GetCategory_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _controller.GetCategory(999, default);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<CategoryDto>;
        response!.Success.Should().BeFalse();
        response.Message.Should().Contain("Category with ID 999 not found");
    }

    #endregion

    #region CreateCategory Tests

    [Fact]
    public async Task CreateCategory_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            Name = "New Category",
            Description = "A new category description"
        };
        var createdCategory = CreateTestCategory(1, request.Name);

        _mockCategoryRepository.Setup(r => r.CreateAsync(It.IsAny<Category>(), It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(createdCategory);

        // Act
        var result = await _controller.CreateCategory(request, default);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        var response = createdResult!.Value as ApiResponse<CategoryDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be("New Category");
        response.Message.Should().Contain("Category created successfully");
    }

    [Fact]
    public async Task CreateCategory_VerifiesRepositoryCalledWithCorrectData()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            Name = "Test Category",
            Description = "Test Description"
        };
        var createdCategory = CreateTestCategory(1, request.Name);

        _mockCategoryRepository.Setup(r => r.CreateAsync(It.IsAny<Category>(), It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(createdCategory);

        // Act
        await _controller.CreateCategory(request, default);

        // Assert
        _mockCategoryRepository.Verify(r => r.CreateAsync(
            It.Is<Category>(c => c.Name == request.Name && c.Description == request.Description),
            It.IsAny<SqlTransaction>(),
            default), Times.Once);
    }

    #endregion

    #region UpdateCategory Tests

    [Fact]
    public async Task UpdateCategory_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var category = CreateTestCategory(1, "Original Name");
        var request = new UpdateCategoryRequest
        {
            Name = "Updated Name",
            Description = "Updated Description"
        };

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(category);
        _mockCategoryRepository.Setup(r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateCategory(1, request, default);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<CategoryDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Contain("Category updated successfully");
    }

    [Fact]
    public async Task UpdateCategory_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateCategoryRequest
        {
            Name = "Updated Name"
        };

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _controller.UpdateCategory(999, request, default);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<CategoryDto>;
        response!.Success.Should().BeFalse();
        response.Message.Should().Contain("Category with ID 999 not found");
    }

    [Fact]
    public async Task UpdateCategory_WhenUpdateFails_ReturnsNotFound()
    {
        // Arrange
        var category = CreateTestCategory(1, "Original Name");
        var request = new UpdateCategoryRequest { Name = "Updated Name" };

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(category);
        _mockCategoryRepository.Setup(r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateCategory(1, request, default);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<CategoryDto>;
        response!.Success.Should().BeFalse();
        response.Message.Should().Contain("Failed to update category with ID 1");
    }

    [Fact]
    public async Task UpdateCategory_WithPartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var category = CreateTestCategory(1, "Original Name");
        var request = new UpdateCategoryRequest
        {
            Name = "Updated Name"
            // Description not provided
        };

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(category);
        _mockCategoryRepository.Setup(r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateCategory(1, request, default);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _mockCategoryRepository.Verify(r => r.UpdateAsync(
            It.Is<Category>(c => c.Name == "Updated Name"),
            It.IsAny<SqlTransaction>(),
            default), Times.Once);
    }

    #endregion

    #region DeleteCategory Tests

    [Fact]
    public async Task DeleteCategory_WithValidId_ReturnsOkResult()
    {
        // Arrange
        _mockCategoryRepository.Setup(r => r.DeleteAsync(1, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteCategory(1, default);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<object>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Message.Should().Contain("Category deleted successfully");
    }

    [Fact]
    public async Task DeleteCategory_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        _mockCategoryRepository.Setup(r => r.DeleteAsync(999, It.IsAny<SqlTransaction>(), default))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteCategory(999, default);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        var response = notFoundResult!.Value as ApiResponse<object>;
        response!.Success.Should().BeFalse();
        response.Message.Should().Contain("Category with ID 999 not found");
    }

    #endregion

    #region Helper Methods

    private static Category CreateTestCategory(int id, string name)
    {
        var category = new Category(name, "Test Description");
        // Use reflection to set the Id property
        typeof(Category).GetProperty("Id")!.SetValue(category, id);
        return category;
    }

    #endregion
}
