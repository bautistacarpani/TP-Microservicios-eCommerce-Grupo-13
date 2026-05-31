using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;                 // 🔥 AGREGADO: Para mapear el objeto ProblemDetails
using Microsoft.Extensions.Logging;
using Notifications.API.Exceptions;
using Notifications.API.Models;
using Notifications.API.Repositories;
using Notifications.API.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Notifications.API.Extensions;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        // =========================================================================
        // 1. POST /api/notifications/send
        // =========================================================================
        app.MapPost("/api/notifications/send", async (
            CreateNotificationRequest request, 
            NotificationsRepository repo,
            EmailService emailService,// email noti
            UsersClient usersClient, // email noti
            ILogger<Program> logger,
            IHttpClientFactory httpClientFactory,
            HttpContext context) => // para el correlation ID
        {
            logger.LogInformation("Iniciando proceso de envío de notificación para el usuario: {UsuarioId}", request.UsuarioId);

            // Validaciones de reglas de negocio -> NTF-002
            if (string.IsNullOrWhiteSpace(request.Mensaje) || string.IsNullOrWhiteSpace(request.Tipo))
            {
                logger.LogWarning("Validación fallida: Datos incompletos para UsuarioId: {UsuarioId}", request.UsuarioId);
                throw new BusinessRuleException("NTF-002", "Los datos de la notificación son inválidos.");
            }

            // Validación de existencia de Usuario -> NTF-001
            if (string.IsNullOrEmpty(request.UsuarioId))
            {
                logger.LogWarning("Validación fallida: Intento de envío a Guid vacío.");
                throw new NotFoundException("NTF-001", "Usuario no encontrado.");
            }

            // 🔥 NUEVO BLOQUE (PUNTOS EXTRA): Validación Cruzada Dinámica por HTTP 
            // 1. Extraemos o generamos el Correlation ID para mantener el hilo de trazabilidad (Punto 5.5)
            if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            // 2. Preparamos el cliente HTTP registrado en tu Program.cs
            var httpClient = httpClientFactory.CreateClient("UsersClient");

            // Propagamos el Correlation ID en la cabecera saliente de la consulta interna (Punto 5.5)
            httpClient.DefaultRequestHeaders.Remove("X-Correlation-Id");
            httpClient.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId.ToString());

            try
            {
                // Le preguntamos a Users.API si el ID realmente existe en su base de datos
                var response = await httpClient.GetAsync($"/api/users/{request.UsuarioId}/exists");

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Validación fallida: El UsuarioId {UsuarioId} no existe en la base de datos de Users.API. CorrelationID: {CorrelationId}",
                        request.UsuarioId, correlationId);

                    // Reutiliza tu excepción exacta para disparar el código de catálogo NTF-001
                    throw new NotFoundException("NTF-001", "Usuario no encontrado.");
                }
            }
            catch (NotFoundException)
            {
                // Si fue nuestra propia excepción de "Usuario no encontrado", la dejamos pasar para que la ataje el Handler
                throw;
            }
            catch (Exception ex)
            {
                // Si falla la comunicación por red o timeout, lo logueamos como advertencia de infraestructura
                logger.LogError(ex, "Error de comunicación al intentar validar la existencia del usuario {UsuarioId}", request.UsuarioId);
                // Podés decidir si dejar pasar la notificación o frenarla. Al lanzar, protegés la integridad:
                throw new BusinessRuleException("SYS-002", "No se pudo verificar la identidad del usuario en este momento.");
            }
            // ---------------------------------------------------------------------------------

            // validación camino feliz
            try
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid().ToString(),
                    UsuarioId = request.UsuarioId,
                    Mensaje = request.Mensaje,
                    Tipo = request.Tipo,
                    Estado = "Pendiente",
                    Leida = false,
                    FechaEnvio = DateTime.UtcNow
                };

                // Guardamos en BD primero, independientemente del resultado del email
                await repo.CreateAsync(notification);
                logger.LogInformation("Notificación guardada en BD. ID: {Id}", notification.Id);

                // Intentamos enviar el email solo si el tipo es Email
                if (request.Tipo == "Email")
                {
                    // Obtenemos el email real del usuario desde Users API
                    var email = await usersClient.ObtenerEmailAsync(
                        request.UsuarioId,
                        correlationId.ToString());

                    if (email != null)
                    {
                        var emailEnviado = await emailService.EnviarEmailAsync(email, request.Mensaje);
                        var nuevoEstado = emailEnviado ? "Enviada" : "Fallida";
                        await repo.UpdateEstadoAsync(notification.Id, nuevoEstado);
                        notification.Estado = nuevoEstado;
                        logger.LogInformation("Estado de notificación {Id} actualizado a {Estado}",
                            notification.Id, nuevoEstado);
                    }
                    else
                    {
                        // No pudimos obtener el email, marcamos como Fallida
                        await repo.UpdateEstadoAsync(notification.Id, "Fallida");
                        notification.Estado = "Fallida";
                        logger.LogWarning("No se pudo obtener email para usuario {UsuarioId}", request.UsuarioId);
                    }
                }
                else
                {
                    // Para Push y SMS simulamos el envío
                    await repo.UpdateEstadoAsync(notification.Id, "Enviada");
                    notification.Estado = "Enviada";
                }

                return Results.Created($"/api/notifications/{notification.Id}", notification);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error crítico inesperado al persistir en DB para el usuario {UsuarioId}", request.UsuarioId);
                throw;
            }
        })
        .WithTags("Notifications") // 🔥 PUNTO 5.1: Agrupación en la UI de Swagger
        .WithSummary("Envía una notificación a un usuario específico.") // 🔥 PUNTO 5.2: Descripción para Swagger
        .WithDescription("Valida la existencia del usuario y registra una notificación en el sistema") // 🔥 PUNTO 5.2: Descripción detallada para Swagger
        .Produces<Notification>(StatusCodes.Status201Created)       // Ejemplo de éxito
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)   // Ejemplo de error NTF-002
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);    // Ejemplo de error NTF-001

        // =========================================================================
        // 2. GET /api/notifications/{userId}
        // =========================================================================
        app.MapGet("/api/notifications/{userId}", async (string userId, NotificationsRepository repo, ILogger<Program> logger) =>
        {
            logger.LogInformation("Consultando base de datos para el usuario: {UsuarioId}", userId);

            var result = await repo.GetByUserIdAsync(userId);
            var userNotifications = result.ToList();

            // Validación de historial vacío -> NTF-003
            if (!userNotifications.Any())
            {
                logger.LogWarning("No se encontraron registros en SQLite para el usuario: {UsuarioId}", userId);
                throw new NotFoundException("NTF-003", "No se encontraron notificaciones para el usuario.");
            }

            logger.LogInformation("Se encontraron {Cantidad} notificaciones en la base de datos.", userNotifications.Count);

            var response = userNotifications.Select(n => new NotificationResponse(n.Id, n.UsuarioId, n.Mensaje, n.Tipo, n.Estado, n.FechaEnvio, n.Leida));
            return Results.Ok(response);
       
        })
        .WithTags("Notifications") // 🔥 PUNTO 5.1: Agrupación en la UI de Swagger
        .WithSummary("Obtiene el historial de notificaciones de un usuario.") // 🔥 PUNTO 5.2: Descripción para Swagger
        .WithDescription("Consulta la base de datos para devolver todas las notificaciones asociadas a un usuario específico.") // 🔥 PUNTO 5.2: Descripción detallada para Swagger
        .Produces<IEnumerable<Notification>>(StatusCodes.Status200OK) // Ejemplo de éxito (Lista)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);             // Ejemplo de error NTF-003
    }
}
