using System.Text.Json;
using F24.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace F24.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException)
        {
            context.Response.StatusCode = 499;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = new { code = "REQUEST_CANCELLED", message = "The request was cancelled." }
            }));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled request error");
            var (status, code, message) = exception switch
            {
                InvalidNameException error => (400, error.Code, error.Message),
                EntryNotFoundException error => (404, error.Code, error.Message),
                DuplicateNameException error => (409, error.Code, error.Message),
                CannotDeleteRootException error => (409, error.Code, error.Message),
                DomainException { Code: "INVALID_LIMIT" or "INVALID_TYPE" } error => (400, error.Code,
                    error.Message),
                DbUpdateException => (500, "DATABASE_ERROR", "The database operation could not be completed."),
                NpgsqlException => (500, "DATABASE_ERROR", "The database operation could not be completed."),
                _ => (500, "INTERNAL_ERROR", "An unexpected error occurred.")
            };

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = new { code, message } }));
        }
    }
}
