using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.SolicitudesPrestamo;

namespace SIGEBI.API.Controllers;

/// <summary>
/// Devuelve en una sola respuesta los datos mínimos que necesita el inicio del
/// panel de personal. Evita que el cliente tenga que coordinar varias llamadas
/// y transferir colecciones que no se van a mostrar.
/// </summary>
[Authorize(Roles = "Administrador,Bibliotecario")]
[ApiController]
[Route("api/[controller]")]
public sealed class ResumenInicioController(
    IServiceScopeFactory scopeFactory,
    IMemoryCache cache) : ControllerBase
{
    private const string CacheKey = "resumen-inicio-operativo-v1";

    [HttpGet]
    public async Task<IActionResult> Obtener(CancellationToken cancellationToken)
    {
        var resumen = await cache.GetOrCreateAsync(
            CacheKey,
            async entry =>
            {
                // Un intervalo corto absorbe las entradas repetidas al Inicio
                // sin dejar la información operativa desactualizada.
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(15);
                return await CrearResumenAsync();
            });

        return Ok(resumen);
    }

    private async Task<ResumenInicioDto> CrearResumenAsync()
    {
        // Cada consulta tiene su propio alcance y DbContext. Así se pueden
        // ejecutar en paralelo sin reutilizar una instancia de EF Core.
        var solicitudesTask = EnAlcanceAsync(serviceProvider =>
            serviceProvider.GetRequiredService<ISolicitudPrestamoService>()
                .GetAllAsync());
        var activosTask = EnAlcanceAsync(serviceProvider =>
            serviceProvider.GetRequiredService<IPrestamoService>()
                .ObtenerActivosAsync(CancellationToken.None));
        var vencidosTask = EnAlcanceAsync(serviceProvider =>
            serviceProvider.GetRequiredService<IPrestamoService>()
                .ObtenerVencidosAsync(CancellationToken.None));
        var multasTask = EnAlcanceAsync(serviceProvider =>
            serviceProvider.GetRequiredService<IMultaService>()
                .ObtenerPorEstadoAsync("Pendiente", CancellationToken.None));

        await Task.WhenAll(solicitudesTask, activosTask, vencidosTask, multasTask);

        var todasLasSolicitudes = await solicitudesTask;
        var prestamosActivos = await activosTask;
        var prestamosVencidos = await vencidosTask;
        var multasPendientes = await multasTask;
        var pendientes = todasLasSolicitudes.Count(solicitud =>
            solicitud.Estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase));
        var totalSolicitudes = todasLasSolicitudes.Count();
        var atendidas = totalSolicitudes - pendientes;

        return new ResumenInicioDto(
            pendientes,
            prestamosActivos.Count,
            prestamosVencidos.Count,
            multasPendientes.Sum(multa => multa.Monto),
            totalSolicitudes == 0
                ? 0
                : (int)Math.Round(atendidas * 100d / totalSolicitudes),
            todasLasSolicitudes
                .OrderByDescending(solicitud => solicitud.FechaSolicitud)
                .Take(3)
                .ToArray());
    }

    private async Task<T> EnAlcanceAsync<T>(Func<IServiceProvider, Task<T>> operation)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await operation(scope.ServiceProvider);
    }

    private sealed record ResumenInicioDto(
        int SolicitudesPendientes,
        int PrestamosActivos,
        int PrestamosVencidos,
        decimal MontoMultasPendientes,
        int PorcentajeAtencion,
        IReadOnlyCollection<SolicitudPrestamoDto> ActividadReciente);
}
