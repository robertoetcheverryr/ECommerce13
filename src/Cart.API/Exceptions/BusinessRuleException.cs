namespace Cart.API.Exceptions;

/// <summary>
/// Regla de negocio violada. El HTTP status lo define el catálogo
/// (422 para stock insuficiente en Cart).
/// </summary>
public class BusinessRuleException : Exception
{
    /// <summary>
    /// Código del catálogo, por ejemplo CRT-003.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Detalle Problem Details de esta regla.
    /// </summary>
    public string Detail { get; }

    /// <summary>
    /// Código HTTP que debe devolver el handler.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Crea la excepción con el código, el mensaje, el detalle y el status.
    /// </summary>
    /// <param name="errorCode">Código del catálogo.</param>
    /// <param name="message">Mensaje de error de negocio.</param>
    /// <param name="detail">Detalle Problem Details.</param>
    /// <param name="statusCode">Código HTTP.</param>
    public BusinessRuleException(
        string errorCode,
        string message,
        string detail,
        int statusCode) : base(message)
    {
        ErrorCode = errorCode;
        Detail = detail;
        StatusCode = statusCode;
    }
}
