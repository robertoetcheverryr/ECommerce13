using System.Net;

namespace Cart.API.Services;

/// <summary>
/// Cliente HTTP de Products.API. Lee el stock con GET /api/products/{id}.
/// </summary>
public class ProductCatalogClient : IProductCatalog
{
    private readonly HttpClient _http;

    /// <summary>
    /// Crea el cliente con el HttpClient nombrado por IHttpClientFactory.
    /// </summary>
    /// <param name="http">Cliente con base address de Products.</param>
    public ProductCatalogClient(HttpClient http)
    {
        _http = http;
    }

    /// <inheritdoc />
    public async Task<ProductStock?> GetAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"/api/products/{productId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        // Any other failure is unexpected. The global handler turns it into CRT-005.
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductPayload>(cancellationToken);
        return new ProductStock { Stock = product?.Stock ?? 0 };
    }

    private sealed class ProductPayload
    {
        public int Stock { get; set; }
    }
}
