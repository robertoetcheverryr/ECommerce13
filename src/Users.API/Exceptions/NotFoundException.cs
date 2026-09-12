namespace Users.API.Exceptions;

/// <summary>
/// Recurso no encontrado. Se traduce en HTTP 404.
/// </summary>
public class NotFoundException : Exception
{
    public string ErrorCode { get; }

    public NotFoundException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
