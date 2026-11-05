using Cart.API.DTOs;
using Cart.API.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace Cart.API.ExceptionHandlers;

/// <summary>
/// Handler genérico. Responde CRT-005 y no expone el stack.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly ErrorHandlingOptions _options;

    /// <summary>
    /// Crea el handler con el logger del request y la sección ErrorHandling.
    /// </summary>
    /// <param name="logger">Logger de la API.</param>
    /// <param name="options">Define si detail lleva exception.Message (Spec 5.2).</param>
    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IOptions<ErrorHandlingOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Escribe el Problem Details de error interno.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Unhandled error {ErrorCode}: {ErrorMessage}",
            ErrorCodes.CRT_005,
            ErrorCodes.CRT_005_Message);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = 500,
            Detail = _options.IncludeExceptionDetails
                ? exception.Message
                : "Ocurrió un error inesperado.",
            Instance = context.Request.Path.Value ?? string.Empty,
            ErrorCode = ErrorCodes.CRT_005,
            ErrorMessage = ErrorCodes.CRT_005_Message,
            CorrelationId = CorrelationId.Get(context)
        }, cancellationToken);

        return true;
    }
}
