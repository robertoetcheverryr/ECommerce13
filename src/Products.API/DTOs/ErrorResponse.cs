namespace Products.API.DTOs;

/// <summary>
/// Contrato de error de la API (Problem Details extendido con errorCode y errorMessage).
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// URI de referencia del tipo de error HTTP (RFC).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Título corto del error.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Código de estado HTTP.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Detalle legible del error.
    /// </summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>
    /// Ruta de la request que originó el error.
    /// </summary>
    public string Instance { get; set; } = string.Empty;

    /// <summary>
    /// Código de error propio del catálogo del microservicio (ej: PRD-001).
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje de error de negocio definido en el catálogo.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}