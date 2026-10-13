namespace Cart.API.Services;

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
    /// Agrega el header si el request actual ya tiene correlation id.
    /// </summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is not null)
        {
            var correlationId = CorrelationId.Get(context);
            if (!string.IsNullOrEmpty(correlationId))
                request.Headers.TryAddWithoutValidation(CorrelationId.HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
