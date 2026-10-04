using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Products.API.Services;

/// <summary>
/// Comprobación de readiness: abre SQLite y ejecuta SELECT 1.
/// No crea tablas ni lee el esquema.
/// </summary>
public class SqliteHealthCheck : IHealthCheck
{
    private readonly IConfiguration _config;

    /// <summary>
    /// Crea la sonda con la configuración de la conexión.
    /// </summary>
    public SqliteHealthCheck(IConfiguration config) => _config = config;

    /// <summary>
    /// Devuelve Healthy si SQLite responde. Unhealthy si no se puede abrir.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _config.GetConnectionString("DefaultConnection")
                ?? "Data Source=products.db";

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            // SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'Product' LIMIT 1
            // once the persistence PR is merged
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("SELECT 1 ok");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQLite is not reachable", ex);
        }
    }
}
