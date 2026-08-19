using System.Net; // Contains HttpStatusCode (OK, NotFound, etc.)
using System.Net.Http.Json; // Contains ReadFromJsonAsync (deserialize JSON body)
using FluentAssertions; // Library for readable assertions (.Should().Be(...))
using Microsoft.AspNetCore.Mvc.Testing; // Contains WebApplicationFactory (spins up the API in-memory)
using Products.API.Models; // Product domain model

namespace Products.API.Tests;

// IClassFixture<> tells xUnit:
// "Create ONE single instance of WebApplicationFactory and share it across all tests in this class"
// This way we don't restart the API from scratch for every test (that would be very slow).
// Roughly comparable to a session-scoped fixture in pytest.
public class ProductsEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    // HttpClient is the object we use to make HTTP requests
    // (same idea as the "requests" library in Python or fetch in JavaScript).
    private readonly HttpClient _client;

    // Constructor: xUnit calls it automatically and injects the factory.
    public ProductsEndpointsTests(WebApplicationFactory<Program> factory)
    {
        // CreateClient() starts the API in-memory (no real port is opened)
        // and returns an HttpClient already configured to talk to it.
        _client = factory.CreateClient();
    }

    // [Fact] = "this is a test".
    // xUnit finds every method marked with [Fact] and runs it.
    // It is the equivalent of "def test_something():" in pytest.
    [Fact]
    public async Task GetAll_ShouldReturnOk_WithProductList()
    {
        // async + Task → this method is asynchronous.
        // In C# network operations (HTTP) are asynchronous.
        // "Task" is similar to a Promise/Future: it represents an operation
        // that will finish in the future.
        // "async" allows us to use "await" inside the method.

        // Send a GET request to /api/products
        var response = await _client.GetAsync("/api/products");

        // Assert that the status code is 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // ReadFromJsonAsync deserializes the JSON body into a List<Product>
        // (similar to response.json() in Python requests)
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();

        products.Should().NotBeNull();
        products.Should().HaveCount(1);
        products[0].Nombre.Should().Be("Notebook Dell XPS 15");
    }

    [Fact]
    public async Task GetById_WhenProductExists_ShouldReturnOk_WithProduct()
    {
        // Permanent success case (at least until persistence is implemented)
        // Uses the exact ID of the hardcoded demo product.
        var id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

        var response = await _client.GetAsync($"/api/products/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var product = await response.Content.ReadFromJsonAsync<Product>();

        product.Should().NotBeNull();
        // The ! here indicates to the compiler "Trust me, this is NOT null".
        // The IDE complains because FluentAssertions is smart enough to say
        // "we JUST tested it is NOT null", so it marks the ! as redundant.
        // Kept here on purpose to document this interesting feature.
        // ReSharper disable once RedundantSuppressNullableWarningExpression
        product!.Id.Should().Be(id);
        product.Nombre.Should().Be("Notebook Dell XPS 15");
        product.Categoria.Should().Be("Electrónica");
    }

    [Fact]
    public async Task GetById_WhenProductDoesNotExist_ShouldReturnNotFound()
    {
        // Error case (still a plain 404, without the errorCode format yet).
        // Later we will harden the assertions to check
        // errorCode = "PRD-001" and the Problem Details body.
        var id = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/products/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturnOk_WithHelloMessage()
    {
        // PostAsync requires a body. We pass null because the Hello World
        // endpoint doesn't care about the body yet.
        var response = await _client.PostAsync("/api/products", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("POST /api/products");
    }

    [Fact]
    public async Task Update_ShouldReturnOk_WithHelloMessage()
    {
        var id = Guid.NewGuid();
        var response = await _client.PutAsync($"/api/products/{id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"PUT /api/products/{id}");
    }

    [Fact]
    public async Task Delete_ShouldReturnOk_WithHelloMessage()
    {
        var id = Guid.NewGuid();
        var response = await _client.DeleteAsync($"/api/products/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"DELETE /api/products/{id}");
    }
}