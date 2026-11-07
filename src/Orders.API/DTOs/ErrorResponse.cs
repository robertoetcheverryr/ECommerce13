namespace Orders.API.DTOs;

/// <summary>
/// Representa la estructura de una respuesta de error de la API.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// URI que identifica el tipo de error HTTP.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Descripción general del error.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Código de estado HTTP.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Detalle del error ocurrido.
    /// </summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>
    /// Ruta de la solicitud que produjo el error.
    /// </summary>
    public string Instance { get; set; } = string.Empty;

    /// <summary>
    /// Código identificador del error de negocio.
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje asociado al código de error.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Identificador de correlación de la solicitud.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
}