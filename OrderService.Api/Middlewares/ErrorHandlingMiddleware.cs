using System.Net;
using System.Text.Json;
using FluentValidation;

namespace OrderService.Api.Middlewares;

/// <summary>
/// Global error handling middleware for centralized error handling and response formatting
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        object response;
        HttpStatusCode statusCode;

        switch (exception)
        {
            case ValidationException validationEx:
                statusCode = HttpStatusCode.BadRequest;
                response = CreateValidationErrorResponse(validationEx);
                break;
            case ArgumentNullException:
                statusCode = HttpStatusCode.BadRequest;
                response = CreateErrorResponse("Invalid Request", exception.Message);
                break;
            case ArgumentException:
                statusCode = HttpStatusCode.BadRequest;
                response = CreateErrorResponse("Invalid Request", exception.Message);
                break;
            case InvalidOperationException:
                statusCode = HttpStatusCode.BadRequest;
                response = CreateErrorResponse("Invalid Operation", exception.Message);
                break;
            case KeyNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                response = CreateErrorResponse("Resource Not Found", exception.Message);
                break;
            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                response = CreateErrorResponse("Unauthorized", exception.Message);
                break;
            case TimeoutException:
                statusCode = HttpStatusCode.RequestTimeout;
                response = CreateErrorResponse("Request Timeout", exception.Message);
                break;
            default:
                statusCode = HttpStatusCode.InternalServerError;
                response = CreateErrorResponse("Internal Server Error", "An unexpected error occurred");
                break;
        }

        context.Response.StatusCode = (int)statusCode;

        var jsonResponse = JsonSerializer.Serialize(response, _jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }

    private static object CreateErrorResponse(string title, string detail)
    {
        return new
        {
            type = "https://tools.ietf.org/html/rfc7231",
            title = title,
            status = (int)HttpStatusCode.BadRequest,
            detail = detail,
            timestamp = DateTime.UtcNow
        };
    }

    private static object CreateValidationErrorResponse(ValidationException validationException)
    {
        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            title = "Validation Error",
            status = 400,
            detail = "One or more validation errors occurred",
            errors = errors,
            timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Extension method to register the error handling middleware
/// </summary>
public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
