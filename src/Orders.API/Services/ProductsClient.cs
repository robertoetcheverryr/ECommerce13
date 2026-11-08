using System.Net;
using System.Net.Http.Json;
using Orders.API.DTOs;

namespace Orders.API.Services;

/// <summary>
/// Implementa la comunicación HTTP con Products API.
/// </summary>
public class ProductsClient : IProductsClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Inicializa el cliente HTTP de Products API.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado para Products API.</param>
    public ProductsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Obtiene un producto mediante su identificador.
    /// </summary>
    /// <param name="id">Identificador del producto.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>
    /// El producto encontrado o null si Products API responde 404.
    /// </returns>
    public async Task<ProductResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/products/{id}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductResponse>(
            cancellationToken: cancellationToken);
    }
}