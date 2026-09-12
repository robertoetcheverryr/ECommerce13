using Microsoft.AspNetCore.Diagnostics;
using Users.API.DTOs;
using Users.API.Exceptions;

namespace Users.API.ExceptionHandlers;

/// <summary>
/// Handler de NotFoundException. Devuelve HTTP 404 con el contrato de error.
/// </summary>
public class NotFoundExceptionHandler : IExceptionHandler
{
    private readonly ILogger<NotFoundExceptionHandler> _logger;

    public NotFoundExceptionHandler(ILogger<NotFoundExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not NotFoundException ex)
            return false;

        _logger.LogWarning("Business error {ErrorCode}: {ErrorMessage}", ex.ErrorCode, ex.Message);

        context.Response.StatusCode = StatusCodes.Status404NotFound;

        await context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "Not Found",
            Status = 404,
            Detail = "El recurso solicitado no fue encontrado.",
            Instance = context.Request.Path.Value ?? string.Empty,
            ErrorCode = ex.ErrorCode,
            ErrorMessage = ex.Message,
            CorrelationId = CorrelationId.Get(context)
        }, cancellationToken);

        return true;
    }
}
