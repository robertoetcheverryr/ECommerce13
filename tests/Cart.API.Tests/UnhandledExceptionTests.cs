using System.Net;
using System.Text.Json;
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

    [Theory]
    [InlineData("Development", "Unexpected failure")]
    [InlineData("Production", "Ocurrió un error inesperado.")]
    public async Task Get_WhenServiceThrows_ShouldReturnDetailByEnvironment_WithCrt005(
        string environment,
        string expectedDetail)
    {
        using var client = _factory.CreateClientForEnvironment(environment, services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICartService));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddSingleton<ICartService, ThrowingCartService>();
        });
        var userId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/cart/{userId}");
        var rawBody = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(rawBody);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        document.RootElement.GetProperty("status").GetInt32().Should().Be(500);
        document.RootElement.GetProperty("detail").GetString().Should().Be(expectedDetail);
        document.RootElement.GetProperty("instance").GetString().Should().Be($"/api/cart/{userId}");
        document.RootElement.GetProperty("errorCode").GetString().Should().Be(ErrorCodes.CRT_005);
        document.RootElement.GetProperty("errorMessage").GetString().Should().Be(ErrorCodes.CRT_005_Message);
        document.RootElement.GetProperty("correlationId").GetString()
            .Should().Be(response.Headers.GetValues(CorrelationId.HeaderName).Single());
        rawBody.Should().NotContain(" at ").And.NotContain("ThrowingCartService");
    }

    private sealed class ThrowingCartService : ICartService
    {
        public CartModel Get(Guid usuarioId) => throw new Exception("Unexpected failure");

        public Task<CartModel> AddItem(Guid usuarioId, Guid productoId, int cantidad, CancellationToken cancellationToken = default)
            => throw new Exception("Unexpected failure");

        public Task<CartModel> UpdateItem(Guid usuarioId, Guid productoId, int cantidad, CancellationToken cancellationToken = default)
            => throw new Exception("Unexpected failure");

        public void RemoveItem(Guid usuarioId, Guid productoId) => throw new Exception("Unexpected failure");

        public CartModel Save(CartModel cart) => throw new Exception("Unexpected failure");

        public void Delete(Guid usuarioId) => throw new Exception("Unexpected failure");
    }
}
