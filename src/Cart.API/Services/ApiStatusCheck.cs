using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cart.API.Services;

/// <summary>
/// Comprobación de liveness: el proceso está en ejecución.
/// </summary>
public class ApiStatusCheck : IHealthCheck
{
    private static readonly DateTime StartTime = DateTime.UtcNow;

    /// <summary>
    /// Informa que la API está viva, con runtime y tiempo en ejecución.
    /// </summary>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var uptime = DateTime.UtcNow - StartTime;
        var data = new Dictionary<string, object>
        {
            ["runtime"] = $".NET {Environment.Version}",
            ["uptime"] = uptime.ToString(@"hh\:mm\:ss"),
            ["startedAt"] = StartTime.ToString("o")
        };

        return Task.FromResult(HealthCheckResult.Healthy("Process is up", data));
    }
}
