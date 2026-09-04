using Microsoft.AspNetCore.Diagnostics;
using Products.API.Exceptions;

namespace Products.API.ExceptionHandlers;

/// <summary>
/// Handler de BusinessRuleException.
/// El status sale de la excepción.
/// </summary>
public class BusinessRuleExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException ex)
            return false;

        var status = ex.StatusCode;
        context.Response.StatusCode = status;

        await context.Response.WriteAsJsonAsync(new
        {
            type = TypeFor(status),
            title = TitleFor(status),
            status,
            detail = ex.Detail,
            instance = context.Request.Path.Value,
            errorCode = ex.ErrorCode,
            errorMessage = ex.Message
        }, cancellationToken);

        return true;
    }

    private static string TypeFor(int status) => status switch
    {
        StatusCodes.Status401Unauthorized =>
            "https://tools.ietf.org/html/rfc7235#section-3.1",
        StatusCodes.Status403Forbidden =>
            "https://tools.ietf.org/html/rfc7231#section-6.5.3",
        StatusCodes.Status409Conflict =>
            "https://tools.ietf.org/html/rfc7231#section-6.5.9",
        StatusCodes.Status422UnprocessableEntity =>
            "https://tools.ietf.org/html/rfc4918#section-11.2",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.5.1"
    };

    private static string TitleFor(int status) => status switch
    {
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
        _ => "Bad Request"
    };
}