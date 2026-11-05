
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Notifications.API.DTOs;
using Notifications.API.Models;
using Notifications.API.Services;

namespace Notifications.API.Tests;

public class GlobalExceptionTests
{
    [Fact]
    public async Task GetNotifications_WhenServiceFails_ReturnsNtf004()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    var descriptor = services.Single(
                        d => d.ServiceType == typeof(INotificationService));

                    services.Remove(descriptor);
                    services.AddSingleton<INotificationService, ThrowingNotificationService>();
                });
            });

        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/notifications/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(content);
        Assert.Equal("NTF-004", content.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(content.CorrelationId));
    }

    private sealed class ThrowingNotificationService : INotificationService
    {
        public Notification SendNotification(SendNotificationRequest request)
            => throw new Exception("Error inesperado de prueba");

        public List<Notification> GetNotificationsByUser(Guid userId)
            => throw new Exception("Error inesperado de prueba");
    }

    private sealed class ErrorResponse
    {
        public string? ErrorCode { get; set; }

        public string? CorrelationId { get; set; }
    }
}
