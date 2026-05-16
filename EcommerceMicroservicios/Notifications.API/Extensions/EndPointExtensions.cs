using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;                 // 🔥 AGREGADO: Para mapear el objeto ProblemDetails
using Microsoft.Extensions.Logging;
using Notifications.API.Exceptions;
using Notifications.API.Models;
using Notifications.API.Repositories;

namespace Notifications.API.Extensions;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        // =========================================================================
        // 1. POST /api/notifications/send
        // =========================================================================
        app.MapPost("/api/notifications/send", async (CreateNotificationRequest request, NotificationsRepository repo, ILogger<Program> logger) =>
        {
            logger.LogInformation("Iniciando proceso de envío de notificación para el usuario: {UsuarioId}", request.UsuarioId);

            // Validaciones de reglas de negocio -> NTF-002
            if (string.IsNullOrWhiteSpace(request.Mensaje) || string.IsNullOrWhiteSpace(request.Tipo))
            {
                logger.LogWarning("Validación fallida: Datos incompletos para UsuarioId: {UsuarioId}", request.UsuarioId);
                throw new BusinessRuleException("NTF-002", "Los datos de la notificación son inválidos.");
            }

            // Validación de existencia de Usuario -> NTF-001
            if (request.UsuarioId == Guid.Empty)
            {
                logger.LogWarning("Validación fallida: Intento de envío a Guid vacío.");
                throw new NotFoundException("NTF-001", "Usuario no encontrado.");
            }

            try
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UsuarioId = request.UsuarioId,
                    Mensaje = request.Mensaje,
                    Tipo = request.Tipo,
                    Estado = "Pendiente",
                    FechaEnvio = DateTime.UtcNow
                };

                // Guardamos en la base de datos a través del patrón repositorio
                await repo.CreateAsync(notification);

                logger.LogInformation("Notificación guardada vía Repositorio. ID: {Id}", notification.Id);
                return Results.Created($"/api/notifications/{notification.Id}", notification);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error crítico inesperado al persistir en DB para el usuario {UsuarioId}", request.UsuarioId);
                throw;
            }
        })
        .WithTags("Notifications") // 🔥 PUNTO 5.1: Agrupación en la UI de Swagger
        .Produces<Notification>(StatusCodes.Status201Created)       // Ejemplo de éxito
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)   // Ejemplo de error NTF-002
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);    // Ejemplo de error NTF-001

        // =========================================================================
        // 2. GET /api/notifications/{userId}
        // =========================================================================
        app.MapGet("/api/notifications/{userId}", async (Guid userId, NotificationsRepository repo, ILogger<Program> logger) =>
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
            return Results.Ok(userNotifications);
        })
        .WithTags("Notifications") // 🔥 PUNTO 5.1: Agrupación en la UI de Swagger
        .Produces<IEnumerable<Notification>>(StatusCodes.Status200OK) // Ejemplo de éxito (Lista)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);             // Ejemplo de error NTF-003
    }
}
