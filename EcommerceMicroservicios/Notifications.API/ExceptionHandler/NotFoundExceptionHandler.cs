using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using Microsoft.AspNetCore.Http; 
using Notifications.API.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Notifications.API.Handler
{
    public class NotFoundExceptionHandler : IExceptionHandler
    {
        private readonly IWebHostEnvironment _env; // <-- Inyectamos el entorno

        public NotFoundExceptionHandler(IWebHostEnvironment env)
        {
            _env = env;
        }
        public async ValueTask<bool> TryHandleAsync(
          HttpContext context, Exception exception,
          CancellationToken cancellationToken)
        {
            if (exception is not NotFoundException ex) return false;

            context.Response.StatusCode = 404;

            // 🪵 🔥 LOGGING ENRIQUECIDO (Punto 5.3)
            var logger = context.RequestServices.GetRequiredService<ILogger<NotFoundExceptionHandler>>();
            logger.LogWarning("Recurso de notificación no encontrado en {Endpoint}. Código de Error: {ErrorCode}. Detalle: {Message}",
                context.Request.Path,
                ex.ErrorCode,
                ex.Message);

            // 🛡️ CONTROL DE ENTORNO (Punto 5.2)
            var detalleSeguro = _env.IsDevelopment()
                ? ex.Message
                : "El recurso solicitado no existe o no está disponible.";

            var errorMsgSeguro = _env.IsDevelopment()
                ? ex.Message
                : "Recurso de notificación no encontrado";

            // CORRELATION ID (Punto 5.5) - Captura el ID propagado desde Users.API
            if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }


            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Not Found",
                Status = 404,
                Detail = detalleSeguro,
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
