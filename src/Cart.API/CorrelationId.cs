namespace Cart.API;

// This helper should live under Infrastructure/ once we extract cross-cutting
// types. Left next to Program.cs for now because the TP project layout does
// not include that folder.

/// <summary>
/// Identificador de correlación por request.
/// Reutiliza X-Correlation-Id si viene en el request; si no, genera un Guid.
/// Se guarda en HttpContext.Items para que handlers y logs usen el mismo valor.
/// </summary>
public static class CorrelationId
{
    /// <summary>
    /// Nombre del header de correlación.
    /// </summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>
    /// Clave de HttpContext.Items.
    /// </summary>
    public const string ItemKey = "CorrelationId";

    /// <summary>
    /// Nombre de la propiedad de log.
    /// </summary>
    public const string LogProperty = "CorrelationId";

    /// <summary>
    /// Toma el header entrante o genera un Guid.
    /// </summary>
    /// <param name="request">Request actual.</param>
    /// <returns>Identificador de correlación.</returns>
    public static string Resolve(HttpRequest request)
    {
        if (request.Headers.TryGetValue(HeaderName, out var incoming))
        {
            var value = incoming.ToString().Trim();
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Lee el identificador ya asignado al request.
    /// </summary>
    /// <param name="context">Contexto HTTP.</param>
    /// <returns>El identificador, o vacío si todavía no se asignó.</returns>
    public static string Get(HttpContext context)
    {
        if (context.Items.TryGetValue(ItemKey, out var stored) && stored is string value)
            return value;

        return string.Empty;
    }

    /// <summary>
    /// Guarda el identificador y lo copia al header de respuesta.
    /// </summary>
    /// <param name="context">Contexto HTTP.</param>
    /// <param name="correlationId">Identificador de correlación.</param>
    public static void Assign(HttpContext context, string correlationId)
    {
        context.Items[ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });
    }
}
