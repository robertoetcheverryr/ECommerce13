using Microsoft.AspNetCore.Mvc;
using Notifications.API.DTOs;
using Notifications.API.Exceptions;
using Notifications.API.Models;
using Notifications.API.Services;

namespace Notifications.API.Controllers;

/// <summary>
/// Gestiona las operaciones de notificaciones.
/// </summary>
[Tags("Notificaciones")]
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
    [ProducesResponseType(typeof(Notification), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult SendNotification([FromBody] SendNotificationRequest request)
    {
        var notification = _notificationService.SendNotification(request);

        return StatusCode(StatusCodes.Status201Created, notification);
    }

    /// <summary>
    /// Obtiene las notificaciones de un usuario.
    /// </summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(List<Notification>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetNotificationsByUser(Guid userId)
    {
        var notifications = _notificationService.GetNotificationsByUser(userId);

        if (notifications.Count == 0)
        {
            throw new NotFoundException(
                "NTF-003",
                "No se encontraron notificaciones para el usuario.");
        }

        return Ok(notifications);
    }
}