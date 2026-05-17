using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting; // <-- Necesario para IWebHostEnvironment
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace Notifications.API.Handler
{
    public class BaseExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<BaseExceptionHandler> _logger;
        private readonly IWebHostEnvironment _env; // <-- Inyectamos el entorno
        public BaseExceptionHandler(ILogger<BaseExceptionHandler> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext context,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Ocurrió un error no controlado en el servidor.");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            // Si estamos en desarrollo, exponemos el mensaje real de la falla. 
            // Si estamos en producción, devolvemos un mensaje genérico por seguridad.
            var detalleError = _env.IsDevelopment()
                ? exception.Message
                : "Ocurrió un error interno en el servidor. Intente más tarde.";

            // CORRELATION ID (Punto 5.5)
            if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error",
                Status = 500,
                Detail = detalleError, // <-- Se adapta según el entorno (Consigna 5.2)
                Instance = context.Request.Path.Value,
                Extensions =
                {
                    ["errorCode"] = "SYS-001",
                    ["errorMessage"] = "Error inesperado del sistema.",
                    ["correlationId"] = correlationId.ToString()
                }
            }, cancellationToken: cancellationToken);


            return true;
        }
    }
}