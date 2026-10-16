using Notifications.API.DTOs;
using Notifications.API.Models;

namespace Notifications.API.Services;

public interface INotificationService
{
    Notification SendNotification(SendNotificationRequest request);

    List<Notification> GetNotificationsByUser(Guid userId);
}