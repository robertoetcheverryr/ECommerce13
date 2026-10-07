using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Users.API.Services;

/// <summary>
/// Comprobación de readiness: abre SQLite y verifica que exista la tabla users.
/// </summary>
public class SqliteHealthCheck : IHealthCheck
{
    private readonly IConfiguration _config;

    /// <summary>
    /// Crea la comprobación con la configuración de la conexión.
    /// </summary>
    public SqliteHealthCheck(IConfiguration config) => _config = config;

    /// <summary>
    /// Devuelve Healthy si la tabla users existe. Unhealthy si no se puede abrir o si falta la tabla.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _config.GetConnectionString("DefaultConnection")
                ?? "Data Source=users.db";

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            // users is the only store. Present means this file was initialized, or it already had the table.
            command.CommandText = """
                SELECT 1
                FROM sqlite_master
                WHERE type = 'table' AND name = 'users'
                LIMIT 1
                """;
            var found = await command.ExecuteScalarAsync(cancellationToken);
            if (found is null)
                return HealthCheckResult.Unhealthy("users table is missing");

            return HealthCheckResult.Healthy("users table is present");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQLite is not reachable", ex);
        }
    }
}
