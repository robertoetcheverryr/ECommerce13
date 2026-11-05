using System.Net;
using System.Text.Json;
using Cart.API.Exceptions;
using FluentAssertions;

namespace Cart.API.Tests;

/*
Non-functional tests: Swagger / OpenAPI documentation.
These are not business endpoint tests; they verify cross-cutting concerns.
*/
public class SwaggerTests : IClassFixture<CartApiFactory>
{
    private readonly HttpClient _client;

    public SwaggerTests(CartApiFactory factory)
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
    public async Task SwaggerJson_ShouldContainCartEndpoints()
    {
        var swagger = await LoadSwaggerAsync();
        FindPath(swagger, "/api/cart/{userId}").Should().NotBeNull();
        FindPath(swagger, "/api/cart/{userId}/items").Should().NotBeNull();
        FindPath(swagger, "/api/cart/{userId}/items/{productId}").Should().NotBeNull();
    }

    [Fact]
    public async Task SwaggerJson_ShouldDocumentErrorResponseContract()
    {
        var swagger = await LoadSwaggerAsync();
        var json = swagger.RootElement.GetRawText();

        json.Should().Contain("ErrorResponse");
        json.Should().Contain("errorCode");
        json.Should().Contain("errorMessage");
        json.Should().Contain("correlationId");
    }

    [Fact]
    public async Task SwaggerJson_EachEndpoint_ShouldDocumentOnlyTheContractStatusCodes()
    {
        var swagger = await LoadSwaggerAsync();

        AssertResponseCodes(swagger, "/api/cart/{userId}", "get", ["200", "404", "500"]);
        AssertResponseCodes(swagger, "/api/cart/{userId}/items", "post", ["200", "400", "404", "422", "500"]);
        AssertResponseCodes(swagger, "/api/cart/{userId}/items/{productId}", "put", ["200", "400", "404", "422", "500"]);
        AssertResponseCodes(swagger, "/api/cart/{userId}/items/{productId}", "delete", ["204", "404", "500"]);
        AssertResponseCodes(swagger, "/api/cart/{userId}", "delete", ["204", "404", "500"]);
    }

