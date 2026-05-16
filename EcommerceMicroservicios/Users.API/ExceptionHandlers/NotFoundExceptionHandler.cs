using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Users.API.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace Users.API.ExceptionHandlers;

public class NotFoundExceptionHandler : IExceptionHandler
{
   
    private readonly IWebHostEnvironment _env; // <-- Inyectamos el entorno

    public NotFoundExceptionHandler(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not NotFoundException ex)
            return false;

        context.Response.StatusCode = StatusCodes.Status404NotFound;

        // 🪵 🔥 LOGGING ENRIQUECIDO (Exigencia del punto 5.3)
                var logger = context.RequestServices.GetRequiredService<ILogger<NotFoundExceptionHandler>>();
                        logger.LogWarning("Recurso no encontrado en {Endpoint}. Código de Error: {ErrorCode}. Detalle: {Message}",
                          context.Request.Path,
                          ex.ErrorCode,
                          ex.Message );


        // 🛡️ CONTROL DE ENTORNO (Exigencia del punto 5.2)
        // En Desarrollo mostramos el mensaje que pasamos en el 'throw'
        // En Producción dejamos un mensaje estandarizado para no filtrar datos del sistema
        var mensajeDetalle = _env.IsDevelopment()
            ? ex.Message
            : "El recurso solicitado no existe o no está disponible.";

        var errorMsgSeguro = _env.IsDevelopment()
          ? ex.Message
          : "El recurso no encontrado";

        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            title = "Not Found",
            status = 404,
            detail = mensajeDetalle, // <-- Dinámico por entorno
            instance = context.Request.Path.Value,
            errorCode = ex.ErrorCode,
            errorMessage = errorMsgSeguro,
        }, cancellationToken: cancellationToken);


        return true;
    }
}