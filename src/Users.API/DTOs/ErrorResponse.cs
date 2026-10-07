namespace Users.API.DTOs;

/// <summary>
/// Contrato de error de la API (Problem Details extendido con errorCode, errorMessage y correlationId).
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// URI de referencia del tipo de error HTTP (RFC).
    /// </summary>
    /// <example>https://tools.ietf.org/html/rfc7231#section-6.5.4</example>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Título corto del error.
    /// </summary>
    /// <example>Not Found</example>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Código de estado HTTP.
    /// </summary>
    /// <example>404</example>
    public int Status { get; set; }

    /// <summary>
    /// Detalle legible del error.
    /// </summary>
    /// <example>El recurso solicitado no fue encontrado.</example>
    public string Detail { get; set; } = string.Empty;

    /// <summary>
    /// Ruta de la request que originó el error.
    /// </summary>
    /// <example>/api/users/register</example>
    public string Instance { get; set; } = string.Empty;

    /// <summary>
    /// Código de error propio del catálogo del microservicio (ej: USR-001).
    /// </summary>
    /// <example>USR-001</example>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje de error de negocio definido en el catálogo.
    /// </summary>
    /// <example>El email 'maria@email.com' ya está registrado.</example>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Identificador de correlación del request. Coincide con el header X-Correlation-Id.
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public string CorrelationId { get; set; } = string.Empty;
}
