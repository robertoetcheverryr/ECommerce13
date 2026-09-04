using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Products.API.ExceptionHandlers;
using Products.API.Exceptions;
using Products.API.Models;

namespace Products.API.Tests;

public class BusinessRuleLoggingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BusinessRuleLoggingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetById_WhenProductDoesNotExist_ShouldLogWarning_WithPrd001()
    {
        var logger = new CapturingLogger<NotFoundExceptionHandler>();
        var client = _factory.CreateClientWithLogger(logger);

        var response = await client.GetAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        AssertSingleWarning(logger, ErrorCodes.PRD_001, ErrorCodes.PRD_001_Message);
    }

    [Fact]
    public async Task Create_WithInvalidData_ShouldLogWarning_WithPrd002()
    {
        var logger = new CapturingLogger<ValidationExceptionHandler>();
        var client = _factory.CreateClientWithLogger(logger);

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            nombre = "",
            precio = 0,
            stock = 1,
            categoria = "Electrónica"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(LogLevel.Warning);
        logger.Entries[0].Message.Should().Contain(ErrorCodes.PRD_002);
    }

    [Fact]
    public async Task Create_WithDuplicateNameInSameCategory_ShouldLogWarning_WithPrd003()
    {
        var logger = new CapturingLogger<BusinessRuleExceptionHandler>();
        var client = _factory.CreateClientWithLogger(logger);
        var body = new
        {
            nombre = $"Dup Log {Guid.NewGuid():N}",
            descripcion = "x",
            precio = 10.00m,
            stock = 1,
            categoria = "Electrónica"
        };

        (await client.PostAsJsonAsync("/api/products", body)).StatusCode.Should().Be(HttpStatusCode.Created);
        var response = await client.PostAsJsonAsync("/api/products", body);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        AssertSingleWarning(
            logger,
            ErrorCodes.PRD_003,
            string.Format(ErrorCodes.PRD_003_Message, "Electrónica"));
    }

    [Fact]
    public async Task Delete_WhenProductHasActiveOrders_ShouldLogWarning_WithPrd004()
    {
        var logger = new CapturingLogger<BusinessRuleExceptionHandler>();
        var client = _factory.CreateClientWithActiveOrders(services =>
        {
            services.AddSingleton<ILogger<BusinessRuleExceptionHandler>>(logger);
        });

        var createResponse = await client.PostAsJsonAsync("/api/products", new
        {
            nombre = $"Activos Log {Guid.NewGuid():N}",
            descripcion = "x",
            precio = 10.00m,
            stock = 1,
            categoria = "Electrónica"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<Product>();

        var response = await client.DeleteAsync($"/api/products/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        AssertSingleWarning(logger, ErrorCodes.PRD_004, ErrorCodes.PRD_004_Message);
    }

    private static void AssertSingleWarning<T>(
        CapturingLogger<T> logger,
        string errorCode,
        string errorMessage)
    {
        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(LogLevel.Warning);
        logger.Entries[0].Message.Should().Contain(errorCode);
        logger.Entries[0].Message.Should().Contain(errorMessage);
    }
}
