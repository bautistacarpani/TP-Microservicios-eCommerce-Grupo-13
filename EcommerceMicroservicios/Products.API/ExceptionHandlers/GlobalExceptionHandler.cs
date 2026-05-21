using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Products.API.ExceptionHandlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IWebHostEnvironment env, ILogger<GlobalExceptionHandler> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Ocurrió un error no controlado en el servidor.");

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var detalle = _env.IsDevelopment()
            ? exception.Message
            : "Ocurrió un error interno en el servidor. Intente más tarde.";

        if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
            correlationId = Guid.NewGuid().ToString();

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = 500,
            Detail = detalle,
            Instance = context.Request.Path.Value,
            Extensions =
            {
                ["correlationId"] = correlationId.ToString(),
                ["errorCode"] = "PRD-005",
                ["errorMessage"] = "Error interno al procesar la solicitud."
            }
        }, cancellationToken);

        return true;
    }
}