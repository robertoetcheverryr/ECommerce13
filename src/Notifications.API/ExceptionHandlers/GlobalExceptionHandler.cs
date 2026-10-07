using Microsoft.AspNetCore.Diagnostics;

namespace Notifications.API.ExceptionHandlers;

/// <summary>
/// Maneja las excepciones no contempladas por los handlers específicos.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    /// <summary>
    /// Maneja una excepción inesperada.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = "Internal Server Error",
            status = StatusCodes.Status500InternalServerError,
            detail = "Error interno al procesar la notificación.",
            instance = context.Request.Path.Value,
            errorCode = "NTF-004",
            errorMessage = "Error interno al procesar la notificación.",
            correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        }, cancellationToken: cancellationToken);

        return true;
    }
}