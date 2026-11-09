namespace Notifications.API.Constants;

/// <summary>
/// Centraliza los códigos de error de la API de notificaciones.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Código utilizado para usuario inexistente.
    /// </summary>
    public const string UserNotFound = "NTF-001";
    /// <summary>
    /// Código utilizado para errores de validación.
    /// </summary>
    public const string ValidationError = "NTF-002";

    /// <summary>
    /// Código utilizado cuando no se encuentran notificaciones.
    /// </summary>
    public const string NotificationsNotFound = "NTF-003";

    /// <summary>
    /// Código utilizado para errores internos del servidor.
    /// </summary>
    public const string InternalServerError = "NTF-004";
}