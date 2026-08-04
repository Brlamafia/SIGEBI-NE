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
            () => _dbSet.Where(item => item.UsuarioId == usuarioId)
                .OrderByDescending(item => item.FechaPrestamo),
            ct,
            "préstamos del usuario",
            usuarioId);

    public Task<IReadOnlyCollection<Prestamo>> ObtenerPorLibroAsync(
        int libroId,
        CancellationToken ct = default) =>
        ConsultarAsync(
            () => _dbSet.Where(item => item.LibroId == libroId)
                .OrderByDescending(item => item.FechaPrestamo),
            ct,
            "préstamos del libro",
            libroId);

    public async Task<IReadOnlyCollection<Prestamo>> ObtenerPorEjemplarAsync(
        int ejemplarId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Consultando préstamos del ejemplar {EjemplarId}",
                ejemplarId);
            return await ConsultarAsync(
                () => _dbSet.Where(item =>
                        _context.PrestamoEjemplares.Any(relation =>
                            relation.EjemplarId == ejemplarId &&
                            relation.PrestamoId == item.Id))
                    .OrderByDescending(item => item.FechaPrestamo),
                ct,
                "préstamos del ejemplar",
                ejemplarId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Error al consultar préstamos del ejemplar {EjemplarId}",
                ejemplarId);
            throw;
        }
    }

    public Task<IReadOnlyCollection<Prestamo>> ObtenerDevolucionesPorUsuarioAsync(
        int usuarioId,
        CancellationToken ct = default) =>
        ConsultarAsync(
            () => _dbSet.Where(item =>
                    item.UsuarioId == usuarioId &&
                    item.FechaRealDevolucion != null)
                .OrderByDescending(item => item.FechaRealDevolucion),
            ct,
            "devoluciones del usuario",
            usuarioId);

    public Task<IReadOnlyCollection<Prestamo>> ObtenerDevolucionesPorLibroAsync(
        int libroId,
        CancellationToken ct = default) =>
        ConsultarAsync(
            () => _dbSet.Where(item =>
                    item.LibroId == libroId &&
                    item.FechaRealDevolucion != null)
                .OrderByDescending(item => item.FechaRealDevolucion),
            ct,
            "devoluciones del libro",
            libroId);

    public Task<IReadOnlyCollection<Prestamo>> ObtenerPorEstadoAsync(
        EstadoPrestamo estado,
        CancellationToken ct = default) =>
        ConsultarAsync(
            () => _dbSet.Where(item => item.Estado == estado)
                .OrderByDescending(item => item.FechaPrestamo),
            ct,
            $"préstamos con estado {estado}");

    public Task<IReadOnlyCollection<Prestamo>> ObtenerPorRangoAsync(
        DateTime desde,
        DateTime hasta,
        CancellationToken ct = default) =>
        ConsultarAsync(
            () => _dbSet.Where(item =>
                item.FechaPrestamo >= desde &&
                item.FechaPrestamo <= hasta),
            ct,
            $"préstamos entre {desde:O} y {hasta:O}");

    public Task<IReadOnlyCollection<Prestamo>> ObtenerActivosVencidosAsync(
        DateTime fechaReferencia,
        CancellationToken ct = default) =>
        ConsultarAsync(
            () => _dbSet.Where(item =>
                item.Estado == EstadoPrestamo.Activo &&
                item.FechaEsperadaDevolucion < fechaReferencia),
            ct,
            $"préstamos activos vencidos al {fechaReferencia:O}");

    public Task<IReadOnlyCollection<Prestamo>> ObtenerActivosProximosAVencerAsync(
        DateTime desde,
        DateTime hasta,
        CancellationToken ct = default) =>
        ConsultarAsync(
            () => _dbSet.Where(item =>
                    item.Estado == EstadoPrestamo.Activo &&
                    item.FechaEsperadaDevolucion >= desde &&
                    item.FechaEsperadaDevolucion <= hasta)
                .OrderBy(item => item.FechaEsperadaDevolucion),
            ct,
            $"préstamos próximos a vencer entre {desde:O} y {hasta:O}");

    public Task<bool> TieneVencidosPorUsuarioAsync(
        int usuarioId,
        CancellationToken ct = default) =>
        EjecutarConsultaAsync(
            () => _dbSet.AnyAsync(
                item =>
                    item.UsuarioId == usuarioId &&
                    (item.Estado == EstadoPrestamo.Vencido ||
                     item.Estado == EstadoPrestamo.Activo &&
                     item.FechaEsperadaDevolucion < DateTime.UtcNow),
                ct),
            "verificar préstamos vencidos del usuario",
            usuarioId);

    public Task<int> ContarActivosPorUsuarioAsync(
        int usuarioId,
        CancellationToken ct = default) =>
        EjecutarConsultaAsync(
            () => _dbSet.CountAsync(
                item =>
                    item.UsuarioId == usuarioId &&
                    item.Estado == EstadoPrestamo.Activo,
                ct),
            "contar préstamos activos del usuario",
            usuarioId);

    public override async Task AgregarAsync(
        Prestamo prestamo,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Registrando préstamo para usuario {UsuarioId}, libro {LibroId} y ejemplar {EjemplarId}",
                prestamo.UsuarioId,
                prestamo.LibroId,
                prestamo.EjemplarId);
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
            _logger.LogInformation(
                "Préstamo {PrestamoId} y ejemplar {EjemplarId} relacionados correctamente",
                prestamo.Id,
                ejemplarId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Error al registrar préstamo para usuario {UsuarioId} y libro {LibroId}",
                prestamo.UsuarioId,
                prestamo.LibroId);
            throw;
        }
    }

    private async Task<IReadOnlyCollection<Prestamo>> ConsultarAsync(
        Func<IQueryable<Prestamo>> crearConsulta,
        CancellationToken ct,
        string operacion,
        int? entidadId = null)
    {
        try
        {
            _logger.LogInformation(
                "Iniciando consulta de {Operacion}. Identificador: {EntidadId}",
                operacion,
                entidadId);
            var resultados = await crearConsulta()
                .AsNoTracking()
                .Select(prestamo => new
                {
                    Prestamo = prestamo,
                    EjemplarId = _context.PrestamoEjemplares
                        .Where(relation => relation.PrestamoId == prestamo.Id)
                        .Select(relation => (int?)relation.EjemplarId)
                        .FirstOrDefault()
                })
                .ToListAsync(ct);
            foreach (var resultado in resultados)
            {
                if (resultado.EjemplarId.HasValue)
                    resultado.Prestamo.CargarEjemplarPersistido(resultado.EjemplarId.Value);
            }

            var prestamos = resultados.Select(resultado => resultado.Prestamo).ToArray();

            _logger.LogInformation(
                "Consulta de {Operacion} completada con {Cantidad} registros",
                operacion,
                prestamos.Length);
            return prestamos;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Error al consultar {Operacion}. Identificador: {EntidadId}",
                operacion,
                entidadId);
            throw;
        }
    }

    private async Task<T> EjecutarConsultaAsync<T>(
        Func<Task<T>> consulta,
        string operacion,
        int entidadId)
    {
        try
        {
            _logger.LogInformation(
                "Iniciando operación para {Operacion}. Identificador: {EntidadId}",
                operacion,
                entidadId);
            var resultado = await consulta();
            _logger.LogInformation(
                "Operación completada para {Operacion}. Identificador: {EntidadId}",
                operacion,
                entidadId);
            return resultado;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Error al {Operacion}. Identificador: {EntidadId}",
                operacion,
                entidadId);
            throw;
        }
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
