using System.Net;
using System.Text.Json;
using DbDemo.WebApi.DTOs;

namespace DbDemo.WebApi.Middleware;

/// <summary>
/// Global error handling middleware to catch and format exceptions
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var message = "An internal server error occurred";
        var errors = new List<string>();

        // Customize error response based on exception type
        switch (exception)
        {
            case ArgumentException or ArgumentNullException:
                statusCode = HttpStatusCode.BadRequest;
                message = "Invalid request data";
                errors.Add(exception.Message);
                break;

            case InvalidOperationException:
                statusCode = HttpStatusCode.BadRequest;
                message = "Invalid operation";
                errors.Add(exception.Message);
                break;

            case KeyNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                message = "Resource not found";
                errors.Add(exception.Message);
                break;

            default:
                // Don't expose internal error details in production
                errors.Add("An unexpected error occurred. Please try again later.");
                break;
        }

        var response = ApiResponse<object>.ErrorResponse(message, errors);
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Extension method to register the error handling middleware
/// </summary>
public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
