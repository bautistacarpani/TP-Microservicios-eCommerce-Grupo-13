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
        private static readonly List<Notification> notifications = [];

        public static void MapNotificationEndpoints(this WebApplication app)
        {
            app.MapPost("/api/notifications", (CreateNotificationRequest request) =>
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

                notifications.Add(notification);

                return Results.Created($"/api/notifications/{notification.Id}", notification);
            });
        }
    }
}
