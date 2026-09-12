using Microsoft.AspNetCore.Diagnostics;
using Users.API.DTOs;
using Users.API.Exceptions;

namespace Users.API.ExceptionHandlers;

/// <summary>
/// Handler de ValidationException. Devuelve HTTP 400 con el contrato de error.
/// </summary>
public class ValidationExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ValidationExceptionHandler> _logger;

    public ValidationExceptionHandler(ILogger<ValidationExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException ex)
            return false;

        _logger.LogWarning("Business error {ErrorCode}: {ErrorMessage}", ex.ErrorCode, ex.Message);

        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        await context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = 400,
            Detail = "Los datos enviados no son válidos.",
            Instance = context.Request.Path.Value ?? string.Empty,
            ErrorCode = ex.ErrorCode,
            ErrorMessage = ex.Message,
            CorrelationId = CorrelationId.Get(context)
        }, cancellationToken);

        return true;
    }
}
