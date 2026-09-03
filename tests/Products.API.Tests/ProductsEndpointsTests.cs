using System.Net; // HttpStatusCode (OK, NotFound, Created, etc.)
using System.Net.Http.Json; // PostAsJsonAsync, ReadFromJsonAsync
using FluentAssertions; // Readable assertions (.Should().Be(...))
using Microsoft.AspNetCore.Mvc.Testing; // WebApplicationFactory (spins up the API in-memory)
using Microsoft.Extensions.DependencyInjection; // Needed to replace services in WithWebHostBuilder
using Products.API.DTOs; // CreateProductRequest, UpdateProductRequest
using Products.API.Exceptions; // ErrorCodes
using Products.API.Models; // Product domain model
using Products.API.Services; // IProductService (for the throwing fake)
using static Products.API.Tests.ErrorResponseAssertions;
using static Products.API.Tests.ProductResponseAssertions;

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
        // Seed once for the whole test class or that was the idea but in the end
        // the IClassFixture is built and destroyed once per test...
        SeedProductsAsync().GetAwaiter().GetResult();
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
        var products = await AssertProductListOk(response);

        // This test is brittle once we get persistence see how to improve it
        products.Should().HaveCountGreaterThan(1);
        products[0].Nombre.Should().Be("Notebook Dell XPS 15");
    }

    [Fact]
    public async Task GetAll_FilterByCategoria_ShouldReturnMatchingProducts()
    {
        var response = await _client.GetAsync("/api/products?categoria=Deportes");

        var products = await AssertProductListOk(response);
        products.Should().OnlyContain(p => p.Categoria == "Deportes");
        products.Should().Contain(p => p.Nombre == "Pelota Adidas");
    }

    [Fact]
    public async Task GetAll_FilterByNombre_ShouldReturnMatchingProducts()
    {
        var response = await _client.GetAsync("/api/products?nombre=Silla");

        var products = await AssertProductListOk(response);
        products.Should().OnlyContain(p =>
            p.Nombre.Contains("Silla", StringComparison.OrdinalIgnoreCase));
        products.Should().Contain(p => p.Nombre == "Silla Gamer Pro");
    }

    [Fact]
    public async Task GetAll_FilterByCategoriaAndNombre_ShouldReturnMatchingProducts()
    {
        var response = await _client.GetAsync("/api/products?categoria=Electrónica&nombre=Notebook");

        var products = await AssertProductListOk(response);
        products.Should().OnlyContain(p =>
            p.Categoria == "Electrónica" &&
            p.Nombre.Contains("Notebook", StringComparison.OrdinalIgnoreCase));
        products.Should().Contain(p => p.Nombre == "Notebook Dell XPS 15");
    }

    [Fact]
    public async Task GetById_WhenProductExists_ShouldReturnOk_WithProduct()
    {
        // Find the ID of the notebook after seeding
        var listResponse = await _client.GetAsync("/api/products?nombre=Notebook Dell XPS 15");
        var products = await AssertProductListOk(listResponse);
        var notebook = products.First(p => p.Nombre == "Notebook Dell XPS 15");

        var response = await _client.GetAsync($"/api/products/{notebook.Id}");

        var product = await AssertProductOk(response);
        // The ! here indicates to the compiler "Trust me, this is NOT null".
        // The IDE complains because FluentAssertions is smart enough to say
        // "we JUST tested it is NOT null", so it marks the ! as redundant.
        // Kept here on purpose to document this interesting feature.
        // ReSharper disable once RedundantSuppressNullableWarningExpression
        product!.Id.Should().Be(notebook.Id);
        product.Nombre.Should().Be("Notebook Dell XPS 15");
        product.Categoria.Should().Be("Electrónica");
    }

    [Fact]
    public async Task GetById_WhenProductDoesNotExist_ShouldReturnNotFound_WithPrd001()
    {
        // Fixed GUID that is guaranteed not to match the demo product.
        var id = Guid.Parse("00000000-0000-0000-0000-000000000099");

        var response = await _client.GetAsync($"/api/products/{id}");

        await AssertNotFound(
            response,
            $"/api/products/{id}",
            ErrorCodes.PRD_001,
            ErrorCodes.PRD_001_Message);
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

        var product = await AssertProductCreated(response);
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

        await AssertBadRequestWithFieldErrors(response, "/api/products", ErrorCodes.PRD_002);
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

        await AssertConflict(
            response,
            "/api/products",
            ErrorCodes.PRD_003,
            string.Format(ErrorCodes.PRD_003_Message, "Electrónica"));
    }

    [Fact]
    public async Task Update_WhenProductExists_ShouldReturnOk_WithUpdatedProduct()
    {
        var createRequest = new
        {
            nombre = $"Teclado Mecanico {Guid.NewGuid():N}",
            descripcion = "Switch red",
            precio = 120.00m,
            stock = 15,
            categoria = "Electrónica"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/products", createRequest);
        var created = await AssertProductCreated(createResponse);

        var updateRequest = new
        {
            nombre = created.Nombre,
            descripcion = "Switch brown, 64GB layout",
            precio = 145.50m,
            stock = 9,
            categoria = "Electrónica"
        };

        var response = await _client.PutAsJsonAsync($"/api/products/{created.Id}", updateRequest);

        var updated = await AssertProductOk(response);
        updated.Id.Should().Be(created.Id);
        updated.Nombre.Should().Be(updateRequest.nombre);
        updated.Descripcion.Should().Be(updateRequest.descripcion);
        updated.Precio.Should().Be(updateRequest.precio);
        updated.Stock.Should().Be(updateRequest.stock);
        updated.Categoria.Should().Be(updateRequest.categoria);
        updated.FechaCreacion.Should().Be(created.FechaCreacion);

        var getResponse = await _client.GetAsync($"/api/products/{created.Id}");
        var fetched = await AssertProductOk(getResponse);
        fetched.Descripcion.Should().Be(updateRequest.descripcion);
        fetched.Precio.Should().Be(updateRequest.precio);
        fetched.Stock.Should().Be(updateRequest.stock);
    }

    [Fact]
    public async Task Update_WithInvalidData_ShouldReturnBadRequest_WithPrd002()
    {
        var listResponse = await _client.GetAsync("/api/products?nombre=Notebook Dell XPS 15");
        var products = await AssertProductListOk(listResponse);
        var notebook = products.First(p => p.Nombre == "Notebook Dell XPS 15");

        var request = new
        {
            nombre = "",
            precio = -10m,
            stock = -5,
            categoria = ""
        };

        var response = await _client.PutAsJsonAsync($"/api/products/{notebook.Id}", request);

        await AssertBadRequestWithFieldErrors(
            response,
            $"/api/products/{notebook.Id}",
            ErrorCodes.PRD_002);
    }

    [Fact]
    public async Task Update_WhenProductDoesNotExist_ShouldReturnNotFound_WithPrd001()
    {
        var id = Guid.Parse("00000000-0000-0000-0000-000000000099");
        var request = new
        {
            nombre = "Producto Inexistente",
            descripcion = "No deberia persistirse",
            precio = 10.00m,
            stock = 1,
            categoria = "Otros"
        };

        var response = await _client.PutAsJsonAsync($"/api/products/{id}", request);

        await AssertNotFound(
            response,
            $"/api/products/{id}",
            ErrorCodes.PRD_001,
            ErrorCodes.PRD_001_Message);
    }

    [Fact]
    public async Task Delete_WhenProductExists_ShouldReturnNoContent_AndRemoveProduct()
    {
        var createRequest = new
        {
            nombre = $"Mouse Inalambrico {Guid.NewGuid():N}",
            descripcion = "Mouse de prueba para delete",
            precio = 25.00m,
            stock = 4,
            categoria = "Electrónica"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/products", createRequest);
        var created = await AssertProductCreated(createResponse);

        var response = await _client.DeleteAsync($"/api/products/{created.Id}");

        await AssertNoContent(response);

        var getResponse = await _client.GetAsync($"/api/products/{created.Id}");
        await AssertNotFound(
            getResponse,
            $"/api/products/{created.Id}",
            ErrorCodes.PRD_001,
            ErrorCodes.PRD_001_Message);
    }

    [Fact]
    public async Task Delete_WhenProductDoesNotExist_ShouldReturnNotFound_WithPrd001()
    {
        var id = Guid.Parse("00000000-0000-0000-0000-000000000099");

        var response = await _client.DeleteAsync($"/api/products/{id}");

        await AssertNotFound(
            response,
            $"/api/products/{id}",
            ErrorCodes.PRD_001,
            ErrorCodes.PRD_001_Message);
    }

    [Fact]
    public async Task Delete_WhenProductHasActiveOrders_ShouldReturnConflict_WithPrd004()
    {
        var createRequest = new
        {
            nombre = $"Auriculares Activos {Guid.NewGuid():N}",
            descripcion = "No deberia poder borrarse",
            precio = 80.00m,
            stock = 2,
            categoria = "Electrónica"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/products", createRequest);
        var created = await AssertProductCreated(createResponse);

        // Temporal workaround while Orders is built, inject a mocked checker that returns true
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IActiveOrdersChecker));

                if (descriptor is not null)
                    services.Remove(descriptor);

                services.AddSingleton<IActiveOrdersChecker, AlwaysActiveOrdersChecker>();
            });
        });

        var client = factory.CreateClient();
        var response = await client.DeleteAsync($"/api/products/{created.Id}");

        await AssertConflict(
            response,
            $"/api/products/{created.Id}",
            ErrorCodes.PRD_004,
            ErrorCodes.PRD_004_Message);
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

        await AssertInternalError(
            response,
            "/api/products",
            ErrorCodes.PRD_005,
            ErrorCodes.PRD_005_Message);
    }

    private class ThrowingProductService : IProductService
    {
        public IEnumerable<Product> GetAll(string? categoria = null, string? nombre = null)
            => throw new Exception("Unexpected failure");

        public Product GetById(Guid id)
            => throw new Exception("Unexpected failure");

        public Product Create(CreateProductRequest request)
            => throw new Exception("Unexpected failure");

        public Product Update(Guid id, UpdateProductRequest request)
            => throw new Exception("Unexpected failure");

        public void Delete(Guid id)
            => throw new Exception("Unexpected failure");
    }

    private class AlwaysActiveOrdersChecker : IActiveOrdersChecker
    {
        public bool HasActiveOrders(Guid productId)
        {
            return true;
        }
    }

    /// <summary>
    /// Loads three known products into the in-memory store.
    /// </summary>
    private async Task SeedProductsAsync()
    {
        var products = new[]
        {
            new
            {
                nombre = "Notebook Dell XPS 15", descripcion = "Laptop 15 pulgadas, 32GB RAM", precio = 1500.00m,
                stock = 10, categoria = "Electrónica",
            },
            new
            {
                nombre = "Silla Gamer Pro", descripcion = "Silla ergonómica", precio = 250.00m, stock = 8,
                categoria = "Hogar y Deco",
            },
            new
            {
                nombre = "Pelota Adidas", descripcion = "Pelota de fútbol", precio = 45.00m, stock = 30,
                categoria = "Deportes",
            }
        };

        foreach (var p in products)
        {
            var response = await _client.PostAsJsonAsync("/api/products", p);

            // Accept both Created (first time) and Conflict (already exists)
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict);
        }
    }
}