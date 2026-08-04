using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Context;

namespace SIGEBI.API.Jobs;

/// <summary>
/// Prepara el pool de conexiones durante el arranque para que la primera acción
/// del usuario no pague el costo de conexión a Supabase.
/// </summary>
public sealed class DatabaseWarmupHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseWarmupHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SigebiContext>();
            await context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            // Ejecuta una vez la misma forma de consulta usada por el login. El
            // correo no puede existir, pero EF, Npgsql y el plan quedan calientes.
            var users = scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();
            _ = await users.ObtenerPorEmailAsync(
                "__sigebi_startup_warmup__@invalid.local",
                cancellationToken);
            logger.LogInformation("Conexión con Supabase preparada para recibir solicitudes.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "No fue posible preparar la conexión con Supabase.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
