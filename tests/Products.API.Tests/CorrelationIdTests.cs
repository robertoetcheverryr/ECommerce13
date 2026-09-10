using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;


namespace Products.API.Tests;

/// <summary>
/// Minimum coverage for spec 5.5 on Products.API:
/// accept or generate X-Correlation-Id, echo it on the response,
/// push it into Serilog, include it on error bodies.
/// </summary>
public class CorrelationIdTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CorrelationIdTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_WhenHeaderMissing_ShouldGenerateAndEchoCorrelationId()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products");

        response.EnsureSuccessStatusCode();
        var correlationId = GetHeader(response);
        correlationId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(correlationId, out _).Should().BeTrue("generated ids are Guids");
    }

    [Fact]
    public async Task GetAll_WhenHeaderProvided_ShouldEchoTheSameValue()
    {
        var client = _factory.CreateClient();
        var incoming = "corr-from-client-001";
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        request.Headers.Add(CorrelationId.HeaderName, incoming);

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        GetHeader(response).Should().Be(incoming);
    }

    [Fact]
    public async Task GetAll_WhenHeaderIsBlank_ShouldGenerateANewId()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        request.Headers.Add(CorrelationId.HeaderName, "   ");

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var correlationId = GetHeader(response);
        correlationId.Should().NotBeNullOrWhiteSpace();
        correlationId.Trim().Should().NotBeEmpty();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_ShouldIncludeCorrelationIdOnRequestLogs()
    {
        var sink = new CollectingSink();
        var client = _factory.CreateClientWithLogs(sink);
        var incoming = "corr-logged-002";
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        request.Headers.Add(CorrelationId.HeaderName, incoming);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        sink.Events.ShouldAllHaveEndpoint("/api/products");
        sink.Events.ShouldAllHaveCorrelationId(incoming);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldRepeatCorrelationIdOnHeaderAndErrorBody()
    {
        var client = _factory.CreateClient();
        var incoming = "corr-error-003";
        var id = Guid.Parse("00000000-0000-0000-0000-000000000099");
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/products/{id}");
        request.Headers.Add(CorrelationId.HeaderName, incoming);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
