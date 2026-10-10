using Cart.API.DTOs;
using Cart.API.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Cart.API.ExceptionHandlers;

/// <summary>
/// Handler de BusinessRuleException. El status sale de la excepción.
/// </summary>
public class BusinessRuleExceptionHandler : IExceptionHandler
{
    private readonly ILogger<BusinessRuleExceptionHandler> _logger;

    /// <summary>
    /// Crea el handler con el logger del request.
    /// </summary>
    /// <param name="logger">Logger de la API.</param>
    public BusinessRuleExceptionHandler(ILogger<BusinessRuleExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Escribe el Problem Details si la excepción es BusinessRuleException.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException ex)
            return false;

        _logger.LogWarning("Business error {ErrorCode}: {ErrorMessage}", ex.ErrorCode, ex.Message);

        var status = ex.StatusCode;
        context.Response.StatusCode = status;

        await context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Type = TypeFor(status),
            Title = TitleFor(status),
            Status = status,
            Detail = ex.Detail,
            Instance = context.Request.Path.Value ?? string.Empty,
            ErrorCode = ex.ErrorCode,
            ErrorMessage = ex.Message,
            CorrelationId = CorrelationId.Get(context)
        }, cancellationToken);

        return true;
    }

    private static string TypeFor(int status) => status switch
    {
        StatusCodes.Status409Conflict =>
            "https://tools.ietf.org/html/rfc7231#section-6.5.9",
        StatusCodes.Status422UnprocessableEntity =>
            "https://tools.ietf.org/html/rfc4918#section-11.2",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.5.1"
    };

    private static string TitleFor(int status) => status switch
    {
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
        _ => "Bad Request"
    };
}
