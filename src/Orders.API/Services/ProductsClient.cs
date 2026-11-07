using System.Net;
using System.Net.Http.Json;
using Orders.API.DTOs;

namespace Orders.API.Services;

public class ProductsClient : IProductsClient
{
    private readonly HttpClient _httpClient;

    public ProductsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

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