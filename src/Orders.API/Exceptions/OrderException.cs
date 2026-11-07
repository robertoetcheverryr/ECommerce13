namespace Orders.API.Exceptions;

/// <summary>
/// Representa una excepción personalizada de Orders API.
/// </summary>
public class OrderException : Exception
{
    /// <summary>
    /// Código identificador del error.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Código de estado HTTP asociado al error.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Inicializa una excepción personalizada de órdenes.
    /// </summary>
    /// <param name="errorCode">Código identificador del error.</param>
    /// <param name="message">Descripción del error.</param>
    /// <param name="statusCode">Código de estado HTTP correspondiente.</param>
    public OrderException(
        string errorCode,
        string message,
        int statusCode) : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}