    [Fact]
    public async Task SwaggerJson_AddItem_ShouldIncludeRequestAndStockExample()
    {
        var swagger = await LoadSwaggerAsync();
        var request = GetRequestExample(swagger, "/api/cart/{userId}/items", "post");
        var example422 = GetResponseExample(swagger, "/api/cart/{userId}/items", "post", "422");

        request.Should().Contain("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        example422.Should().Contain(ErrorCodes.CRT_003);
        example422.Should().Contain("Stock insuficiente. Disponible: 1, solicitado: 5.");
        example422.Should().Contain("correlationId");
    }

    [Fact]
    public async Task SwaggerJson_AddItem_ShouldUseNamedExamples_ForBadRequestAndNotFound()
    {
        var swagger = await LoadSwaggerAsync();
        var media400 = GetResponseMediaType(swagger, "/api/cart/{userId}/items", "post", "400");
        var media404 = GetResponseMediaType(swagger, "/api/cart/{userId}/items", "post", "404");
        var crt004 = GetNamedResponseExample(swagger, "/api/cart/{userId}/items", "post", "400", "crt-004");
        var invalidBody = GetNamedResponseExample(swagger, "/api/cart/{userId}/items", "post", "400", "body-invalido");
        var crt002 = GetNamedResponseExample(swagger, "/api/cart/{userId}/items", "post", "404", "crt-002");
        var invalidUserId = GetNamedResponseExample(swagger, "/api/cart/{userId}/items", "post", "404", "userid-no-guid");

        media400.TryGetProperty("example", out _).Should().BeFalse();
        media400.GetProperty("examples").EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(["crt-004", "body-invalido"]);
        media404.TryGetProperty("example", out _).Should().BeFalse();
        media404.GetProperty("examples").EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(["crt-002", "userid-no-guid"]);
        crt004.Should().Contain(ErrorCodes.CRT_004).And.Contain(ErrorCodes.CRT_004_Message);
        JsonDocument.Parse(invalidBody).RootElement.GetProperty("errorCode").GetString().Should().BeEmpty();
        invalidBody.Should().Contain("El campo 'cantidad' es obligatorio.");
        crt002.Should().Contain(ErrorCodes.CRT_002).And.Contain(ErrorCodes.CRT_002_Message);
        JsonDocument.Parse(invalidUserId).RootElement.GetProperty("errorCode").GetString().Should().BeEmpty();
        invalidUserId.Should().Contain(ErrorCodes.InvalidUserId_Message);
    }

    [Fact]
    public async Task SwaggerJson_UpdateItem_ShouldUseNamedExamples_ForBadRequestAndNotFound()
    {
        var swagger = await LoadSwaggerAsync();
        var path = "/api/cart/{userId}/items/{productId}";
        var media400 = GetResponseMediaType(swagger, path, "put", "400");
        var media404 = GetResponseMediaType(swagger, path, "put", "404");
        var crt004 = GetNamedResponseExample(swagger, path, "put", "400", "crt-004");
        var invalidBody = GetNamedResponseExample(swagger, path, "put", "400", "body-invalido");
        var crt001 = GetNamedResponseExample(swagger, path, "put", "404", "crt-001");
        var crt002 = GetNamedResponseExample(swagger, path, "put", "404", "crt-002");

        media400.TryGetProperty("example", out _).Should().BeFalse();
        media400.GetProperty("examples").EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(["crt-004", "body-invalido"]);
        media404.TryGetProperty("example", out _).Should().BeFalse();
        media404.GetProperty("examples").EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(["crt-001", "crt-002"]);
        crt004.Should().Contain(ErrorCodes.CRT_004).And.Contain(ErrorCodes.CRT_004_Message);
        JsonDocument.Parse(invalidBody).RootElement.GetProperty("errorCode").GetString().Should().BeEmpty();
        invalidBody.Should().Contain("El campo 'cantidad' es obligatorio.");
        crt001.Should().Contain(ErrorCodes.CRT_001).And.Contain(ErrorCodes.CRT_001_Message);
        crt002.Should().Contain(ErrorCodes.CRT_002).And.Contain(ErrorCodes.CRT_002_Message);
    }

    [Fact]
    public async Task SwaggerJson_RemoveItem_ShouldUseNamedExamples_ForNotFound()
    {
        var swagger = await LoadSwaggerAsync();
        var path = "/api/cart/{userId}/items/{productId}";
        var media404 = GetResponseMediaType(swagger, path, "delete", "404");
        var crt001 = GetNamedResponseExample(swagger, path, "delete", "404", "crt-001");
        var invalidProductId = GetNamedResponseExample(swagger, path, "delete", "404", "productid-no-guid");

        media404.TryGetProperty("example", out _).Should().BeFalse();
        media404.GetProperty("examples").EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(["crt-001", "productid-no-guid"]);
        crt001.Should().Contain(ErrorCodes.CRT_001).And.Contain(ErrorCodes.CRT_001_Message);
        JsonDocument.Parse(invalidProductId).RootElement.GetProperty("errorCode").GetString().Should().BeEmpty();
        invalidProductId.Should().Contain(ErrorCodes.InvalidProductId_Message);
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
        paths.TryGetProperty(path, out var pathItem).Should().BeTrue($"swagger should document {path}");
        return pathItem;
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
            $"documented HTTP codes for {method.ToUpperInvariant()} {path} must match the contract");
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

    private static JsonElement GetResponseMediaType(
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
        return media;
    }

    private static string GetNamedResponseExample(
        JsonDocument swagger,
        string path,
        string method,
        string statusCode,
        string name)
    {
        var media = GetResponseMediaType(swagger, path, method, statusCode);
        media.TryGetProperty("examples", out var examples).Should().BeTrue(
            $"{method.ToUpperInvariant()} {path} {statusCode} should have named examples");
        examples.TryGetProperty(name, out var example).Should().BeTrue(
            $"{method.ToUpperInvariant()} {path} {statusCode} should have the '{name}' example");
        example.TryGetProperty("value", out var value).Should().BeTrue();
        return value.GetRawText();
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
