using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Products.API.Exceptions;

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
    public async Task SwaggerJson_GetById_ShouldIncludeSuccessAndPrd001Examples()
    {
        var swagger = await LoadSwaggerAsync();
        var example200 = GetResponseExample(swagger, "/api/products/{id}", "get", "200");
        var example404 = GetResponseExample(swagger, "/api/products/{id}", "get", "404");

        example200.Should().Contain("Notebook Dell XPS 15");
        example404.Should().Contain(ErrorCodes.PRD_001);
        example404.Should().Contain(ErrorCodes.PRD_001_Message);
    }

    [Fact]
    public async Task SwaggerJson_Create_ShouldIncludeRequestAndConflictExamples()
    {
        var swagger = await LoadSwaggerAsync();
        var request = GetRequestExample(swagger, "/api/products", "post");
        var example409 = GetResponseExample(swagger, "/api/products", "post", "409");

        request.Should().Contain("Notebook Dell XPS 15");
        example409.Should().Contain(ErrorCodes.PRD_003);
    }

    [Fact]
    public async Task SwaggerJson_Delete_ShouldIncludePrd004Example()
    {
        var swagger = await LoadSwaggerAsync();
        var example409 = GetResponseExample(swagger, "/api/products/{id}", "delete", "409");

        example409.Should().Contain(ErrorCodes.PRD_004);
        example409.Should().Contain(ErrorCodes.PRD_004_Detail);
    }

    [Fact]
    public async Task SwaggerJson_ShouldDocument500WithErrorResponse()
    {
        var swagger = await LoadSwaggerAsync();
        var json = swagger.RootElement.GetRawText();

        json.Should().Contain("500");
        json.Should().Contain("ErrorResponse");
        json.Should().ContainEquivalentOf(ErrorCodes.PRD_005);
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

    private static string GetResponseExample(
        JsonDocument swagger,
        string path,
        string method,
        string statusCode)
    {
        var operation = GetOperation(swagger, path, method);
        operation.TryGetProperty("responses", out var responses).Should().BeTrue();
        responses.TryGetProperty(statusCode, out var response).Should().BeTrue();
        response.TryGetProperty("content", out var content).Should().BeTrue();
        content.TryGetProperty("application/json", out var media).Should().BeTrue();
        media.TryGetProperty("example", out var example).Should().BeTrue(
            $"{method.ToUpperInvariant()} {path} {statusCode} should have an example");
        return example.GetRawText();
    }

    private static string GetRequestExample(JsonDocument swagger, string path, string method)
    {
        var operation = GetOperation(swagger, path, method);
        operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue();
        requestBody.TryGetProperty("content", out var content).Should().BeTrue();
        content.TryGetProperty("application/json", out var media).Should().BeTrue();
        media.TryGetProperty("example", out var example).Should().BeTrue(
            $"{method.ToUpperInvariant()} {path} should have a request example");
        return example.GetRawText();
    }

    private static JsonElement GetOperation(JsonDocument swagger, string path, string method)
    {
        var pathItem = FindPath(swagger, path);
        pathItem.TryGetProperty(method, out var operation).Should().BeTrue();
        return operation;
    }
}