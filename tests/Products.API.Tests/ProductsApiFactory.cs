using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Products.API.Tests;

// Keeps WebApplicationFactory off Data Source=products.db, which would land in the test output directory.
public sealed class ProductsApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"products-{Guid.NewGuid():N}.db");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={dbPath};Pooling=false"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IHostedService>(_ => new TempSqliteCleanup(dbPath));
        });
    }

    private sealed class TempSqliteCleanup : IHostedService
    {
        private readonly string _dbPath;

        public TempSqliteCleanup(string dbPath) => _dbPath = dbPath;

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Windows denies the delete while SQLite still has the file, and that
            // exception is UnauthorizedAccessException, not IOException. A leftover
            // temp file must not fail the test.
            try
            {
                if (File.Exists(_dbPath))
                    File.Delete(_dbPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }

            return Task.CompletedTask;
        }
    }
}
