using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Cart.API.Exceptions;

namespace Cart.API.ExceptionHandlers;
// ══════════════════════════════════════════════════════════════════════
// BUSINESS RULE EXCEPTION HANDLER
// Atrapa BusinessRuleException y devuelve HTTP 422.
// Se dispara cuando se viola una regla de negocio del carrito.
// ══════════════════════════════════════════════════════════════════════
public class BusinessRuleExceptionHandler : IExceptionHandler
{
    private readonly IWebHostEnvironment _env;

    public BusinessRuleExceptionHandler(IWebHostEnvironment env) => _env = env;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException ex)
            return false;

        context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

        var logger = context.RequestServices.GetRequiredService<ILogger<BusinessRuleExceptionHandler>>();
        logger.LogWarning("Regla de negocio violada en {Endpoint}. Código de Error: {ErrorCode}. Detalle: {Message}",
            context.Request.Path, ex.ErrorCode, ex.Message);

        var detalle = _env.IsDevelopment()
            ? ex.Message
            : "La solicitud no cumple con las condiciones de negocio del sistema.";

        var errorMsg = _env.IsDevelopment()
            ? ex.Message
            : "Operación inválida por reglas de dominio.";

        if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
            correlationId = Guid.NewGuid().ToString();

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.4.2",
            Title = "Unprocessable Entity",
            Status = 422,
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
