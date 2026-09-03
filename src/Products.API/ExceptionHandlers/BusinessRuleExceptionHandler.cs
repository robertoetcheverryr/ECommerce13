using Microsoft.AspNetCore.Diagnostics;
using Products.API.Exceptions;

namespace Products.API.ExceptionHandlers;

public class BusinessRuleExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException ex)
            return false;

        context.Response.StatusCode = StatusCodes.Status409Conflict;

        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.9",
            title = "Conflict",
            status = 409,
            detail = ex.Detail,
            instance = context.Request.Path.Value,
            errorCode = ex.ErrorCode,
            errorMessage = ex.Message
        }, cancellationToken);

        return true;
    }
}