using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Orders.API.ExceptionHandlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context,
        Exception exception, CancellationToken cancellationToken)
    {
        // Este handler captura cualquier excepción no manejada por los handlers específicos
        _logger.LogError(exception, "Error inesperado en Orders API");

        var correlationId = context.Items["X-Correlation-Id"]?.ToString();

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "Error interno al procesar la orden.",
            Instance = context.Request.Path,
            Extensions =
            {
                ["errorCode"] = "ORD-007",
                ["errorMessage"] = "Error interno al procesar la orden.",
                ["correlationId"] = correlationId
            }
        }, cancellationToken);

        return true;
    }
}