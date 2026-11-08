using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Orders.API.Services;

/// <summary>
/// Comprobación de readiness: abre SQLite y verifica que existan las tablas de las órdenes.
/// </summary>
public class SqliteHealthCheck : IHealthCheck
{
    private readonly IConfiguration _config;

    /// <summary>
    /// Crea la comprobación con la configuración de la conexión.
    /// </summary>
    public SqliteHealthCheck(IConfiguration config) => _config = config;

    /// <summary>
    /// Devuelve Healthy si orders y order_items existen. Unhealthy si no se puede abrir o si falta una tabla.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _config.GetConnectionString("DefaultConnection")
                ?? "Data Source=orders.db";

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            // Both tables are the store. Present means this file was initialized, or it already had them.
            command.CommandText = """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table' AND name IN ('orders', 'order_items')
                """;
            var found = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            if (found != 2)
                return HealthCheckResult.Unhealthy("order tables are missing");

            return HealthCheckResult.Healthy("order tables are present");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQLite is not reachable", ex);
        }
    }
}
