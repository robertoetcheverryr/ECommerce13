using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using Orders.API.DTOs;
using Orders.API.Exceptions;

namespace Orders.API.ExceptionHandlers;

/// <summary>
/// Maneja globalmente las excepciones producidas por Orders API.
/// </summary>
public class OrdersExceptionHandler : IExceptionHandler
{
    private readonly ILogger<OrdersExceptionHandler> _logger;
    private readonly ErrorHandlingOptions _options;

    /// <summary>
    /// Inicializa el manejador global de excepciones.
    /// </summary>
    /// <param name="logger">Servicio de registro de eventos.</param>
    /// <param name="options">Define si detail lleva exception.Message para errores inesperados.</param>
    public OrdersExceptionHandler(
        ILogger<OrdersExceptionHandler> logger,
        IOptions<ErrorHandlingOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Procesa una excepción y genera una respuesta de error estandarizada.
    /// </summary>
    /// <param name="context">Contexto HTTP de la solicitud.</param>
    /// <param name="exception">Excepción que se produjo.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>Indica si la excepción fue manejada.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        string errorCode;
        string errorMessage;
        int statusCode;
        string detail;

        if (exception is OrderException orderException)
        {
            errorCode = orderException.ErrorCode;
            errorMessage = orderException.Message;
            statusCode = orderException.StatusCode;
            detail = errorMessage;

            _logger.LogWarning(
                "Error {ErrorCode}: {ErrorMessage}",
                errorCode,
                errorMessage);
        }
        else
        {
            errorCode = ErrorCodes.ORD_007;
            errorMessage = ErrorCodes.ORD_007_Message;
            statusCode = StatusCodes.Status500InternalServerError;
            detail = _options.IncludeExceptionDetails
                ? exception.Message
                : errorMessage;

            _logger.LogError(
                exception,
                "Error inesperado al procesar una solicitud de órdenes.");
        }

        var correlationId = context.Items["CorrelationId"]?.ToString()
                            ?? context.TraceIdentifier;

        context.Response.StatusCode = statusCode;
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        await context.Response.WriteAsJsonAsync(
            new ErrorResponse
            {
                Type = TypeFor(statusCode),
                Title = statusCode switch
                {
                    400 => "Bad Request",
                    404 => "Not Found",
                    409 => "Conflict",
                    422 => "Unprocessable Entity",
                    _ => "Internal Server Error"
                },
                Status = statusCode,
                Detail = detail,
                Instance = context.Request.Path,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                CorrelationId = correlationId
            },
            cancellationToken);

        return true;
    }

    // URI RFC correspondiente a cada status code del contrato de errores.
    private static string TypeFor(int status) => status switch
    {
        StatusCodes.Status400BadRequest =>
            "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        StatusCodes.Status404NotFound =>
            "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        StatusCodes.Status409Conflict =>
            "https://tools.ietf.org/html/rfc7231#section-6.5.9",
        StatusCodes.Status422UnprocessableEntity =>
            "https://tools.ietf.org/html/rfc4918#section-11.2",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
    };
}
