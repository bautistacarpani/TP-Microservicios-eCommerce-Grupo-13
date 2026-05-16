using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;


namespace Notifications.API.Models
{


    // ── Entidad principal ─────────────────────────────────────
    public record Notification
    {
        public Guid Id { get; init; }                  // Identificador único
        public Guid UsuarioId { get; init; }           // Usuario destinatario
        public string Mensaje { get; init; } = "";     // Máx. 500 caracteres
        public string Tipo { get; init; } = "";        // Email | Push | SMS
        public string Estado { get; set; } = "Pendiente"; // Pendiente | Enviada | Fallida
        public DateTime FechaEnvio { get; init; }      // Automático
    }

    // Request para crear
   
    public record CreateNotificationRequest(
    [Required][DefaultValue("00000000-0000-0000-0000-000000000000")] Guid UsuarioId, // Reemplazar por un Guid real al probar
    [Required][DefaultValue("¡Tu registro en el eCommerce fue exitoso!")] string Mensaje,
    [Required][DefaultValue("Email")] string Tipo
);


    // Request para actualizar estado
    public record UpdateNotificationStatusRequest(
        string Estado
    );



    // Response
    public record NotificationResponse(
        Guid Id,
        Guid UsuarioId,
        string Mensaje,
        string Tipo,
        string Estado,
        DateTime FechaEnvio
    );
}
