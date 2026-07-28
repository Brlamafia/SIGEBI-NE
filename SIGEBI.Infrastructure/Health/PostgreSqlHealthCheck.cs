using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace SIGEBI.Infrastructure.Health;

public sealed class PostgreSqlHealthCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy("PostgreSQL disponible.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "No fue posible conectar con PostgreSQL.",
                exception);
        }
    }
}
