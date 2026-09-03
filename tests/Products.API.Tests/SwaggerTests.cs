using System.Net;
using System.Text.Json;
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
        var swagger = await LoadSwaggerAsync();
        FindPath(swagger, "/api/products").Should().NotBeNull();
    }

    [Fact]
    public async Task SwaggerJson_GetById_ShouldDocument404WithErrorResponse()
    {
        var swagger = await LoadSwaggerAsync();
        var json = swagger.RootElement.GetRawText();

        FindPath(swagger, "/api/products/{id}").Should().NotBeNull();
        json.Should().Contain("ErrorResponse");
        json.Should().Contain("errorCode");
        json.Should().Contain("errorMessage");
    }

    [Fact]
    public async Task SwaggerJson_EachEndpoint_ShouldDocumentOnlyTheContractStatusCodes()
    {
        var swagger = await LoadSwaggerAsync();

        AssertResponseCodes(swagger, "/api/products", "get", ["200", "500"]);
        AssertResponseCodes(swagger, "/api/products/{id}", "get", ["200", "404", "500"]);
        AssertResponseCodes(swagger, "/api/products", "post", ["201", "400", "409", "500"]);
        AssertResponseCodes(swagger, "/api/products/{id}", "put", ["200", "400", "404", "500"]);
        AssertResponseCodes(swagger, "/api/products/{id}", "delete", ["204", "404", "409", "500"]);
    }

    [Fact]
    public async Task SwaggerJson_ShouldDocument500WithErrorResponse()
    {
        var swagger = await LoadSwaggerAsync();
        var json = swagger.RootElement.GetRawText();

        json.Should().Contain("500");
        json.Should().Contain("ErrorResponse");
        json.Should().ContainEquivalentOf("PRD-005");
    }

    private async Task<JsonDocument> LoadSwaggerAsync()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }

    private static JsonElement FindPath(JsonDocument swagger, string path)
    {
        swagger.RootElement.TryGetProperty("paths", out var paths).Should().BeTrue();

        foreach (var property in paths.EnumerateObject())
        {
            if (string.Equals(property.Name, path, StringComparison.OrdinalIgnoreCase))
                return property.Value;
        }

        throw new InvalidOperationException($"Path '{path}' was not found in swagger.json.");
    }

    private static void AssertResponseCodes(
        JsonDocument swagger,
        string path,
        string method,
        string[] expectedCodes)
    {
        var pathItem = FindPath(swagger, path);
        pathItem.TryGetProperty(method, out var operation).Should().BeTrue(
            $"swagger should document {method.ToUpperInvariant()} {path}");

        operation.TryGetProperty("responses", out var responses).Should().BeTrue();

        var actualCodes = responses.EnumerateObject()
            .Select(property => property.Name)
            .ToList();

        actualCodes.Should().BeEquivalentTo(
            expectedCodes,
            $"documented HTTP codes for {method.ToUpperInvariant()} {path} must match 4.1");
    }
}