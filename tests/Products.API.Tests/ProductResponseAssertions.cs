using System.Net;
using System.Text.Json;
using FluentAssertions;
using Products.API.Models;

namespace Products.API.Tests;

/// <summary>
/// Shared assertions for product success payloads.
/// Checks HTTP status and the JSON key set before deserializing.
/// </summary>
internal static class ProductResponseAssertions
{
    private static readonly string[] ProductKeys =
    [
        "id", "nombre", "descripcion", "precio", "stock", "categoria", "fechaCreacion"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static async Task<Product> AssertProductOk(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await ReadProduct(response);
    }

    internal static async Task<Product> AssertProductCreated(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await ReadProduct(response);
    }

    internal static async Task<List<Product>> AssertProductListOk(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            item.ValueKind.Should().Be(JsonValueKind.Object);
            item.EnumerateObject()
                .Select(property => property.Name)
                .Should()
                .Contain(ProductKeys);
        }

        var products = JsonSerializer.Deserialize<List<Product>>(json, JsonOptions);
        products.Should().NotBeNull();
        return products;
    }

    internal static async Task AssertNoContent(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().BeEmpty();
    }

    private static async Task<Product> ReadProduct(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var body = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        body.Should().NotBeNull();
        body.Should().ContainKeys(ProductKeys);

        var product = JsonSerializer.Deserialize<Product>(json, JsonOptions);
        product.Should().NotBeNull();
        return product;
    }
}