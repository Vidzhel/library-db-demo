using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using DbDemo.WebApi.DTOs;
using DbDemo.WebApi.Middleware;

namespace DbDemo.WebApi.Tests.Middleware;

public class ErrorHandlingMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<ILogger<ErrorHandlingMiddleware>> _mockLogger;
    private readonly ErrorHandlingMiddleware _middleware;
    private readonly DefaultHttpContext _httpContext;

    public ErrorHandlingMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        _middleware = new ErrorHandlingMiddleware(_mockNext.Object, _mockLogger.Object);
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNextDelegate()
    {
        // Arrange
        _mockNext.Setup(next => next(_httpContext)).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNext.Verify(next => next(_httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var exceptionMessage = "Invalid argument provided";
        _mockNext.Setup(next => next(_httpContext))
            .ThrowsAsync(new ArgumentException(exceptionMessage));

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        _httpContext.Response.ContentType.Should().Be("application/json");

        var responseBody = await GetResponseBody();
        var response = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("Invalid request data");
        response.Errors.Should().Contain(exceptionMessage);
    }

    [Fact]
    public async Task InvokeAsync_WithArgumentNullException_ReturnsBadRequest()
    {
        // Arrange
        var exceptionMessage = "Value cannot be null";
        _mockNext.Setup(next => next(_httpContext))
            .ThrowsAsync(new ArgumentNullException("paramName", exceptionMessage));

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

        var responseBody = await GetResponseBody();
        var response = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("Invalid request data");
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var exceptionMessage = "Invalid operation performed";
        _mockNext.Setup(next => next(_httpContext))
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

        var responseBody = await GetResponseBody();
        var response = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("Invalid operation");
        response.Errors.Should().Contain(exceptionMessage);
    }

    [Fact]
    public async Task InvokeAsync_WithKeyNotFoundException_ReturnsNotFound()
    {
        // Arrange
        var exceptionMessage = "The specified key was not found";
        _mockNext.Setup(next => next(_httpContext))
            .ThrowsAsync(new KeyNotFoundException(exceptionMessage));

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

        var responseBody = await GetResponseBody();
        var response = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("Resource not found");
        response.Errors.Should().Contain(exceptionMessage);
    }

    [Fact]
    public async Task InvokeAsync_WithGenericException_ReturnsInternalServerError()
    {
        // Arrange
        _mockNext.Setup(next => next(_httpContext))
            .ThrowsAsync(new Exception("Some unexpected error"));

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

        var responseBody = await GetResponseBody();
        var response = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Be("An internal server error occurred");
        response.Errors.Should().Contain("An unexpected error occurred. Please try again later.");
        // Verify that the actual exception message is NOT exposed
        response.Errors.Should().NotContain("Some unexpected error");
    }

    [Fact]
    public async Task InvokeAsync_WithException_LogsError()
    {
        // Arrange
        var exception = new Exception("Test exception");
        _mockNext.Setup(next => next(_httpContext))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unhandled exception occurred")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithException_SetsCorrectContentType()
    {
        // Arrange
        _mockNext.Setup(next => next(_httpContext))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task InvokeAsync_WithException_ReturnsValidJson()
    {
        // Arrange
        _mockNext.Setup(next => next(_httpContext))
            .ThrowsAsync(new ArgumentException("Test argument exception"));

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        var responseBody = await GetResponseBody();

        // Verify it's valid JSON by attempting to deserialize
        var deserializeAction = () => JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        deserializeAction.Should().NotThrow();
    }

    [Fact]
    public async Task InvokeAsync_WithMultipleExceptionTypes_ReturnsCorrectStatusCodes()
    {
        // Test ArgumentException
        await TestExceptionStatusCode(new ArgumentException("test"), HttpStatusCode.BadRequest);

        // Test InvalidOperationException
        await TestExceptionStatusCode(new InvalidOperationException("test"), HttpStatusCode.BadRequest);

        // Test KeyNotFoundException
        await TestExceptionStatusCode(new KeyNotFoundException("test"), HttpStatusCode.NotFound);

        // Test generic Exception
        await TestExceptionStatusCode(new Exception("test"), HttpStatusCode.InternalServerError);
    }

    private async Task TestExceptionStatusCode(Exception exception, HttpStatusCode expectedStatusCode)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        var mockNext = new Mock<RequestDelegate>();
        mockNext.Setup(next => next(httpContext)).ThrowsAsync(exception);
        var middleware = new ErrorHandlingMiddleware(mockNext.Object, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.StatusCode.Should().Be((int)expectedStatusCode);
    }

    private async Task<string> GetResponseBody()
    {
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_httpContext.Response.Body);
        return await reader.ReadToEndAsync();
    }
}
