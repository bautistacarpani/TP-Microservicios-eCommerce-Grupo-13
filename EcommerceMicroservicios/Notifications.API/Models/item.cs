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

    /// <summary>
    /// Entidad que representa una notificación en el sistema.
    /// </summary>

    public record Notification
    {
        public string Id { get; init; }                  // Identificador único
        public string UsuarioId { get; init; }           // Usuario destinatario
        public string Mensaje { get; init; } = "";     // Máx. 500 caracteres
        public string Tipo { get; init; } = "";        // Email | Push | SMS
        public string Estado { get; set; } = "Pendiente"; // Pendiente | Enviada | Fallida
        public DateTime FechaEnvio { get; init; }      // Automático
        public bool Leida { get; set; }              // Si el usuario ha leído la notificación
    }

    // Request para crear
   
    /// <summary>
    /// Modelo requerido para registrar y enviar una nueva notificación a un usuario.
    /// </summary>
    /// 
    /// <summary> Contrato para enviar una nueva notificación. </summary>
/// <param name="UsuarioId" example="8AEE11A6-A2DA-49D4-975F-EB48568CB304">ID del usuario destinatario.</param>
/// <param name="Mensaje" example="¡Bienvenido a la plataforma!">Cuerpo de la notificación.</param>
/// <param name="Tipo" example="Email">Canal de envío (Email, SMS, Push).</param>

    public record CreateNotificationRequest(string UsuarioId,string Mensaje,string Tipo);


    // Request para actualizar estado
    public record UpdateNotificationStatusRequest(
        string Estado,
        bool Leida
    );

    // Response
    public record NotificationResponse(
        string Id,
        string UsuarioId,
        string Mensaje,
        string Tipo,
        string Estado,
        DateTime FechaEnvio,
        bool Leida
    );


  

}
