using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using DbDemo.WebApi.DTOs;

namespace DbDemo.WebApi.Tests.DTOs;

public class ValidationTests
{
    #region CreateBookRequest Validation Tests

    [Fact]
    public void CreateBookRequest_WithValidData_PassesValidation()
    {
        // Arrange
        var request = new CreateBookRequest
        {
            ISBN = "1234567890",
            Title = "Valid Book Title",
            CategoryId = 1,
            TotalCopies = 5
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CreateBookRequest_WithInvalidISBN_FailsValidation(string? isbn)
    {
        // Arrange
        var request = new CreateBookRequest
        {
            ISBN = isbn!,
            Title = "Valid Title",
            CategoryId = 1,
            TotalCopies = 5
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("ISBN"));
    }

    [Theory]
    [InlineData("123")]  // Too short
    [InlineData("12345678901234")]  // Too long
    [InlineData("abcd567890")]  // Contains letters
    public void CreateBookRequest_WithInvalidISBNFormat_FailsValidation(string isbn)
    {
        // Arrange
        var request = new CreateBookRequest
        {
            ISBN = isbn,
            Title = "Valid Title",
            CategoryId = 1,
            TotalCopies = 5
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().Contain(v => v.MemberNames.Contains("ISBN"));
    }

    [Fact]
    public void CreateBookRequest_WithValidISBN10_PassesValidation()
    {
        // Arrange
        var request = new CreateBookRequest
        {
            ISBN = "1234567890",  // 10 digits
            Title = "Valid Title",
            CategoryId = 1,
            TotalCopies = 5
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void CreateBookRequest_WithValidISBN13_PassesValidation()
    {
        // Arrange
        var request = new CreateBookRequest
        {
            ISBN = "1234567890123",  // 13 digits
            Title = "Valid Title",
            CategoryId = 1,
            TotalCopies = 5
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CreateBookRequest_WithInvalidTitle_FailsValidation(string? title)
    {
        // Arrange
        var request = new CreateBookRequest
        {
            ISBN = "1234567890",
            Title = title!,
            CategoryId = 1,
            TotalCopies = 5
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("Title"));
    }

    [Fact]
    public void CreateBookRequest_WithTooLongTitle_FailsValidation()
    {
        // Arrange
        var request = new CreateBookRequest
        {
            ISBN = "1234567890",
            Title = new string('A', 201),  // Exceeds 200 character limit
            CategoryId = 1,
            TotalCopies = 5
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().Contain(v => v.MemberNames.Contains("Title"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateBookRequest_WithInvalidCategoryId_FailsValidation(int categoryId)
    {
        // Arrange
        var request = new CreateBookRequest
        {
            ISBN = "1234567890",
            Title = "Valid Title",
            CategoryId = categoryId,
            TotalCopies = 5
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().Contain(v => v.MemberNames.Contains("CategoryId"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1001)]
    public void CreateBookRequest_WithInvalidTotalCopies_FailsValidation(int totalCopies)
    {
        // Arrange
        var request = new CreateBookRequest
        {
            ISBN = "1234567890",
            Title = "Valid Title",
            CategoryId = 1,
            TotalCopies = totalCopies
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().Contain(v => v.MemberNames.Contains("TotalCopies"));
    }

    [Fact]
    public void CreateBookRequest_WithTooLongSubtitle_FailsValidation()
    {
        // Arrange
        var request = new CreateBookRequest
        {
            ISBN = "1234567890",
            Title = "Valid Title",
            Subtitle = new string('A', 201),
            CategoryId = 1,
            TotalCopies = 5
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().Contain(v => v.MemberNames.Contains("Subtitle"));
    }

    [Fact]
    public void CreateBookRequest_WithTooLongDescription_FailsValidation()
    {
        // Arrange
        var request = new CreateBookRequest
        {
            ISBN = "1234567890",
            Title = "Valid Title",
            Description = new string('A', 2001),
            CategoryId = 1,
            TotalCopies = 5
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().Contain(v => v.MemberNames.Contains("Description"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10001)]
    public void CreateBookRequest_WithInvalidPageCount_FailsValidation(int pageCount)
    {
        // Arrange
        var request = new CreateBookRequest
        {
            ISBN = "1234567890",
            Title = "Valid Title",
            PageCount = pageCount,
            CategoryId = 1,
            TotalCopies = 5
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().Contain(v => v.MemberNames.Contains("PageCount"));
    }

    #endregion

    #region CreateCategoryRequest Validation Tests

    [Fact]
    public void CreateCategoryRequest_WithValidData_PassesValidation()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            Name = "Fiction",
            Description = "Fictional books"
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CreateCategoryRequest_WithInvalidName_FailsValidation(string? name)
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            Name = name!,
            Description = "Valid description"
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("Name"));
    }

    #endregion

    #region UpdateBookRequest Validation Tests

    [Fact]
    public void UpdateBookRequest_WithValidData_PassesValidation()
    {
        // Arrange
        var request = new UpdateBookRequest
        {
            Title = "Updated Title",
            CategoryId = 1
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void UpdateBookRequest_WithNoData_PassesValidation()
    {
        // Arrange - All fields are optional in update request
        var request = new UpdateBookRequest();

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    #endregion

    #region ApiResponse Tests

    [Fact]
    public void ApiResponse_SuccessResponse_HasCorrectProperties()
    {
        // Arrange & Act
        var response = ApiResponse<string>.SuccessResponse("test data", "Success message");

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().Be("test data");
        response.Message.Should().Be("Success message");
        response.Errors.Should().BeNull();
    }

    [Fact]
    public void ApiResponse_ErrorResponse_HasCorrectProperties()
    {
        // Arrange
        var errors = new List<string> { "Error 1", "Error 2" };

        // Act
        var response = ApiResponse<string>.ErrorResponse("Error message", errors);

        // Assert
        response.Success.Should().BeFalse();
        response.Data.Should().BeNull();
        response.Message.Should().Be("Error message");
        response.Errors.Should().BeEquivalentTo(errors);
    }

    #endregion

    #region PaginatedResponse Tests

    [Fact]
    public void PaginatedResponse_CalculatesTotalPages_Correctly()
    {
        // Arrange
        var response = new PaginatedResponse<BookDto>
        {
            Data = new List<BookDto>(),
            Page = 1,
            PageSize = 10,
            TotalCount = 25
        };

        // Act
        var totalPages = response.TotalPages;

        // Assert
        totalPages.Should().Be(3);  // 25 items / 10 per page = 3 pages
    }

    [Fact]
    public void PaginatedResponse_WithExactDivision_CalculatesTotalPages()
    {
        // Arrange
        var response = new PaginatedResponse<BookDto>
        {
            Data = new List<BookDto>(),
            Page = 1,
            PageSize = 10,
            TotalCount = 30
        };

        // Act
        var totalPages = response.TotalPages;

        // Assert
        totalPages.Should().Be(3);  // 30 items / 10 per page = 3 pages
    }

    [Fact]
    public void PaginatedResponse_WithZeroCount_ReturnsZeroPages()
    {
        // Arrange
        var response = new PaginatedResponse<BookDto>
        {
            Data = new List<BookDto>(),
            Page = 1,
            PageSize = 10,
            TotalCount = 0
        };

        // Act
        var totalPages = response.TotalPages;

        // Assert
        totalPages.Should().Be(0);
    }

    [Fact]
    public void PaginatedResponse_HasNextPage_WhenNotOnLastPage()
    {
        // Arrange
        var response = new PaginatedResponse<BookDto>
        {
            Data = new List<BookDto>(),
            Page = 1,
            PageSize = 10,
            TotalCount = 25
        };

        // Act
        var hasNextPage = response.HasNextPage;

        // Assert
        hasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PaginatedResponse_HasNoPreviousPage_WhenOnFirstPage()
    {
        // Arrange
        var response = new PaginatedResponse<BookDto>
        {
            Data = new List<BookDto>(),
            Page = 1,
            PageSize = 10,
            TotalCount = 25
        };

        // Act
        var hasPreviousPage = response.HasPreviousPage;

        // Assert
        hasPreviousPage.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }

    #endregion
}
