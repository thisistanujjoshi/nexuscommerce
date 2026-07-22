using Microsoft.AspNetCore.Mvc;
using Orders.Application.Common;
using Orders.Domain.Exceptions;

namespace Orders.Api.Middleware;

/// <summary>
/// Translates application/domain exceptions into RFC 7807 problem responses.
/// Invalid state transitions surface as 409 Conflict; invariant violations as 400.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var (status, title) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
                OrderDomainException e when e.Message.StartsWith("Cannot move order") =>
                    (StatusCodes.Status409Conflict, "Invalid state transition"),
                OrderDomainException => (StatusCodes.Status400BadRequest, "Invalid request"),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
            };

            if (status == StatusCodes.Status500InternalServerError)
                logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                logger.LogInformation("Request {Method} {Path} failed with {Status}: {Message}",
                    context.Request.Method, context.Request.Path, status, ex.Message);

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status == StatusCodes.Status500InternalServerError ? null : ex.Message,
                Instance = context.Request.Path
            });
        }
    }
}
