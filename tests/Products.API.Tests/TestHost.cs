using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Products.API.Services;

namespace Products.API.Tests;

internal sealed class AlwaysActiveOrdersChecker : IActiveOrdersChecker
{
    public bool HasActiveOrders(Guid productId) => true;
}

internal static class TestHost
{
    public static HttpClient CreateClientWithActiveOrders(
        this WebApplicationFactory<Program> factory,
        Action<IServiceCollection>? configure = null)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IActiveOrdersChecker));
                if (descriptor is not null)
                    services.Remove(descriptor);

                services.AddSingleton<IActiveOrdersChecker, AlwaysActiveOrdersChecker>();
                configure?.Invoke(services);
            });
        }).CreateClient();
    }

    public static HttpClient CreateClientWithLogger<THandler>(
        this WebApplicationFactory<Program> factory,
        CapturingLogger<THandler> logger)
        where THandler : class
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ILogger<THandler>>(logger);
            });
        }).CreateClient();
    }
}
