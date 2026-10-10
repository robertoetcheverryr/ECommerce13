using System.Net;
using Cart.API.Exceptions;
using Cart.API.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;

namespace Cart.API.Tests;

using CartModel = global::Cart.API.Models.Cart;

public class UnhandledExceptionTests : IClassFixture<CartApiFactory>
{
    private readonly CartApiFactory _factory;

    public UnhandledExceptionTests(CartApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_WhenServiceThrows_ShouldLogError_WithCrt005_AndHideTheException()
    {
        var sink = new CollectingSink();
        using var client = _factory.CreateClientWithLogs(sink, services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICartService));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddSingleton<ICartService, ThrowingCartService>();
        });
        var userId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/cart/{userId}");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        body.Should().Contain(ErrorCodes.CRT_005);
        body.Should().Contain(ErrorCodes.CRT_005_Message);
        body.Should().NotContain("Unexpected failure");
        body.Should().NotContain("StackTrace");

        var error = sink.Events.Should().ContainSingle(e =>
            e.Level == LogEventLevel.Error &&
            e.RenderMessage().Contains(ErrorCodes.CRT_005)).Subject;
        error.RenderMessage().Should().Contain(ErrorCodes.CRT_005_Message);
        error.Exception.Should().NotBeNull();
        sink.Events.ShouldAllHaveEndpoint($"/api/cart/{userId}");
        sink.Events.ShouldAllHaveCorrelationId(
            response.Headers.GetValues(CorrelationId.HeaderName).Single());
    }

    private sealed class ThrowingCartService : ICartService
    {
        public CartModel Get(Guid usuarioId) => throw new Exception("Unexpected failure");

        public Task<CartModel> AddItem(Guid usuarioId, Guid productoId, int cantidad, CancellationToken cancellationToken = default)
            => throw new Exception("Unexpected failure");

        public Task<CartModel> UpdateItem(Guid usuarioId, Guid productoId, int cantidad, CancellationToken cancellationToken = default)
            => throw new Exception("Unexpected failure");

        public CartModel RemoveItem(Guid usuarioId, Guid productoId) => throw new Exception("Unexpected failure");

        public CartModel Save(CartModel cart) => throw new Exception("Unexpected failure");

        public void Delete(Guid usuarioId) => throw new Exception("Unexpected failure");
    }
}
