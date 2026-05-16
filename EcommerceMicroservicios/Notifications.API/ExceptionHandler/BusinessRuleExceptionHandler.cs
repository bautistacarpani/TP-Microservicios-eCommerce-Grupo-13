using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Notifications.API.Exceptions;

namespace Notifications.API.Handler
{
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

            // Ajustado a 400 según el catálogo del TP para NTF-002
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            // 🪵 🔥 LOGGING ENRIQUECIDO (Punto 5.3)
            var logger = context.RequestServices.GetRequiredService<ILogger<BusinessRuleExceptionHandler>>();
            logger.LogWarning("Regla de negocio de notificaciones violada en {Endpoint}. Código de Error: {ErrorCode}. Detalle: {Message}",
                context.Request.Path,
                ex.ErrorCode,
                ex.Message);

            // 🛡️ CONTROL DE ENTORNO (Punto 5.2)
            var detalleSeguro = _env.IsDevelopment()
                ? ex.Message
                : "La solicitud de notificación no cumple con las condiciones del sistema.";

            var errorMsgSeguro = _env.IsDevelopment()
                ? ex.Message
                : "Operación rechazada por reglas de dominio.";


            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "https://httpstatuses.com/400",
                Title = "Bad Request",
                Status = 400,
                Detail = detalleSeguro,
                Instance = context.Request.Path,
                Extensions =
                {
                    ["errorCode"] = ex.ErrorCode,
                    ["errorMessage"] = errorMsgSeguro
                }
            }, cancellationToken);

            return true;
        }
    }
}

