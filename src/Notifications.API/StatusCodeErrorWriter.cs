using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;
using Notifications.API.DTOs;

namespace Notifications.API;

// The framework answers some requests without reaching a controller or an
// IExceptionHandler: unknown route (404), wrong method (405) and a body that
// is not JSON (415). Those come back with an empty body. UseStatusCodePages
// calls this only when the response is an error with no body yet, so the
// handlers' responses are left alone.
// Section 4 has no catalog code for these cases, so errorCode stays empty
// instead of borrowing one that means something else.
/// <summary>
/// Writes the ErrorResponse contract for empty framework error responses (404, 405, 415).
/// </summary>
public static class StatusCodeErrorWriter
{
    /// <summary>
    /// Callback for UseStatusCodePages: builds and writes the contract body for the current status.
    /// </summary>
    public static Task WriteAsync(StatusCodeContext statusCodeContext)
    {
        var context = statusCodeContext.HttpContext;
        var status = context.Response.StatusCode;
        var (type, detail, errorMessage) = Describe(status);

        return context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Type = type,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Status = status,
            Detail = detail,
            Instance = context.Request.Path.Value ?? string.Empty,
            ErrorCode = string.Empty,
            ErrorMessage = errorMessage,
            CorrelationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? string.Empty
        }, context.RequestAborted);
    }

    private static (string Type, string Detail, string ErrorMessage) Describe(int status) => status switch
    {
        StatusCodes.Status404NotFound => (
            "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            "El recurso solicitado no fue encontrado.",
            "La ruta solicitada no existe."),
        StatusCodes.Status405MethodNotAllowed => (
            "https://tools.ietf.org/html/rfc7231#section-6.5.5",
            "El método HTTP no está permitido para este recurso.",
            "Método no permitido."),
        StatusCodes.Status415UnsupportedMediaType => (
            "https://tools.ietf.org/html/rfc7231#section-6.5.13",
            "El tipo de contenido no está soportado.",
            "El body debe enviarse como application/json."),
        _ => (
            "https://tools.ietf.org/html/rfc7231#section-6",
            "La solicitud no pudo procesarse.",
            ReasonPhrases.GetReasonPhrase(status))
    };
}
