namespace Users.API.Exceptions;

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
