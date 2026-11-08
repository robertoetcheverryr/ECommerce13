using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Notifications.API.DTOs;
using Notifications.API.Models;
using Notifications.API.Services;

namespace Notifications.API.Tests;

public class CorrelationIdHeaderTests
{
    [Fact]
    public async Task GetNotifications_WhenUserHasNone_ReturnsNtf003WithCorrelationIdHeader()
    {
        await using var factory = CreateFactory(new EmptyNotificationService());
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/notifications/{Guid.NewGuid()}");
        request.Headers.Add("X-Correlation-Id", "ntf-003-header");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var values));
        Assert.Equal("ntf-003-header", Assert.Single(values));
    }

    [Fact]
    public async Task GetNotifications_WhenServiceFails_ReturnsNtf004WithCorrelationIdHeader()
    {
        await using var factory = CreateFactory(new ThrowingNotificationService());
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/notifications/{Guid.NewGuid()}");
        request.Headers.Add("X-Correlation-Id", "ntf-004-header");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var values));
        Assert.Equal("ntf-004-header", Assert.Single(values));
    }

    [Fact]
    public async Task GetNotifications_WhenHeaderIsBlank_GeneratesGuidOnHeaderAndBody()
    {
        await using var factory = CreateFactory(new EmptyNotificationService());
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/notifications/{Guid.NewGuid()}");
        request.Headers.Add("X-Correlation-Id", "   ");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var values));
        var correlationId = Assert.Single(values);
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.True(Guid.TryParse(correlationId, out _));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(correlationId, body.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task GetNotifications_WhenHeaderHasPadding_TrimsCorrelationIdOnHeaderAndBody()
    {
        await using var factory = CreateFactory(new EmptyNotificationService());
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/notifications/{Guid.NewGuid()}");
        request.Headers.Add("X-Correlation-Id", " abc ");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var values));
        Assert.Equal("abc", Assert.Single(values));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("abc", body.GetProperty("correlationId").GetString());
    }

    private static WebApplicationFactory<Program> CreateFactory(INotificationService service)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    var descriptor = services.Single(
                        d => d.ServiceType == typeof(INotificationService));

                    services.Remove(descriptor);
                    services.AddSingleton(service);
                });
            });
    }

    private sealed class EmptyNotificationService : INotificationService
    {
        public Notification SendNotification(SendNotificationRequest request)
            => throw new NotSupportedException();

        public List<Notification> GetNotificationsByUser(Guid userId)
            => new();
    }

    private sealed class ThrowingNotificationService : INotificationService
    {
        public Notification SendNotification(SendNotificationRequest request)
            => throw new Exception("Error inesperado de prueba");

        public List<Notification> GetNotificationsByUser(Guid userId)
            => throw new Exception("Error inesperado de prueba");
    }
}
