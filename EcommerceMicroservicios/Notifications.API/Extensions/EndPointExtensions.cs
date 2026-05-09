using Notifications.API.Exceptions;
using Notifications.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Dapper; // <-- Necesario para los métodos de extensión SQL
using System.Data;
using Notifications.API.Repositories; // <-- Necesario para IDbConnection

namespace Notifications.API.Extensions
{
    public static class NotificationEndpoints
    {
        // EXPLICACIÓN: Borramos la lista estática porque ahora la "fuente de verdad" es notifications.db

        public static void MapNotificationEndpoints(this WebApplication app)
        {
            // 1. POST /api/notifications/send
            // EXPLICACIÓN: Agregamos 'async' y el parámetro 'IDbConnection db'
            app.MapPost("/api/notifications/send", async (CreateNotificationRequest request, NotificationsRepository repo, ILogger<Program> logger) =>
            {
                logger.LogInformation("Iniciando proceso de envío de notificación para el usuario: {UsuarioId}", request.UsuarioId);

                // Validaciones de reglas de negocio (siguen igual)
                if (string.IsNullOrWhiteSpace(request.Mensaje) || string.IsNullOrWhiteSpace(request.Tipo))
                {
                    logger.LogWarning("Validación fallida: Datos incompletos para UsuarioId: {UsuarioId}", request.UsuarioId);
                    throw new BusinessRuleException("NTF-002", "Los datos de la notificación son inválidos.");
                }

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

                    // USAMOS EL REPOSITORIO
                    await repo.CreateAsync(notification);

                    logger.LogInformation("Notificación guardada vía Repositorio. ID: {Id}", notification.Id);
                    return Results.Created($"/api/notifications/{notification.Id}", notification);

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error crítico inesperado al persistir en DB para el usuario {UsuarioId}", request.UsuarioId);
                    throw;
                }
            });

            // 2. GET /api/notifications/{userId}
            app.MapGet("/api/notifications/{userId}", async (Guid userId, NotificationsRepository repo, ILogger<Program> logger) =>
            {
                logger.LogInformation("Consultando base de datos para el usuario: {UsuarioId}", userId);

                var result = await repo.GetByUserIdAsync(userId);
                var userNotifications = result.ToList();

                if (!userNotifications.Any())
                {
                    logger.LogWarning("No se encontraron registros en SQLite para el usuario: {UsuarioId}", userId);
                    throw new NotFoundException("NTF-003", "No se encontraron notificaciones para el usuario.");
                }

                logger.LogInformation("Se encontraron {Cantidad} notificaciones en la base de datos.", userNotifications.Count);

                return Results.Ok(userNotifications);
            });
        }
    }
}

