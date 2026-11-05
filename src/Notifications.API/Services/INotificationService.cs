using Notifications.API.DTOs;
using Notifications.API.Models;

namespace Notifications.API.Services;

/// <summary>
/// Define las operaciones disponibles para gestionar notificaciones.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Registra una nueva notificación.
    /// </summary>
    /// <param name="request">Datos de la notificación.</param>
    /// <returns>Notificación registrada.</returns>
    Notification SendNotification(SendNotificationRequest request);

    /// <summary>
    /// Obtiene las notificaciones asociadas a un usuario.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <returns>Lista de notificaciones del usuario.</returns>
    List<Notification> GetNotificationsByUser(Guid userId);
}