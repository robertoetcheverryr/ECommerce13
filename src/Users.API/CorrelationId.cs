namespace Users.API;

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
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";
    public const string LogProperty = "CorrelationId";

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

    public static string Get(HttpContext context)
    {
        if (context.Items.TryGetValue(ItemKey, out var stored) && stored is string value)
            return value;

        return string.Empty;
    }

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
