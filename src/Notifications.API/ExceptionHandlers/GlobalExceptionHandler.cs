using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using Serilog;
using Notifications.API.Constants;

namespace Notifications.API.ExceptionHandlers;

/// <summary>
/// Maneja las excepciones no contempladas por los handlers específicos.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ErrorHandlingOptions _options;

    public GlobalExceptionHandler(IOptions<ErrorHandlingOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Maneja una excepción inesperada.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        Log.Error(
            exception,
            "NTF-004: Error interno al procesar la notificación. Endpoint: {Endpoint}",
            context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = "Internal Server Error",
            status = StatusCodes.Status500InternalServerError,
            detail = _options.IncludeExceptionDetails
                ? exception.Message
                : "Error interno al procesar la notificación.",
            instance = context.Request.Path.Value,
            errorCode = ErrorCodes.InternalServerError,
            errorMessage = "Error interno al procesar la notificación.",
            correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        }, cancellationToken: cancellationToken);

        return true;
    }
}