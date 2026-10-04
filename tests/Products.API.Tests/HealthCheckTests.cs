using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Products.API.Tests;

/*
Spec 5.4: GET /health, GET /health/ready, GET /health/live.
Body is JSON and status is Healthy, Degraded, or Unhealthy.

Ready is the SQLite SELECT 1 probe. Live is the process check.
Tests point the connection at :memory: so no database file is created
in the test output directory. The failure case uses a missing directory.
*/
public class HealthChecksTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string MemoryConnectionString = "Data Source=:memory:";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HealthChecksTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", MemoryConnectionString);
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Health_ShouldReturnJsonStatusHealthy()
    {
        var body = await GetStatusAsync(_client, "/health");

        body.StatusCode.Should().Be(HttpStatusCode.OK);
        body.ContentType.Should().StartWith("application/json");
        body.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthReady_ShouldReturnJsonStatusHealthy()
    {
        var body = await GetStatusAsync(_client, "/health/ready");

        body.StatusCode.Should().Be(HttpStatusCode.OK);
        body.ContentType.Should().StartWith("application/json");
        body.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthLive_ShouldReturnJsonStatusHealthy()
    {
        var body = await GetStatusAsync(_client, "/health/live");

        body.StatusCode.Should().Be(HttpStatusCode.OK);
        body.ContentType.Should().StartWith("application/json");
        body.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthReady_WhenSqliteCannotOpen_ShouldBeUnhealthy_AndLiveShouldStayHealthy()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Data Source=/this/path/does/not/exist/products-health.db");
        });
        using var client = factory.CreateClient();

        var health = await GetStatusAsync(client, "/health");
        health.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        health.Status.Should().Be("Unhealthy");

        var ready = await GetStatusAsync(client, "/health/ready");
        ready.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        ready.Status.Should().Be("Unhealthy");

        var live = await GetStatusAsync(client, "/health/live");
        live.StatusCode.Should().Be(HttpStatusCode.OK);
        live.Status.Should().Be("Healthy");
    }

    public void Dispose() => _factory.Dispose();

    private static async Task<StatusBody> GetStatusAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        var json = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(json);
        var status = document.RootElement.GetProperty("status").GetString();

        return new StatusBody(response.StatusCode, mediaType, status ?? string.Empty);
    }

    private sealed record StatusBody(HttpStatusCode StatusCode, string? ContentType, string Status);
}
