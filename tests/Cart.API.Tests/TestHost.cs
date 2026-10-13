using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Cart.API.Tests;

internal static class TestHost
{
    public static HttpClient CreateClientWithLogs(
        this WebApplicationFactory<Program> factory,
        CollectingSink? sink = null,
        Action<IServiceCollection>? configure = null)
    {
        var host = factory.WithLogs(sink, configure);
        return HostBoundClient.Create(host);
    }

    public static WebApplicationFactory<Program> WithLogs(
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
                        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                        .Enrich.FromLogContext()
                        .WriteTo.Sink(sink)
                        .CreateLogger();

                    services.Configure<RequestLoggingOptions>(opts =>
                    {
                        opts.Logger = tapLogger;
                    });
                    foreach (var descriptor in services.Where(d => d.ServiceType == typeof(ILoggerFactory)).ToList())
                        services.Remove(descriptor);

                    services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory(tapLogger));
                }

                configure?.Invoke(services);
            });
        });
    }
}

// HttpClient sends through the handler. Overriding SendAsync is not enough.
// Disposing the client disposes the host WithLogs built.
file sealed class HostBoundClient
{
    public static HttpClient Create(WebApplicationFactory<Program> host)
    {
        try
        {
            var client = host.CreateDefaultClient(new DisposeHostHandler(host));
            return client;
        }
        catch
        {
            host.Dispose();
            throw;
        }
    }

    private sealed class DisposeHostHandler : DelegatingHandler
    {
        private readonly WebApplicationFactory<Program> _host;

        public DisposeHostHandler(WebApplicationFactory<Program> host) => _host = host;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                _host.Dispose();
        }
    }
}
