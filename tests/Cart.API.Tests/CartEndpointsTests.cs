using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cart.API.Models;
using Cart.API.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Cart.API.Tests;

using CartModel = Models.Cart;

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

    private sealed class CartBody
    {
        public Guid UsuarioId { get; set; }
        public List<ItemBody> Items { get; set; } = [];
    }

    private sealed class ItemBody
    {
        public Guid ProductoId { get; set; }
        public int Cantidad { get; set; }
    }
}
