using System.Net;                          // HttpStatusCode (OK, NotFound, Created, etc.)
using System.Net.Http.Json;                // PostAsJsonAsync, ReadFromJsonAsync
using FluentAssertions;                    // Readable assertions (.Should().Be(...))
using Microsoft.AspNetCore.Mvc.Testing;    // WebApplicationFactory (spins up the API in-memory)
using Microsoft.Extensions.DependencyInjection; // Needed to replace services in WithWebHostBuilder
using Products.API.DTOs;                   // CreateProductRequest
using Products.API.Models;                 // Product domain model
using Products.API.Services;               // IProductService (for the throwing fake)

namespace Products.API.Tests;

// IClassFixture<> tells xUnit:
// "Create ONE single instance of WebApplicationFactory and share it across all tests in this class"
// This way we don't restart the API from scratch for every test (that would be very slow).
// Roughly comparable to a session-scoped fixture in pytest.
public class ProductsEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    // HttpClient is the object we use to make HTTP requests
    // (same idea as the "requests" library in Python or fetch in JavaScript).
    // The factory is needed to build our own service with its own behavior.
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    // Constructor: xUnit calls it automatically and injects the factory.
    public ProductsEndpointsTests(WebApplicationFactory<Program> factory)
    {
        // CreateClient() starts the API in-memory (no real port is opened)
        // and returns an HttpClient already configured to talk to it.
        _factory = factory;
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
    public async Task GetById_WhenProductDoesNotExist_ShouldReturnNotFound_WithPrd001()
    {
        // Fixed GUID that is guaranteed not to match the demo product.
        var id = Guid.Parse("00000000-0000-0000-0000-000000000099");

        var response = await _client.GetAsync($"/api/products/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();

        // Validate the entire error response
        body.Should().ContainKeys(
            "type", "title", "status", "detail", "instance", "errorCode", "errorMessage");
        body["type"].ToString().Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.4");
        body["title"].ToString().Should().Be("Not Found");
        body["status"].ToString().Should().Be("404");
        body["detail"].ToString().Should().Be("El recurso solicitado no fue encontrado.");
        body["instance"].ToString().Should().Be($"/api/products/{id}");
        body["errorCode"].ToString().Should().Be("PRD-001");
        body["errorMessage"].ToString().Should().Be("Producto no encontrado.");
    }

    [Fact]
    public async Task Create_WithValidData_ShouldReturnCreated_WithProduct()
    {
        var request = new
        {
            nombre = "Auriculares Sony WH-1000XM5",
            descripcion = "Auriculares inalámbricos con cancelación de ruido",
            precio = 349.99m,
            stock = 25,
            categoria = "Electrónica"
        };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.Should().NotBeNull();
        product.Nombre.Should().Be("Auriculares Sony WH-1000XM5");
        product.Descripcion.Should().Be("Auriculares inalámbricos con cancelación de ruido");
        product.Precio.Should().Be(349.99m);
        product.Stock.Should().Be(25);
        product.Categoria.Should().Be("Electrónica");
        product.Id.Should().NotBeEmpty();
        product.FechaCreacion.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Create_WithInvalidData_ShouldReturnBadRequest_WithPrd002()
    {
        // Missing required fields + invalid price
        var request = new
        {
            nombre = "",
            precio = -10m,
            stock = -5,
            categoria = ""
        };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        body.Should().ContainKeys("type", "title", "status", "detail", "instance", "errorCode", "errorMessage");
        body["status"].ToString().Should().Be("400");
        body["errorCode"].ToString().Should().Be("PRD-002");
        body["errorMessage"].ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Create_WithDuplicateNameInSameCategory_ShouldReturnConflict_WithPrd003()
    {
        // Same name + category as the seeded demo product
        var request = new
        {
            nombre = "Notebook Dell XPS 15",
            descripcion = "Otro notebook",
            precio = 1600.00m,
            stock = 5,
            categoria = "Electrónica"
        };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        body.Should().ContainKeys("type", "title", "status", "detail", "instance", "errorCode", "errorMessage");
        body["status"].ToString().Should().Be("409");
        body["errorCode"].ToString().Should().Be("PRD-003");
        body["errorMessage"].ToString().Should().Contain("Electrónica");
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

    [Fact]
    public async Task WhenUnexpectedExceptionOccurs_ShouldReturnInternalServerError_WithPrd005()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IProductService));

                if (descriptor is not null)
                    services.Remove(descriptor);

                services.AddSingleton<IProductService, ThrowingProductService>();
            });
        });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        body.Should().ContainKeys("type", "title", "status", "detail", "instance", "errorCode", "errorMessage");
        body["status"].ToString().Should().Be("500");
        body["errorCode"].ToString().Should().Be("PRD-005");
        body["errorMessage"].ToString().Should().Be("Error interno al procesar el producto.");
    }

    private class ThrowingProductService : IProductService
    {
        public IEnumerable<Product> GetAll() => throw new Exception("Unexpected failure");
        public Product GetById(Guid id) => throw new Exception("Unexpected failure");
        public Product Create(CreateProductRequest request) => throw new Exception("Unexpected failure");
    }
}