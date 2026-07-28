using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;
using SIGEBI.Persistence.Models;

namespace SIGEBI.Persistence.Repositories.Prestamos;

public class PrestamoRepository(
    SigebiContext context,
    ILogger<PrestamoRepository> logger)
    : MutableRepository<Prestamo>(context, logger), IPrestamoRepository
{
    public async Task<Prestamo?> ObtenerPorIdAsync(
        int id,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Consultando préstamo {PrestamoId}", id);
            var prestamo = await _dbSet.FindAsync([id], ct);
            if (prestamo is not null)
                await CargarEjemplarAsync(prestamo, ct);
            return prestamo;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error al obtener préstamo ID {Id}", id);
            throw;
        }
    }

    public Task<IReadOnlyCollection<Prestamo>> ObtenerPorUsuarioAsync(
        int usuarioId,
        CancellationToken ct = default) =>
        ConsultarAsync(
            _dbSet.Where(item => item.UsuarioId == usuarioId)
                .OrderByDescending(item => item.FechaPrestamo),
            ct);

    public Task<IReadOnlyCollection<Prestamo>> ObtenerPorLibroAsync(
        int libroId,
        CancellationToken ct = default) =>
        ConsultarAsync(
            _dbSet.Where(item => item.LibroId == libroId)
                .OrderByDescending(item => item.FechaPrestamo),
            ct);

    public async Task<IReadOnlyCollection<Prestamo>> ObtenerPorEjemplarAsync(
        int ejemplarId,
        CancellationToken ct = default)
    {
        var ids = await _context.PrestamoEjemplares
            .Where(item => item.EjemplarId == ejemplarId)
            .Select(item => item.PrestamoId)
            .ToArrayAsync(ct);
        return await ConsultarAsync(
            _dbSet.Where(item => ids.Contains(item.Id))
                .OrderByDescending(item => item.FechaPrestamo),
            ct);
    }

    public Task<IReadOnlyCollection<Prestamo>> ObtenerDevolucionesPorUsuarioAsync(
        int usuarioId,
        CancellationToken ct = default) =>
        ConsultarAsync(
            _dbSet.Where(item =>
                    item.UsuarioId == usuarioId &&
                    item.FechaRealDevolucion != null)
                .OrderByDescending(item => item.FechaRealDevolucion),
            ct);

    public Task<IReadOnlyCollection<Prestamo>> ObtenerDevolucionesPorLibroAsync(
        int libroId,
        CancellationToken ct = default) =>
        ConsultarAsync(
            _dbSet.Where(item =>
                    item.LibroId == libroId &&
                    item.FechaRealDevolucion != null)
                .OrderByDescending(item => item.FechaRealDevolucion),
            ct);

    public Task<IReadOnlyCollection<Prestamo>> ObtenerPorEstadoAsync(
        EstadoPrestamo estado,
        CancellationToken ct = default) =>
        ConsultarAsync(
            _dbSet.Where(item => item.Estado == estado)
                .OrderByDescending(item => item.FechaPrestamo),
            ct);

    public Task<IReadOnlyCollection<Prestamo>> ObtenerPorRangoAsync(
        DateTime desde,
        DateTime hasta,
        CancellationToken ct = default) =>
        ConsultarAsync(
            _dbSet.Where(item =>
                item.FechaPrestamo >= desde &&
                item.FechaPrestamo <= hasta),
            ct);

    public Task<IReadOnlyCollection<Prestamo>> ObtenerActivosVencidosAsync(
        DateTime fechaReferencia,
        CancellationToken ct = default) =>
        ConsultarAsync(
            _dbSet.Where(item =>
                item.Estado == EstadoPrestamo.Activo &&
                item.FechaEsperadaDevolucion < fechaReferencia),
            ct);

    public Task<IReadOnlyCollection<Prestamo>> ObtenerActivosProximosAVencerAsync(
        DateTime desde,
        DateTime hasta,
        CancellationToken ct = default) =>
        ConsultarAsync(
            _dbSet.Where(item =>
                    item.Estado == EstadoPrestamo.Activo &&
                    item.FechaEsperadaDevolucion >= desde &&
                    item.FechaEsperadaDevolucion <= hasta)
                .OrderBy(item => item.FechaEsperadaDevolucion),
            ct);

    public Task<bool> TieneVencidosPorUsuarioAsync(
        int usuarioId,
        CancellationToken ct = default) =>
        _dbSet.AnyAsync(
            item =>
                item.UsuarioId == usuarioId &&
                (item.Estado == EstadoPrestamo.Vencido ||
                 item.Estado == EstadoPrestamo.Activo &&
                 item.FechaEsperadaDevolucion < DateTime.UtcNow),
            ct);

    public Task<int> ContarActivosPorUsuarioAsync(
        int usuarioId,
        CancellationToken ct = default) =>
        _dbSet.CountAsync(
            item =>
                item.UsuarioId == usuarioId &&
                item.Estado == EstadoPrestamo.Activo,
            ct);

    public override async Task AgregarAsync(
        Prestamo prestamo,
        CancellationToken ct = default)
    {
        var ejemplarId = prestamo.EjemplarId;
        await base.AgregarAsync(prestamo, ct);
        await _context.PrestamoEjemplares.AddAsync(
            new PrestamoEjemplarRelacion
            {
                PrestamoId = prestamo.Id,
                EjemplarId = ejemplarId,
                FechaAsignacion = DateTime.UtcNow
            },
            ct);
        await _context.SaveChangesAsync(ct);
    }

    private async Task<IReadOnlyCollection<Prestamo>> ConsultarAsync(
        IQueryable<Prestamo> consulta,
        CancellationToken ct)
    {
        var prestamos = await consulta.ToListAsync(ct);
        if (prestamos.Count == 0)
            return prestamos;

        var ids = prestamos.Select(item => item.Id).ToArray();
        var relaciones = await _context.PrestamoEjemplares
            .AsNoTracking()
            .Where(item => ids.Contains(item.PrestamoId))
            .ToListAsync(ct);
        var ejemplares = relaciones
            .GroupBy(item => item.PrestamoId)
            .ToDictionary(group => group.Key, group => group.First().EjemplarId);
        foreach (var prestamo in prestamos)
        {
            if (ejemplares.TryGetValue(prestamo.Id, out var ejemplarId))
                prestamo.CargarEjemplarPersistido(ejemplarId);
        }

        return prestamos;
    }

    private async Task CargarEjemplarAsync(
        Prestamo prestamo,
        CancellationToken ct)
    {
        var relacion = await _context.PrestamoEjemplares
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.PrestamoId == prestamo.Id, ct);
        if (relacion is not null)
            prestamo.CargarEjemplarPersistido(relacion.EjemplarId);
    }
}
