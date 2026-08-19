using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Products.API.Tests;

// Non-functional tests: Swagger / OpenAPI documentation.
// These are not business endpoint tests; they verify cross-cutting concerns.
public class SwaggerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SwaggerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SwaggerUI_ShouldBeAvailable()
    {
        var response = await _client.GetAsync("/swagger/index.html");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SwaggerJson_ShouldContainProductsEndpoint()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        // Case-insensitive test:
        json.Should().ContainEquivalentOf("/api/products");
    }

    [Fact]
    public async Task SwaggerJson_GetById_ShouldDocument404WithErrorResponse()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();

        // Path exists
        json.Should().ContainEquivalentOf("/api/Products/{id}");

        // 404 is documented
        json.Should().Contain("404");

        // ErrorResponse schema is present (from our DTO)
        json.Should().Contain("ErrorResponse");
        json.Should().Contain("errorCode");
        json.Should().Contain("errorMessage");
    }
}