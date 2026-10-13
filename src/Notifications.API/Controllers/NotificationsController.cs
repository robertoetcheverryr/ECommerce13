using Microsoft.AspNetCore.Mvc;
using Notifications.API.DTOs;
using Notifications.API.Services;

namespace Notifications.API.Controllers;

/// <summary>
/// Gestiona las operaciones de notificaciones.
/// </summary>
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Registra y simula el envío de una notificación.
    /// </summary>
    [HttpPost("send")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult SendNotification([FromBody] SendNotificationRequest request)
    {
        var notification = _notificationService.SendNotification(request);

        return StatusCode(StatusCodes.Status201Created, notification);
    }
}