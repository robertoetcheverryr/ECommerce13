using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Products.API.Services;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Products.API.Tests;

internal sealed class AlwaysActiveOrdersChecker : IActiveOrdersChecker
{
    public bool HasActiveOrders(Guid productId) => true;
}

internal static class TestHost
{
    /// <summary>
    /// Builds a client. When <paramref name="sink"/> is set, every Serilog event
    /// of the request (handlers + request log) goes to that sink, with LogContext
    /// properties such as Endpoint.
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
                        // override the events from AspNetCore, the "starting app, listening on port x, etc"
                        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                        .Enrich.FromLogContext()
                        .WriteTo.Sink(sink)
                        .CreateLogger();

                    services.Configure<RequestLoggingOptions>(opts =>
                    {
                        opts.Logger = tapLogger;
                    });
                    // Drop the Program.cs ILoggerFactory (console + file)
                    // and route ILogger<T> + request logs to CollectingSink.
                    foreach (var descriptor in services.Where(d => d.ServiceType == typeof(ILoggerFactory)).ToList())
                        services.Remove(descriptor);

                    services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory(tapLogger));
                }

                configure?.Invoke(services);
            });
        }).CreateClient();
    }

    public static HttpClient CreateClientWithActiveOrders(
        this WebApplicationFactory<Program> factory,
        Action<IServiceCollection>? configure = null,
        CollectingSink? sink = null)
    {
        return factory.CreateClientWithLogs(sink, services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IActiveOrdersChecker));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddSingleton<IActiveOrdersChecker, AlwaysActiveOrdersChecker>();
            configure?.Invoke(services);
        });
    }
}
