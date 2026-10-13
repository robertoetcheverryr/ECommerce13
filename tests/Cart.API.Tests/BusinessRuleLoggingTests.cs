using System.Net;
using System.Net.Http.Json;
using Cart.API.DTOs;
using Cart.API.Exceptions;
using Cart.API.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;

namespace Cart.API.Tests;

public class BusinessRuleLoggingTests : IClassFixture<CartApiFactory>
{
    private readonly CartApiFactory _factory;

    public BusinessRuleLoggingTests(CartApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddItem_WithInvalidQuantity_ShouldLogWarning_WithCrt004()
    {
        var sink = new CollectingSink();
        using var client = _factory.CreateClientWithLogs(sink);
        var userId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync($"/api/cart/{userId}/items", new AddCartItemRequest
        {
            ProductoId = Guid.NewGuid(),
            Cantidad = 0
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        AssertWarning(sink, ErrorCodes.CRT_004, ErrorCodes.CRT_004_Message);
        sink.Events.ShouldAllHaveEndpoint($"/api/cart/{userId}/items");
        sink.Events.ShouldAllHaveCorrelationId(
            response.Headers.GetValues(CorrelationId.HeaderName).Single());
    }

    [Fact]
    public async Task Get_WhenCartDoesNotExist_ShouldLogWarning_WithCrt001()
    {
        var sink = new CollectingSink();
        using var client = _factory.CreateClientWithLogs(sink);
        var userId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/cart/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        AssertWarning(sink, ErrorCodes.CRT_001, ErrorCodes.CRT_001_Message);
        sink.Events.ShouldAllHaveEndpoint($"/api/cart/{userId}");
        sink.Events.ShouldAllHaveCorrelationId(
            response.Headers.GetValues(CorrelationId.HeaderName).Single());
    }

    [Fact]
    public async Task AddItem_WhenProductDoesNotExist_ShouldLogWarning_WithCrt002()
    {
        var sink = new CollectingSink();
        using var client = _factory.CreateClientWithLogs(sink, services =>
        {
            // AddHttpClient already registered IProductCatalog as ProductCatalogClient.
            // A second AddSingleton does not replace it, and GetRequiredService cannot pick one.
            // Remove the typed client first, then register the fake that answers 404.
            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IProductCatalog)).ToList())
                services.Remove(descriptor);

            services.AddSingleton<IProductCatalog>(new MissingProductCatalog());
        });
        var userId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync($"/api/cart/{userId}/items", new AddCartItemRequest
        {
            ProductoId = Guid.NewGuid(),
            Cantidad = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        AssertWarning(sink, ErrorCodes.CRT_002, ErrorCodes.CRT_002_Message);
        sink.Events.ShouldAllHaveEndpoint($"/api/cart/{userId}/items");
        sink.Events.ShouldAllHaveCorrelationId(
            response.Headers.GetValues(CorrelationId.HeaderName).Single());
    }

    private static void AssertWarning(CollectingSink sink, string errorCode, string errorMessage)
    {
        var warning = sink.Events.Should().ContainSingle(e =>
            e.Level == LogEventLevel.Warning &&
            e.RenderMessage().Contains(errorCode)).Subject;
        warning.RenderMessage().Should().Contain(errorMessage);
    }

    private sealed class MissingProductCatalog : IProductCatalog
    {
        public Task<ProductStock?> GetAsync(Guid productId, CancellationToken cancellationToken = default)
            => Task.FromResult<ProductStock?>(null);
    }
}
