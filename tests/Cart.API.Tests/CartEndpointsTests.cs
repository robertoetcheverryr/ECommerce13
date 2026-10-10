using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cart.API.DTOs;
using Cart.API.Models;
using Cart.API.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Cart.API.Tests;

using CartModel = Models.Cart;

/*
CRT-002 is a Products 404. CRT-003 is stock lower than the quantity.
These tests do not start Products. ClientWithStock replaces IProductCatalog,
so the rules run without port 5001. ProductCatalogClient is the real call,
used when the API is running. A cross-service test will be built when all API are done.
*/
public class CartEndpointsTests : IClassFixture<CartApiFactory>
{
    private readonly CartApiFactory _factory;

    public CartEndpointsTests(CartApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_WhenCartDoesNotExist_ShouldReturnCrt001()
    {
        using var client = _factory.CreateClient();
        var userId = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333");

        var response = await client.GetAsync($"/api/cart/{userId}");
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        document.RootElement.GetProperty("errorCode").GetString().Should().Be("CRT-001");
        document.RootElement.GetProperty("errorMessage").GetString().Should().Be("Carrito no encontrado.");
        document.RootElement.GetProperty("status").GetInt32().Should().Be(404);
    }

    [Fact]
    public async Task Get_WhenCartExists_ShouldReturnTheCart()
    {
        var userId = Guid.Parse("bbbbbbbb-0000-0000-0000-111122223333");
        var productId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ICartService>().Save(new CartModel
        {
            UsuarioId = userId,
            Items = [new CartItem { ProductoId = productId, Cantidad = 2 }]
        });

        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/cart/{userId}");
        var body = await response.Content.ReadFromJsonAsync<CartBody>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!.UsuarioId.Should().Be(userId);
        body.Items.Should().ContainSingle();
        body.Items[0].ProductoId.Should().Be(productId);
        body.Items[0].Cantidad.Should().Be(2);
    }

    [Fact]
    public async Task AddItem_WhenQuantityIsNotPositive_ShouldReturnCrt004()
    {
        using var client = ClientWithStock(10);
        var userId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync($"/api/cart/{userId}/items", new AddCartItemRequest
        {
            ProductoId = Guid.NewGuid(),
            Cantidad = 0
        });
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        document.RootElement.GetProperty("errorCode").GetString().Should().Be("CRT-004");
        document.RootElement.GetProperty("errorMessage").GetString().Should().Be("Cantidad inválida.");
    }

    [Fact]
    public async Task AddItem_WhenProductDoesNotExist_ShouldReturnCrt002()
    {
        using var client = ClientWithStock(null);
        var userId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync($"/api/cart/{userId}/items", new AddCartItemRequest
        {
            ProductoId = Guid.NewGuid(),
            Cantidad = 1
        });
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        document.RootElement.GetProperty("errorCode").GetString().Should().Be("CRT-002");
        document.RootElement.GetProperty("errorMessage").GetString().Should().Be("Producto no encontrado.");
    }

    [Fact]
    public async Task AddItem_WhenStockIsInsufficient_ShouldReturnCrt003()
    {
        using var client = ClientWithStock(1);
        var userId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync($"/api/cart/{userId}/items", new AddCartItemRequest
        {
            ProductoId = Guid.NewGuid(),
            Cantidad = 5
        });
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        document.RootElement.GetProperty("errorCode").GetString().Should().Be("CRT-003");
        document.RootElement.GetProperty("errorMessage").GetString().Should().Be("Stock insuficiente. Disponible: 1, solicitado: 5.");
    }

    [Fact]
    public async Task AddItem_WhenStockIsEnough_ShouldCreateTheCart()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        using var client = ClientWithStock(10);

        var response = await client.PostAsJsonAsync($"/api/cart/{userId}/items", new AddCartItemRequest
        {
            ProductoId = productId,
            Cantidad = 2
        });
        var body = await response.Content.ReadFromJsonAsync<CartBody>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!.UsuarioId.Should().Be(userId);
        body.Items.Should().ContainSingle();
        body.Items[0].ProductoId.Should().Be(productId);
        body.Items[0].Cantidad.Should().Be(2);
    }


    [Fact]
    public async Task RemoveItem_WhenCartDoesNotExist_ShouldReturnCrt001()
    {
        using var client = _factory.CreateClient();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var response = await client.DeleteAsync($"/api/cart/{userId}/items/{productId}");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        document.RootElement.GetProperty("errorCode").GetString().Should().Be("CRT-001");
    }

    [Fact]
    public async Task RemoveItem_WhenItemExists_ShouldReturnTheCartWithoutIt()
    {
        var userId = Guid.NewGuid();
        var kept = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var removed = Guid.Parse("aaaabbbb-cccc-dddd-eeee-ffff00001111");
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ICartService>().Save(new CartModel
        {
            UsuarioId = userId,
            Items =
            [
                new CartItem { ProductoId = kept, Cantidad = 1 },
                new CartItem { ProductoId = removed, Cantidad = 2 }
            ]
        });

        using var client = _factory.CreateClient();
        var response = await client.DeleteAsync($"/api/cart/{userId}/items/{removed}");
        var body = await response.Content.ReadFromJsonAsync<CartBody>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!.Items.Should().ContainSingle();
        body.Items[0].ProductoId.Should().Be(kept);
        body.Items[0].Cantidad.Should().Be(1);
    }

    [Fact]
    public async Task Clear_WhenCartDoesNotExist_ShouldReturnCrt001()
    {
        using var client = _factory.CreateClient();
        var userId = Guid.NewGuid();

        var response = await client.DeleteAsync($"/api/cart/{userId}");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        document.RootElement.GetProperty("errorCode").GetString().Should().Be("CRT-001");
        document.RootElement.GetProperty("errorMessage").GetString().Should().Be("Carrito no encontrado.");
    }

    [Fact]
    public async Task Clear_WhenCartExists_ShouldReturnTheMessage_AndRemoveTheCart()
    {
        var userId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ICartService>().Save(new CartModel
        {
            UsuarioId = userId,
            Items = [new CartItem { ProductoId = Guid.NewGuid(), Cantidad = 1 }]
        });

        using var client = _factory.CreateClient();
        var response = await client.DeleteAsync($"/api/cart/{userId}");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.RootElement.GetProperty("mensaje").GetString().Should().Be("Carrito vaciado exitosamente.");

        var missing = await client.GetAsync($"/api/cart/{userId}");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private HttpClient ClientWithStock(int? stock)
    {
        var host = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IProductCatalog>(new FixedStockCatalog(stock));
            });
        });
        return host.CreateClient();
    }

    private sealed class FixedStockCatalog : IProductCatalog
    {
        private readonly int? _stock;

        public FixedStockCatalog(int? stock) => _stock = stock;

        public Task<ProductStock?> GetAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            if (_stock is null)
                return Task.FromResult<ProductStock?>(null);
            return Task.FromResult<ProductStock?>(new ProductStock { Stock = _stock.Value });
        }
    }

    private sealed class CartBody
    {
        public Guid UsuarioId { get; set; }
        public List<ItemBody> Items { get; set; } = [];
    }

    private sealed record ItemBody(Guid ProductoId, int Cantidad);
}
