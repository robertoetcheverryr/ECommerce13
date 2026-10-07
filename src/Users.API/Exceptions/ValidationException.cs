namespace Users.API.Exceptions;

/// <summary>
/// Datos de entrada inválidos. Se traduce en HTTP 400.
/// </summary>
public class ValidationException : Exception
{
    public string ErrorCode { get; }

    public ValidationException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
