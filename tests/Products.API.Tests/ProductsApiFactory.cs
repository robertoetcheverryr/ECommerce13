using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
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
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={dbPath}"
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
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
            return Task.CompletedTask;
        }
    }
}
