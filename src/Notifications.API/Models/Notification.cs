namespace Notifications.API.Models;

/// <summary>
/// Representa una notificación enviada a un usuario.
/// </summary>
public class Notification
{
    /// <summary>
    /// Identificador único de la notificación.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identificador del usuario destinatario.
    /// </summary>
    public Guid UsuarioId { get; set; }

    /// <summary>
    /// Mensaje de la notificación.
    /// </summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de notificación: Email, Push o SMS.
    /// </summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>
    /// Estado de la notificación: Pendiente, Enviada o Fallida.
    /// </summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora en que se envió la notificación.
    /// </summary>
    public DateTime FechaEnvio { get; set; }
}