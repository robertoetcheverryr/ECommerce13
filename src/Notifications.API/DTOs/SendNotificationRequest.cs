using System.ComponentModel.DataAnnotations; //importar herramientas de c#

namespace Notifications.API.DTOs;

/// <summary>
/// Datos necesarios para registrar y enviar una notificación.
/// </summary>
public class SendNotificationRequest
{
    /// <summary>
    /// Identificador del usuario destinatario.
    /// </summary>
    [Required] //debe ser obligatorio este dato
    public Guid UsuarioId { get; set; }

    /// <summary>
    /// Mensaje que se enviará al usuario.
    /// </summary>
    [Required]
    [MaxLength(500)] //para limitar la longitud maxima
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de notificación: Email, Push o SMS.
    /// </summary>
    [Required]
    public string Tipo { get; set; } = string.Empty;
}