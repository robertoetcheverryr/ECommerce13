using Microsoft.AspNetCore.Diagnostics;
using Orders.API.DTOs;
using Orders.API.Exceptions;

namespace Orders.API.ExceptionHandlers;

/// <summary>
/// Maneja globalmente las excepciones producidas por Orders API.
/// </summary>
public class OrdersExceptionHandler : IExceptionHandler
{
    private readonly ILogger<OrdersExceptionHandler> _logger;

    /// <summary>
    /// Inicializa el manejador global de excepciones.
    /// </summary>
    /// <param name="logger">Servicio de registro de eventos.</param>
    public OrdersExceptionHandler(ILogger<OrdersExceptionHandler> logger)
    {
        _logger = logger;
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

        if (exception is OrderException orderException)
        {
            errorCode = orderException.ErrorCode;
            errorMessage = orderException.Message;
            statusCode = orderException.StatusCode;

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
                Type = $"https://httpstatuses.com/{statusCode}",
                Title = statusCode switch
                {
                    400 => "Bad Request",
                    404 => "Not Found",
                    409 => "Conflict",
                    422 => "Unprocessable Entity",
                    _ => "Internal Server Error"
                },
                Status = statusCode,
                Detail = errorMessage,
                Instance = context.Request.Path,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                CorrelationId = correlationId
            },
            cancellationToken);

        return true;
    }
}
