using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using DbDemo.Application.Repositories;
using DbDemo.WebApi.DTOs;

namespace DbDemo.WebApi.Tests.Integration;

/// <summary>
/// Integration tests for the WebApi using a test server
/// Note: These tests use mocked repositories to avoid database dependencies
/// </summary>
public class WebApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WebApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing registrations
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(IBookRepository) ||
                    d.ServiceType == typeof(ICategoryRepository) ||
                    d.ServiceType == typeof(SqlConnection)).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Add mock repositories and a real connection with dummy connection string
                var mockBookRepo = new Mock<IBookRepository>();
                var mockCategoryRepo = new Mock<ICategoryRepository>();

                // Use a real SqlConnection with dummy connection string (won't be opened in these tests)
                var connection = new SqlConnection("Server=localhost;Database=TestDb;TrustServerCertificate=True;");

                services.AddScoped(_ => mockBookRepo.Object);
                services.AddScoped(_ => mockCategoryRepo.Object);
                services.AddTransient(_ => connection);
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetBooks_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/books?page=1&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetCategories_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/categories");

        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetBooks_WithInvalidPageNumber_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/books?page=0&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBooks_WithInvalidPageSize_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/books?page=1&pageSize=101");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchBooks_WithEmptyQuery_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/books/search?query=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ApiEndpoints_ReturnJsonContentType()
    {
        // Arrange
        var endpoints = new[]
        {
            "/api/books?page=1&pageSize=10",
            "/api/categories"
        };

        foreach (var endpoint in endpoints)
        {
            // Act
            var response = await _client.GetAsync(endpoint);

            // Assert
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        }
    }

    [Fact]
    public async Task CreateBook_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var invalidRequest = new CreateBookRequest
        {
            ISBN = "",  // Invalid: required
            Title = "",  // Invalid: required
            CategoryId = 0,  // Invalid: must be positive
            TotalCopies = -1  // Invalid: must be non-negative
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/books", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCategory_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var invalidRequest = new CreateCategoryRequest
        {
            Name = "",  // Invalid: required
            Description = null
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/categories", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBook_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/books/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCategory_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/categories/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResponseFormat_MatchesApiResponseStructure()
    {
        // Act
        var response = await _client.GetAsync("/api/categories");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<CategoryDto>>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task PaginatedResponse_ContainsPaginationMetadata()
    {
        // Act
        var response = await _client.GetAsync("/api/books?page=1&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponse<BookDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        paginatedResponse.Should().NotBeNull();
        paginatedResponse!.Page.Should().Be(1);
        paginatedResponse.PageSize.Should().Be(10);
        paginatedResponse.TotalCount.Should().BeGreaterThanOrEqualTo(0);
        paginatedResponse.TotalPages.Should().BeGreaterThanOrEqualTo(0);
        paginatedResponse.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task CorsPolicy_AllowsRequests()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/books");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request);

        // Assert
        // The API should handle OPTIONS requests for CORS
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }
}
