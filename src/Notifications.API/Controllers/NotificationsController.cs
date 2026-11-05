using Notifications.API.Constants;
using Microsoft.AspNetCore.Mvc;
using Notifications.API.DTOs;
using Notifications.API.Exceptions;
using Notifications.API.Models;
using Notifications.API.Services;
using Swashbuckle.AspNetCore.Filters;
using Notifications.API.Swagger;

namespace Notifications.API.Controllers;

/// <summary>
/// Gestiona las operaciones de notificaciones.
/// </summary>
[Produces("application/json")]
[Tags("Notificaciones")]
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    /// <summary>
    /// Inicializa el controlador de notificaciones.
    /// </summary>
    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Registra y simula el envío de una notificación.
    /// </summary>
    [HttpPost("send")]
    [SwaggerRequestExample(typeof(SendNotificationRequest), typeof(SendNotificationRequestExample))]
    [SwaggerResponseExample(StatusCodes.Status201Created, typeof(NotificationResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorExample))]
    [ProducesResponseType(typeof(Notification), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public IActionResult SendNotification([FromBody] SendNotificationRequest request)
    {
        var notification = _notificationService.SendNotification(request);
        return StatusCode(StatusCodes.Status201Created, notification);
    }

    /// <summary>
    /// Obtiene las notificaciones de un usuario.
    /// </summary>
    [HttpGet("{userId:guid}")]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(NotificationsResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorExample))]
    [ProducesResponseType(typeof(List<Notification>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public IActionResult GetNotificationsByUser(Guid userId)
    {
        var notifications = _notificationService.GetNotificationsByUser(userId);

        if (notifications.Count == 0)
        {
            throw new NotFoundException(
                ErrorCodes.NotificationsNotFound,
                "No se encontraron notificaciones para el usuario.");
        }

        return Ok(notifications);
    }
}
