using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Orders.API.Exceptions;

namespace Orders.API.ExceptionHandlers;

public class ConflictExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context,
        Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ConflictException ex)
            return false;

        var correlationId = context.Items["X-Correlation-Id"]?.ToString();

        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.9",
            Title = "Conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = ex.Message,
            Instance = context.Request.Path,
            Extensions =
            {
                ["errorCode"] = ex.ErrorCode,
                ["errorMessage"] = ex.Message,
                ["correlationId"] = correlationId
            }
        }, cancellationToken);

        return true;
    }
}