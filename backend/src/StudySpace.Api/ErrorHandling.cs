using Microsoft.AspNetCore.Diagnostics;

namespace StudySpace.Api;

/// Turns unhandled errors into problem responses. A malformed request is the client's mistake (400); anything else is a 500
/// that never exposes exception details.
public static class ErrorHandling
{
    public static void UseProblemErrors(this WebApplication app) =>
        app.UseExceptionHandler(handler => handler.Run(async context =>
        {
            var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            var (status, title) = error is BadHttpRequestException bad
                ? (bad.StatusCode, "Invalid request.")
                : (StatusCodes.Status500InternalServerError, "Something went wrong.");

            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(
                new { type = "about:blank", title, status },
                options: null,
                contentType: "application/problem+json");
        }));
}
