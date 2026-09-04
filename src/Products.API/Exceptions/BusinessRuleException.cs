namespace Products.API.Exceptions;

/// <summary>
/// Regla de negocio violada. El HTTP status lo define el catálogo
/// (409 en Products; 401/403/422 en otros servicios).
/// </summary>
public class BusinessRuleException : Exception
{
    public string ErrorCode { get; }

    public string Detail { get; }

    public int StatusCode { get; }

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