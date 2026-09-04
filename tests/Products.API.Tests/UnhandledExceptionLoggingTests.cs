using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Products.API.DTOs;
using Products.API.ExceptionHandlers;
using Products.API.Exceptions;
using Products.API.Models;
using Products.API.Services;

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
        var logger = new CapturingLogger<GlobalExceptionHandler>();
        var client = _factory.CreateClientWithLogger(logger, configure: services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IProductService));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddSingleton<IProductService, ThrowingProductService>();
        });

        var response = await client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(LogLevel.Error);
        logger.Entries[0].Message.Should().Contain(ErrorCodes.PRD_005);
        logger.Entries[0].Message.Should().Contain(ErrorCodes.PRD_005_Message);
        logger.Entries[0].Exception.Should().NotBeNull();
    }

    // Duplicated for now because it is not worth it to have a common file for just this
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
