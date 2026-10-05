using Microsoft.AspNetCore.Diagnostics;
using Notifications.API.Exceptions; //para llamar lo del error codeado anteriormente en exceptions

namespace Notifications.API.ExceptionHandlers;

/// <summary>
/// Maneja las excepciones de tipo NotFoundException.
/// </summary>
public class NotFoundExceptionHandler : IExceptionHandler
{
    /// <summary>
    /// Intenta manejar una excepción de recurso no encontrado.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not NotFoundException ex)
            return false;

        context.Response.StatusCode = StatusCodes.Status404NotFound;

        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.4", //para identificar tipo de problemas
            title = "Not Found",
            status = StatusCodes.Status404NotFound,
            detail = "El recurso solicitado no fue encontrado.",
            instance = context.Request.Path.Value,
            errorCode = ex.ErrorCode,
            errorMessage = ex.Message
        }, cancellationToken: cancellationToken);

        return true;
    }
}