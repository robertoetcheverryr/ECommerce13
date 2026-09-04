using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Products.API.Exceptions;
using Products.API.Models;
using Serilog.Events;

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
        var sink = new CollectingSink();
        var client = _factory.CreateClientWithLogs(sink);
        var id = Guid.NewGuid();

        var response = await client.GetAsync($"/api/products/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        AssertWarning(sink, ErrorCodes.PRD_001, ErrorCodes.PRD_001_Message);
        sink.Events.ShouldAllHaveEndpoint($"/api/products/{id}");
    }

    [Fact]
    public async Task Create_WithInvalidData_ShouldLogWarning_WithPrd002()
    {
        var sink = new CollectingSink();
        var client = _factory.CreateClientWithLogs(sink);

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            nombre = "",
            precio = 0,
            stock = 1,
            categoria = "Electrónica"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var warning = sink.Events.Should().Contain(e => e.Level == LogEventLevel.Warning).Subject;
        warning.RenderMessage().Should().Contain(ErrorCodes.PRD_002);
        sink.Events.ShouldAllHaveEndpoint("/api/products");
    }

    [Fact]
    public async Task Create_WithDuplicateNameInSameCategory_ShouldLogWarning_WithPrd003()
    {
        var sink = new CollectingSink();
        var client = _factory.CreateClientWithLogs(sink);
        var body = new
        {
            nombre = $"Dup Log {Guid.NewGuid():N}",
            descripcion = "x",
            precio = 10.00m,
            stock = 1,
            categoria = "Electrónica"
        };

        (await client.PostAsJsonAsync("/api/products", body)).StatusCode.Should().Be(HttpStatusCode.Created);
        sink.Events.Clear();

        var response = await client.PostAsJsonAsync("/api/products", body);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        AssertWarning(
            sink,
            ErrorCodes.PRD_003,
            string.Format(ErrorCodes.PRD_003_Message, "Electrónica"));
        sink.Events.ShouldAllHaveEndpoint("/api/products");
    }

    [Fact]
    public async Task Delete_WhenProductHasActiveOrders_ShouldLogWarning_WithPrd004()
    {
        var sink = new CollectingSink();
        var client = _factory.CreateClientWithActiveOrders(sink: sink);

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
        sink.Events.Clear();

        var response = await client.DeleteAsync($"/api/products/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        AssertWarning(sink, ErrorCodes.PRD_004, ErrorCodes.PRD_004_Message);
        sink.Events.ShouldAllHaveEndpoint($"/api/products/{created.Id}");
    }

    private static void AssertWarning(CollectingSink sink, string errorCode, string errorMessage)
    {
        var warning = sink.Events.Should().Contain(e => e.Level == LogEventLevel.Warning).Subject;
        var rendered = warning.RenderMessage();
        rendered.Should().Contain(errorCode);
        rendered.Should().Contain(errorMessage);
    }
}
