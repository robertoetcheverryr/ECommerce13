using Microsoft.AspNetCore.Diagnostics;
using Products.API.Exceptions;

namespace Products.API.ExceptionHandlers;

/// <summary>
/// Handler genérico de último recurso.
/// Captura cualquier excepción no manejada y devuelve PRD-005.
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
            ErrorCodes.PRD_005,
            ErrorCodes.PRD_005_Message);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // TODO(correlation-id): agregar correlationId al body (spec 5.5)
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = "Internal Server Error",
            status = 500,
            detail = "Ocurrió un error inesperado.",
            instance = context.Request.Path.Value,
            errorCode = ErrorCodes.PRD_005,
            errorMessage = ErrorCodes.PRD_005_Message
        }, cancellationToken);

        return true;
    }
}