using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace SIGEBI.Infrastructure.Health;

public sealed class PostgreSqlHealthCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    private static readonly (string Table, string Column)[] RequiredColumns =
    [
        ("Usuarios", "id_usuario"),
        ("Usuarios", "contrasena_hash"),
        ("Usuarios", "intentos_acceso_fallidos"),
        ("Usuarios", "bloqueado_hasta"),
        ("Libros", "id_libro"),
        ("Inventario", "cantidad_reservada"),
        ("Inventario", "cantidad_fuera_servicio"),
        ("Inventario", "cantidad_perdida"),
        ("Inventario", "cantidad_danada"),
        ("SolicitudPrestamo", "id_solicitud"),
        ("Prestamos", "id_prestamo"),
        ("Multas", "tipo"),
        ("Notificaciones", "id_notificacion"),
        ("Roles", "id_rol"),
        ("Permisos", "id_permiso"),
        ("UsuarioRol", "id_usuario"),
        ("RolPermiso", "id_rol"),
        ("Ejemplares", "id_ejemplar"),
        ("PrestamoEjemplar", "id_prestamo")
    ];

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = """
                SELECT table_name, column_name
                FROM information_schema.columns
                WHERE table_schema = 'public';
                """;
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var available = new HashSet<(string Table, string Column)>();
            while (await reader.ReadAsync(cancellationToken))
                available.Add((reader.GetString(0), reader.GetString(1)));

            var missing = RequiredColumns
                .Where(required => !available.Contains(required))
                .Select(required => $"{required.Table}.{required.Column}")
                .ToArray();
            return missing.Length == 0
                ? HealthCheckResult.Healthy(
                    "PostgreSQL disponible y el esquema SIGEBI es compatible.")
                : HealthCheckResult.Unhealthy(
                    $"El esquema PostgreSQL no es compatible. Faltan: {string.Join(", ", missing)}.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "No fue posible conectar con PostgreSQL.",
                exception);
        }
    }
}
