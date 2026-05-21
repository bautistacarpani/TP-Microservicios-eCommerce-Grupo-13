using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Cart.API.Exceptions;

namespace Cart.API.ExceptionHandlers;

public class ValidationExceptionHandler : IExceptionHandler
{
    private readonly IWebHostEnvironment _env;

    public ValidationExceptionHandler(IWebHostEnvironment env) => _env = env;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException ex)
            return false;

        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        var logger = context.RequestServices.GetRequiredService<ILogger<ValidationExceptionHandler>>();
        logger.LogWarning("Validación fallida en {Endpoint}. Código de Error: {ErrorCode}. Detalle: {Message}",
            context.Request.Path, ex.ErrorCode, ex.Message);

        var detalle = _env.IsDevelopment()
            ? ex.Message
            : "Los datos enviados no cumplen con las reglas de validación.";

        var errorMsg = _env.IsDevelopment()
            ? ex.Message
            : "Error de validación en la solicitud.";

        if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
            correlationId = Guid.NewGuid().ToString();

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = 400,
            Detail = detalle,
            Instance = context.Request.Path.Value,
            Extensions =
            {
                ["correlationId"] = correlationId.ToString(),
                ["errorCode"] = ex.ErrorCode,
                ["errorMessage"] = errorMsg
            }
        }, cancellationToken);

        return true;
    }
}