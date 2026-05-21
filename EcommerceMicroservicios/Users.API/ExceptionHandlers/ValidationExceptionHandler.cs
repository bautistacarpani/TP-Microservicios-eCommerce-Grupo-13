using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Users.API.Exceptions;

namespace Users.API.ExceptionHandlers
{
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

            // 🪵 🔥 LOGGING ENRIQUECIDO (Punto 5.3)
            var logger = context.RequestServices.GetRequiredService<ILogger<ValidationExceptionHandler>>();
            logger.LogWarning("Validación fallida en {Endpoint}. Código de Error: {ErrorCode}. Detalle: {Message}",
                context.Request.Path,
                ex.ErrorCode,
                ex.Message);

            // 2. CONTROL DE ENTORNO (Punto 5.2) - Mensajes seguros en producción
            var detalleSeguro = _env.IsDevelopment()
                ? ex.Message
                : "Los datos enviados no cumplen con las reglas de validación de la API.";

            // Si es producción, unificamos el mensaje para no filtrar estructuras internas
            var mensajeDetalle = _env.IsDevelopment()
                ? ex.Message
                : "Los datos enviados no cumplen con las validaciones requeridas.";

            var errorMsgSeguro = _env.IsDevelopment()
                ? ex.Message
                : "Error de validación en la solicitud.";

            // 3. CORRELATION ID (Punto 5.5) - Extraer cabecera para adjuntar al error
            if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString(); // Fallback de seguridad
            }


            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request",
                Status = 400,
                Detail = mensajeDetalle,
                Instance = context.Request.Path.Value,
                Extensions =
                {
                    ["correlationId"] = correlationId.ToString(),
                    ["errorCode"] = ex.ErrorCode,
                    ["errorMessage"] = errorMsgSeguro,
                }
                 
            }, cancellationToken: cancellationToken);


            return true;
        }
    }
}
