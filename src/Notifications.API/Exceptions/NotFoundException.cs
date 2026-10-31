namespace Notifications.API.Exceptions;

/// <summary>
/// Excepción utilizada cuando no se encuentra un recurso solicitado.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Código de error asociado a la excepción.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Inicializa una nueva instancia de la excepción.
    /// </summary>
    /// <param name="errorCode">Código de error de la aplicación.</param>
    /// <param name="message">Mensaje descriptivo del error.</param>
    public NotFoundException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}