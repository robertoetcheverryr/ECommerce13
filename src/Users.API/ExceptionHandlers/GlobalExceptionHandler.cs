using Microsoft.AspNetCore.Diagnostics;
using Users.API.DTOs;
using Users.API.Exceptions;

namespace Users.API.ExceptionHandlers;

/// <summary>
/// Handler genérico de último recurso.
/// Captura cualquier excepción no manejada y devuelve USR-006.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Unhandled error {ErrorCode}: {ErrorMessage}",
            ErrorCodes.USR_006,
            ErrorCodes.USR_006_Message);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = 500,
            Detail = "Ocurrió un error inesperado.",
            Instance = context.Request.Path.Value ?? string.Empty,
            ErrorCode = ErrorCodes.USR_006,
            ErrorMessage = ErrorCodes.USR_006_Message,
            CorrelationId = CorrelationId.Get(context)
        }, cancellationToken);

        return true;
    }
}
