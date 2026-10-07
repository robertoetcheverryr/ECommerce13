using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Users.API.Services;

namespace Users.API.Tests;

/*
Spec 5.4: GET /health, GET /health/ready, GET /health/live.
Body is JSON and status is Healthy, Degraded, or Unhealthy.

Ready is the users-table probe. Live is the process check.
UsersApiFactory points the connection at a temp file so no database file
is created in the test output directory. The failure case uses a missing directory.
*/
public class HealthChecksTests : IClassFixture<UsersApiFactory>
{
    private readonly UsersApiFactory _factory;
    private readonly HttpClient _client;

    public HealthChecksTests(UsersApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
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
        // Factory config is in-memory and wins over UseSetting, so the bad path is added the same way.
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Data Source=/this/path/does/not/exist/users-health.db"
                });
            });
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

    [Fact]
    public async Task ReadyCheck_WhenUsersTableIsMissing_ShouldBeUnhealthy()
    {
        // Startup would create the table, so this calls the probe directly.
        var dbPath = Path.Combine(Path.GetTempPath(), $"users-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";

        try
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using var other = connection.CreateCommand();
                other.CommandText = "CREATE TABLE other (Id INTEGER);";
                other.ExecuteNonQuery();
            }

            SqliteConnection.ClearAllPools();

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString
            }).Build();

            var result = await new SqliteHealthCheck(config).CheckHealthAsync(new HealthCheckContext());

            result.Status.Should().Be(HealthStatus.Unhealthy);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }

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
