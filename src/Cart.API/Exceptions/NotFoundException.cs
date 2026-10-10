namespace Cart.API.Exceptions;

/// <summary>
/// El recurso pedido no existe.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Código del catálogo, por ejemplo CRT-001.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Crea la excepción con el código y el mensaje del catálogo.
    /// </summary>
    /// <param name="errorCode">Código del catálogo.</param>
    /// <param name="message">Mensaje de error de negocio.</param>
    public NotFoundException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
