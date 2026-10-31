using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Models;
using Products.API.Services;
using Serilog.Events;
using static Products.API.Tests.ErrorResponseAssertions;

namespace Products.API.Tests;

public class UnhandledExceptionLoggingTests : IClassFixture<ProductsApiFactory>
{
    private readonly ProductsApiFactory _factory;

    public UnhandledExceptionLoggingTests(ProductsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_WhenServiceThrows_ShouldLogError_WithPrd005()
    {
        var sink = new CollectingSink();
        using var client = _factory.CreateClientWithLogs(sink, services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IProductService));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddSingleton<IProductService, ThrowingProductService>();
        });

        var response = await client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var error = sink.Events.Should().ContainSingle(e =>
            e.Level == LogEventLevel.Error &&
            e.RenderMessage().Contains(ErrorCodes.PRD_005)).Subject;
        error.RenderMessage().Should().Contain(ErrorCodes.PRD_005_Message);
        error.Exception.Should().NotBeNull();
        sink.Events.ShouldAllHaveEndpoint("/api/products");
        sink.Events.ShouldAllHaveCorrelationId(
            response.Headers.GetValues(CorrelationId.HeaderName).Single());
    }

    [Theory]
    [InlineData("Development", "Unexpected failure")]
    [InlineData("Production", "Ocurrió un error inesperado.")]
    public async Task GetAll_WhenServiceThrows_ShouldReturnDetailByEnvironment_WithPrd005(
        string environment,
        string expectedDetail)
    {
        using var client = _factory.CreateClientForEnvironment(environment, services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IProductService));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddSingleton<IProductService, ThrowingProductService>();
        });

        var response = await client.GetAsync("/api/products");

        await AssertInternalError(
            response,
            "/api/products",
            ErrorCodes.PRD_005,
            ErrorCodes.PRD_005_Message,
            expectedDetail);
        var rawBody = await response.Content.ReadAsStringAsync();
        rawBody.Should().NotContain(" at ").And.NotContain("ThrowingProductService");
    }

    private sealed class ThrowingProductService : IProductService
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
}
