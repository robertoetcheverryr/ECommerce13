namespace Notifications.API.DTOs;

/// <summary>
/// Representa la respuesta de error de la API.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// URI que identifica el tipo de problema.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Título breve del error.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Código HTTP correspondiente al error.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Descripción general del problema.
    /// </summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>
    /// Ruta de la solicitud que produjo el error.
    /// </summary>
    public string Instance { get; set; } = string.Empty;

    /// <summary>
    /// Código de error propio de Notifications API.
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje descriptivo asociado al código de error.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}