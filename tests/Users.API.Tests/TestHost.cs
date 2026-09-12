using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Users.API.Tests;

internal static class TestHost
{
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
        }).CreateClient();
    }
}
