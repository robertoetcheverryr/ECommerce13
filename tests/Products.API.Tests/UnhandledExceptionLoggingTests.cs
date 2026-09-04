using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Models;
using Products.API.Services;
using Serilog.Events;

namespace Products.API.Tests;

public class UnhandledExceptionLoggingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UnhandledExceptionLoggingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_WhenServiceThrows_ShouldLogError_WithPrd005()
    {
        var sink = new CollectingSink();
        var client = _factory.CreateClientWithLogs(sink, services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IProductService));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddSingleton<IProductService, ThrowingProductService>();
        });

        var response = await client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var error = sink.Events.Should().Contain(e => e.Level == LogEventLevel.Error).Subject;
        var rendered = error.RenderMessage();
        rendered.Should().Contain(ErrorCodes.PRD_005);
        rendered.Should().Contain(ErrorCodes.PRD_005_Message);
        error.Exception.Should().NotBeNull();
        sink.Events.ShouldAllHaveEndpoint("/api/products");
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
