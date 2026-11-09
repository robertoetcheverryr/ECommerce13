
using Notifications.API.DTOs;
using Notifications.API.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Notifications.API.Swagger;

/// <summary>
/// Proporciona ejemplos de requests y responses para Swagger.
/// </summary>
public class SendNotificationRequestExample : IExamplesProvider<SendNotificationRequest>
{
    /// <summary>
    /// Obtiene el ejemplo correspondiente.
    /// </summary>
    public SendNotificationRequest GetExamples() => new()
    {
        UsuarioId = Guid.Parse("15cd5d40-7725-4eba-a30f-fae871b0fe24"),
        Mensaje = "Su pedido fue enviado.",
        Tipo = "Email"
    };
}

/// <summary>
/// Proporciona un ejemplo de notificación.
/// </summary>
public class NotificationResponseExample : IExamplesProvider<Notification>
{
    /// <summary>
    /// Obtiene el ejemplo correspondiente.
    /// </summary>
    public Notification GetExamples() => new()
    {
        Id = Guid.Parse("a1b2c3d4-1234-4567-8901-123456789abc"),
        UsuarioId = Guid.Parse("15cd5d40-7725-4eba-a30f-fae871b0fe24"),
        Mensaje = "Su pedido fue enviado.",
        Tipo = "Email",
        Estado = "Enviada",
        FechaEnvio = DateTime.UtcNow
    };
}

/// <summary>
/// Proporciona un ejemplo de lista de notificaciones.
/// </summary>
public class NotificationsResponseExample : IExamplesProvider<List<Notification>>
{
    /// <summary>
    /// Obtiene el ejemplo correspondiente.
    /// </summary>
    public List<Notification> GetExamples() =>
    [
        new NotificationResponseExample().GetExamples()
    ];
}

/// <summary>
/// Proporciona un ejemplo de error de validación.
/// </summary>
public class ValidationErrorExample : IExamplesProvider<object>
{
    /// <summary>
    /// Obtiene el ejemplo correspondiente.
    /// </summary>
    public object GetExamples() => new
    {
        type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        title = "Bad Request",
        status = 400,
        detail = "Los datos de la notificación son inválidos.",
        instance = "/api/notifications/send",
        errorCode = "NTF-002",
        errorMessage = "El tipo debe ser Email, Push o SMS.",
        correlationId = "example-correlation-id"
    };
}

/// <summary>
/// Proporciona un ejemplo de error de recurso no encontrado.
/// </summary>
public class NotFoundErrorExample : IExamplesProvider<object>
{
    /// <summary>
    /// Obtiene el ejemplo correspondiente.
    /// </summary>
    public object GetExamples() => new
    {
        type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        title = "Not Found",
        status = 404,
        detail = "El recurso solicitado no fue encontrado.",
        instance = "/api/notifications/15cd5d40-7725-4eba-a30f-fae871b0fe24",
        errorCode = "NTF-003",
        errorMessage = "No se encontraron notificaciones para el usuario.",
        correlationId = "example-correlation-id"
    };
}

/// <summary>
/// Proporciona un ejemplo de error de usuario destinatario inexistente.
/// </summary>
public class UserNotFoundErrorExample : IExamplesProvider<object>
{
    /// <summary>
    /// Obtiene el ejemplo correspondiente.
    /// </summary>
    public object GetExamples() => new
    {
        type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        title = "Not Found",
        status = 404,
        detail = "El recurso solicitado no fue encontrado.",
        instance = "/api/notifications/send",
        errorCode = "NTF-001",
        errorMessage = "El usuario destinatario no fue encontrado.",
        correlationId = "example-correlation-id"
    };
}

/// <summary>
/// Proporciona un ejemplo de error interno del servidor.
/// </summary>
public class InternalServerErrorExample : IExamplesProvider<object>
{
    /// <summary>
    /// Obtiene el ejemplo correspondiente.
    /// </summary>
    public object GetExamples() => new
    {
        type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
        title = "Internal Server Error",
        status = 500,
        detail = "Error interno al procesar la notificación.",
        instance = "/api/notifications/send",
        errorCode = "NTF-004",
        errorMessage = "Error interno al procesar la notificación.",
        correlationId = "example-correlation-id"
    };
}
