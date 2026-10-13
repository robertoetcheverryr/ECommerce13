namespace Cart.API.Exceptions;

/// <summary>
/// Los datos enviados no cumplen el formato pedido.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Código del catálogo, por ejemplo CRT-004.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Crea la excepción con el código y el mensaje del catálogo.
    /// </summary>
    /// <param name="errorCode">Código del catálogo.</param>
    /// <param name="message">Mensaje de error de negocio.</param>
    public ValidationException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
