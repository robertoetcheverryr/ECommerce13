using Microsoft.AspNetCore.Diagnostics;
using Notifications.API.Exceptions;

namespace Notifications.API.ExceptionHandlers;

/// <summary>
/// Maneja las excepciones de tipo BusinessRuleException.
/// </summary>
public class BusinessRuleExceptionHandler : IExceptionHandler
{
    /// <summary>
    /// Intenta manejar una excepción relacionada con una regla de negocio.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException ex)
            return false;

        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            title = "Bad Request",
            status = StatusCodes.Status400BadRequest,
            detail = "Los datos de la solicitud no son válidos.",
            instance = context.Request.Path.Value,
            errorCode = ex.ErrorCode,
            errorMessage = ex.Message,
            correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        }, cancellationToken: cancellationToken);
        return true;
    }
}