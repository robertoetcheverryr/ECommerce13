using Notifications.API.DTOs;
using Notifications.API.Models;

namespace Notifications.API.Services;

public class NotificationService : INotificationService
{
    private readonly List<Notification> _notifications = new();

    public Notification SendNotification(SendNotificationRequest request)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UsuarioId = request.UsuarioId,
            Mensaje = request.Mensaje,
            Tipo = request.Tipo,
            Estado = "Enviada",
            FechaEnvio = DateTime.Now
        };

        _notifications.Add(notification);

        return notification;
    }

    public List<Notification> GetNotificationsByUser(Guid userId)
    {
        return _notifications
            .Where(n => n.UsuarioId == userId)
            .ToList();
    }
}