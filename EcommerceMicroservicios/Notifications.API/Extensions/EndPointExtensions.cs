using Notifications.API.Exceptions;
using Notifications.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Notifications.API.Extensions
{

    public static class NotificationEndpoints
    {
        private static readonly List<Notification> notifications = new();

        public static void MapNotificationEndpoints(this WebApplication app)
        {
            app.MapPost("/api/notifications/send", (CreateNotificationRequest request) =>
            {
                if (string.IsNullOrWhiteSpace(request.Mensaje) || string.IsNullOrWhiteSpace(request.Tipo))
                {
                    // Lanza la excepción; el BusinessRuleExceptionHandler se encargará de devolver el error 400
                    throw new BusinessRuleException("NTF-002", "Los datos de la notificación son inválidos.");
                }
                // Simulación para NTF-001 (Cuando el usuario no existe)
                // Por ahora lo simulamos con un Guid vacío, luego se validará contra la API de Users
                if (request.UsuarioId == Guid.Empty)
                {
                    throw new NotFoundException("NTF-001", "Usuario no encontrado.");
                }
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UsuarioId = request.UsuarioId,
                    Mensaje = request.Mensaje,
                    Tipo = request.Tipo,
                    Estado = "Pendiente",
                    FechaEnvio = DateTime.UtcNow
                };

                notifications.Add(notification);

                return Results.Created($"/api/notifications/{notification.Id}", notification);
            });
            // 2. GET /api/notifications/{userId}
            // Agregamos el endpoint GET que pide tu catálogo
            app.MapGet("/api/notifications/{userId}", (Guid userId) =>
            {
                // Buscamos en nuestra lista "falsa" las notificaciones de este usuario
                var userNotifications = notifications.Where(n => n.UsuarioId == userId).ToList();

                // --- VALIDACIÓN: Si no tiene notificaciones, lanzamos NTF-003 ---
                if (!userNotifications.Any())
                {
                    throw new NotFoundException("NTF-003", "No se encontraron notificaciones para el usuario.");
                }

                return Results.Ok(userNotifications);
            });
        }
    }
    }


