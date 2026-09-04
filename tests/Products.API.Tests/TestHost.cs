using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Products.API.Services;
using Serilog;
using Serilog.AspNetCore;

namespace Products.API.Tests;

internal sealed class AlwaysActiveOrdersChecker : IActiveOrdersChecker
{
    public bool HasActiveOrders(Guid productId) => true;
}

internal static class TestHost
{
    /// <summary>
    /// Builds a client and optionally taps Serilog request logging via <paramref name="sink"/>.
    /// Extra DI overrides go in <paramref name="configure"/> (fake checkers, ILogger&lt;T&gt;, etc.).
    /// </summary>
    public static HttpClient CreateClientWithLogs(
        this WebApplicationFactory<Program> factory,
        CollectingSink? sink = null,
        Action<IServiceCollection>? configure = null)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                if (sink is not null)
                {
                    var tapLogger = new LoggerConfiguration()
                        .MinimumLevel.Information()
                        .WriteTo.Sink(sink)
                        .CreateLogger();

                    services.Configure<RequestLoggingOptions>(opts =>
                    {
                        opts.Logger = tapLogger;
                    });
                }

                configure?.Invoke(services);
            });
        }).CreateClient();
    }

    public static HttpClient CreateClientWithActiveOrders(
        this WebApplicationFactory<Program> factory,
        Action<IServiceCollection>? configure = null)
    {
        return factory.CreateClientWithLogs(configure: services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IActiveOrdersChecker));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddSingleton<IActiveOrdersChecker, AlwaysActiveOrdersChecker>();
            configure?.Invoke(services);
        });
    }

    public static HttpClient CreateClientWithLogger<THandler>(
        this WebApplicationFactory<Program> factory,
        CapturingLogger<THandler> logger,
        CollectingSink? sink = null,
        Action<IServiceCollection>? configure = null)
        where THandler : class
    {
        return factory.CreateClientWithLogs(sink, services =>
        {
            services.AddSingleton<ILogger<THandler>>(logger);
            configure?.Invoke(services);
        });
    }
}
