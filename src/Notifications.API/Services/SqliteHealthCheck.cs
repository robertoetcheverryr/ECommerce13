using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Notifications.API.Services;

/// <summary>
/// Comprobación de readiness: abre SQLite y verifica que exista la tabla de notificaciones.
/// </summary>
public class SqliteHealthCheck : IHealthCheck
{
    private readonly IConfiguration _config;

    /// <summary>
    /// Crea la comprobación con la configuración de la conexión.
    /// </summary>
    public SqliteHealthCheck(IConfiguration config) => _config = config;

    /// <summary>
    /// Devuelve Healthy si notifications existe. Unhealthy si no se puede abrir o si falta la tabla.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _config.GetConnectionString("DefaultConnection")
                ?? "Data Source=notifications.db";

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            // The table is the store. Present means this file was initialized, or it already had it.
            command.CommandText = """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table' AND name = 'notifications'
                """;
            var found = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            if (found != 1)
                return HealthCheckResult.Unhealthy("notifications table is missing");

            return HealthCheckResult.Healthy("notifications table is present");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQLite is not reachable", ex);
        }
    }
}
