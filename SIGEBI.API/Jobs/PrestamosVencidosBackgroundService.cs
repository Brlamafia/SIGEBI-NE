using Microsoft.Extensions.Options;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Interfaces.Prestamos;

namespace SIGEBI.API.Jobs;

public sealed class PrestamosVencidosBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<PrestamosVencidosOptions> options,
    ILogger<PrestamosVencidosBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configuracion = options.Value;
        if (!configuracion.Habilitado)
            return;

        if (configuracion.IntervaloMinutos <= 0 || configuracion.UsuarioResponsableId <= 0)
            throw new InvalidOperationException("La configuración del trabajo de préstamos vencidos no es válida.");

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(configuracion.IntervaloMinutos));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var prestamos = scope.ServiceProvider.GetRequiredService<IPrestamoService>();
                var cantidad = await prestamos.ActualizarPrestamosVencidosAsync(
                    new ActualizarPrestamosVencidosDto
                    {
                        FechaReferencia = DateTime.UtcNow,
                        UsuarioResponsableId = configuracion.UsuarioResponsableId
                    },
                    stoppingToken);

                if (cantidad > 0)
                    logger.LogInformation("Se actualizaron {Cantidad} préstamos vencidos.", cantidad);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "No fue posible actualizar automáticamente los préstamos vencidos.");
            }
        }
    }
}
