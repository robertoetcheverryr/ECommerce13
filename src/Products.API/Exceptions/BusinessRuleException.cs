namespace Products.API.Exceptions;

public class BusinessRuleException : Exception
{
    public string ErrorCode { get; }

    public string Detail { get; }

    public BusinessRuleException(string errorCode, string message, string detail) : base(message)
    {
        ErrorCode = errorCode;
        Detail = detail;
    }
}