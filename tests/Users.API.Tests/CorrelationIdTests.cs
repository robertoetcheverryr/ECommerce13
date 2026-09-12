using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Users.API.Tests;

public class CorrelationIdTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CorrelationIdTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_WhenHeaderMissing_ShouldGenerateAndEchoCorrelationId()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        var correlationId = GetHeader(response);
        correlationId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(correlationId, out _).Should().BeTrue("generated ids are Guids");
    }

    [Fact]
    public async Task Health_WhenHeaderProvided_ShouldEchoTheSameValue()
    {
        var client = _factory.CreateClient();
        var incoming = "corr-from-client-001";
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(CorrelationId.HeaderName, incoming);

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        GetHeader(response).Should().Be(incoming);
    }

    [Fact]
    public async Task Health_WhenHeaderIsBlank_ShouldGenerateANewId()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(CorrelationId.HeaderName, "   ");

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var correlationId = GetHeader(response);
        correlationId.Should().NotBeNullOrWhiteSpace();
        correlationId.Trim().Should().NotBeEmpty();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task Login_WhenUnauthorized_ShouldRepeatCorrelationIdOnHeaderAndErrorBody()
    {
        var client = _factory.CreateClient();
        var incoming = "corr-error-003";
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/login")
        {
            Content = JsonContent.Create(new
            {
                email = "nobody@email.com",
                password = "WrongPassword123!"
            })
        };
        request.Headers.Add(CorrelationId.HeaderName, incoming);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        GetHeader(response).Should().Be(incoming);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        body.Should().ContainKey("correlationId");
        body["correlationId"].ToString().Should().Be(incoming);
    }

    private static string GetHeader(HttpResponseMessage response)
    {
        response.Headers.TryGetValues(CorrelationId.HeaderName, out var values)
            .Should().BeTrue($"{CorrelationId.HeaderName} must be present on the response");
        return values!.Single();
    }
}
