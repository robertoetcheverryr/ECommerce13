using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Users.API.Exceptions;

namespace Users.API.Tests;

/*
Non-functional tests: Swagger / OpenAPI documentation.
These are not business endpoint tests; they verify cross-cutting concerns.
*/
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
    public async Task SwaggerJson_ShouldContainUsersEndpoints()
    {
        var swagger = await LoadSwaggerAsync();
        FindPath(swagger, "/api/users/register").Should().NotBeNull();
        FindPath(swagger, "/api/users/login").Should().NotBeNull();
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

        AssertResponseCodes(swagger, "/api/users/register", "post", ["201", "400", "409", "500"]);
        AssertResponseCodes(swagger, "/api/users/login", "post", ["200", "400", "401", "403", "500"]);
    }

    [Fact]
    public async Task SwaggerJson_Register_ShouldIncludeRequestAndConflictExamples()
    {
        var swagger = await LoadSwaggerAsync();
        var request = GetRequestExample(swagger, "/api/users/register", "post");
        var example201 = GetResponseExample(swagger, "/api/users/register", "post", "201");
        var example409 = GetResponseExample(swagger, "/api/users/register", "post", "409");

        request.Should().Contain("maria@email.com");
        example201.Should().Contain("María");
        example409.Should().Contain(ErrorCodes.USR_001);
        example409.Should().Contain(string.Format(ErrorCodes.USR_001_Message, "maria@email.com"));
        example409.Should().Contain("correlationId");
        example409.Should().Contain("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    }

    [Fact]
    public async Task SwaggerJson_Login_ShouldIncludeSuccessUnauthorizedAndForbiddenExamples()
    {
        var swagger = await LoadSwaggerAsync();
        var request = GetRequestExample(swagger, "/api/users/login", "post");
        var example200 = GetResponseExample(swagger, "/api/users/login", "post", "200");
        var example401 = GetResponseExample(swagger, "/api/users/login", "post", "401");
        var example403 = GetResponseExample(swagger, "/api/users/login", "post", "403");

        request.Should().Contain("maria@email.com");
        example200.Should().Contain("María");
        example401.Should().Contain(ErrorCodes.USR_003);
        example401.Should().Contain(ErrorCodes.USR_003_Message);
        example403.Should().Contain(ErrorCodes.USR_004);
        example403.Should().Contain(ErrorCodes.USR_004_Message);
        example403.Should().Contain("correlationId");
    }

    [Fact]
    public async Task SwaggerJson_ShouldDocument500WithErrorResponse()
    {
        var swagger = await LoadSwaggerAsync();
        var json = swagger.RootElement.GetRawText();

        json.Should().Contain("500");
        json.Should().Contain("ErrorResponse");
        json.Should().ContainEquivalentOf(ErrorCodes.USR_006);
    }

    [Fact]
    public async Task SwaggerJson_Login_ShouldMentionUsr005On403()
    {
        var swagger = await LoadSwaggerAsync();
        var json = swagger.RootElement.GetRawText();

        json.Should().Contain(ErrorCodes.USR_005);
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
            $"documented HTTP codes for {method.ToUpperInvariant()} {path} must match 4.2");
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
