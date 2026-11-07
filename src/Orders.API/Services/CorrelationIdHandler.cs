namespace Orders.API.Services;

/// <summary>
/// Copia X-Correlation-Id del request entrante a las llamadas HTTP salientes.
/// </summary>
public class CorrelationIdHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Crea el handler con acceso al request actual.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor del contexto HTTP.</param>
    public CorrelationIdHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Agrega el header con el correlation id que ya calculó el middleware.
    /// </summary>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;

        if (context is not null)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString();

            if (!string.IsNullOrEmpty(correlationId))
                request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
