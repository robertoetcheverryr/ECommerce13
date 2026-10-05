namespace Notifications.API.Exceptions;

/// <summary>
/// Excepción utilizada cuando los datos recibidos no cumplen las reglas de validación.
/// </summary>
public class ValidationException : Exception
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
    public ValidationException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}