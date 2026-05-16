using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Users.API.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Users.API.ExceptionHandlers
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

            var statusCode = ex.ErrorCode switch
            {
                "USR-001" => StatusCodes.Status409Conflict,
                "USR-003" => StatusCodes.Status401Unauthorized,
                "USR-004" => StatusCodes.Status403Forbidden,
                "USR-005" => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status422UnprocessableEntity
            };

            context.Response.StatusCode = statusCode;

            // 🪵 🔥 LOGGING ENRIQUECIDO (Exigencia del punto 5.3)
            // Usamos LogWarning para reglas de negocio e inyectamos el 'errorCode' estructurado para Serilog
            var logger = context.RequestServices.GetRequiredService<ILogger<BusinessRuleExceptionHandler>>();
            logger.LogWarning("Regla de negocio violada en {Endpoint}. Código de Error: {ErrorCode}. Detalle: {Message}",
                context.Request.Path,
                ex.ErrorCode,
                ex.Message);


            // 🛡️ CONTROL DE ENTORNO (Exigencia del punto 5.2)
            // Si estamos en Desarrollo, mostramos el mensaje específico del throw.
            // Si pasamos a Producción, devolvemos un mensaje seguro para no dar pistas de lógica interna.
            var detalleSeguro = _env.IsDevelopment()
                ? ex.Message
                : "La solicitud no cumple con las condiciones de negocio del sistema.";
          
            var errorMsgSeguro = _env.IsDevelopment()
                ? ex.Message
                : "Operación inválida por reglas de dominio.";

            // 🛡️ Recuperamos el Correlation ID que está corriendo en este request actual
            // Si por alguna razón no existiera, generamos uno nuevo para que el JSON no vaya vacío
            if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }


            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = $"https://httpstatuses.com/{statusCode}",
                Title = statusCode switch
                {
                    400 => "Bad Request",
                    401 => "Unauthorized",
                    403 => "Forbidden",
                    409 => "Conflict",
                    422 => "Unprocessable Entity",
                    _ => "Error"
                },
                Status = statusCode,
                Detail = detalleSeguro,
                Instance = context.Request.Path,
                Extensions =
            {
                ["errorCode"] = ex.ErrorCode,
                ["errorMessage"] = errorMsgSeguro,
                 // 🔥 EXIGENCIA 5.5: Campo extra en las respuestas de error
                ["correlationId"] = correlationId.ToString()

            }
            }, cancellationToken);

            return true;
        }
    }
}
