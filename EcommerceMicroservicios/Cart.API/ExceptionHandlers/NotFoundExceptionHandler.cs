using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Cart.API.Exceptions;

namespace Cart.API.ExceptionHandlers;
// ══════════════════════════════════════════════════════════════════════
// NOT FOUND EXCEPTION HANDLER
// Atrapa NotFoundException y devuelve HTTP 404.
// Se dispara para CRT-001 (carrito no encontrado) y CRT-002 (producto no en carrito).
// Incluye correlationId en el payload para trazabilidad distribuida.
// ══════════════════════════════════════════════════════════════════════
public class NotFoundExceptionHandler : IExceptionHandler
{
    private readonly IWebHostEnvironment _env;

    public NotFoundExceptionHandler(IWebHostEnvironment env) => _env = env;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not NotFoundException ex)
            return false;

        context.Response.StatusCode = StatusCodes.Status404NotFound;

        var logger = context.RequestServices.GetRequiredService<ILogger<NotFoundExceptionHandler>>();
        logger.LogWarning("Recurso no encontrado en {Endpoint}. Código de Error: {ErrorCode}. Detalle: {Message}",
            context.Request.Path, ex.ErrorCode, ex.Message);

        var detalle = _env.IsDevelopment()
            ? ex.Message
            : "El recurso solicitado no existe o no está disponible.";

        var errorMsg = _env.IsDevelopment()
            ? ex.Message
            : "Recurso no encontrado.";

        if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
            correlationId = Guid.NewGuid().ToString();

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "Not Found",
            Status = 404,
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
