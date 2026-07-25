using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SIGEBI.Persistence.Repositories.Prestamos
{
    public class PrestamoRepository : MutableRepository<Prestamo>, IPrestamoRepository
    {
        public PrestamoRepository(SigebiContext context, ILogger<PrestamoRepository> logger)
            : base(context, logger) { }

        public async Task<Prestamo?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando préstamo {PrestamoId}", id); return await _dbSet.FindAsync(new object[] { id }, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error al obtener préstamo ID {Id}", id); throw; }
        }

        public async Task<IReadOnlyCollection<Prestamo>> ObtenerPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando préstamos del usuario {UsuarioId}", usuarioId); return await _dbSet.Where(p => p.UsuarioId == usuarioId).OrderByDescending(p => p.FechaPrestamo).ToListAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error al listar préstamos usuario {Id}", usuarioId); throw; }
        }

        public async Task<IReadOnlyCollection<Prestamo>> ObtenerPorLibroAsync(int libroId, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando préstamos del libro {LibroId}", libroId); return await _dbSet.Where(p => p.LibroId == libroId).OrderByDescending(p => p.FechaPrestamo).ToListAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error al listar préstamos del libro {LibroId}", libroId); throw; }
        }

        public async Task<IReadOnlyCollection<Prestamo>> ObtenerPorEjemplarAsync(int ejemplarId, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando préstamos del ejemplar {EjemplarId}", ejemplarId); return await _dbSet.Where(p => p.EjemplarId == ejemplarId).OrderByDescending(p => p.FechaPrestamo).ToListAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error al listar préstamos del ejemplar {EjemplarId}", ejemplarId); throw; }
        }

        public async Task<IReadOnlyCollection<Prestamo>> ObtenerDevolucionesPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando devoluciones del usuario {UsuarioId}", usuarioId); return await _dbSet.Where(p => p.UsuarioId == usuarioId && p.FechaRealDevolucion != null).OrderByDescending(p => p.FechaRealDevolucion).ToListAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error al listar devoluciones del usuario {UsuarioId}", usuarioId); throw; }
        }

        public async Task<IReadOnlyCollection<Prestamo>> ObtenerDevolucionesPorLibroAsync(int libroId, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando devoluciones del libro {LibroId}", libroId); return await _dbSet.Where(p => p.LibroId == libroId && p.FechaRealDevolucion != null).OrderByDescending(p => p.FechaRealDevolucion).ToListAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error al listar devoluciones del libro {LibroId}", libroId); throw; }
        }

        public async Task<IReadOnlyCollection<Prestamo>> ObtenerPorEstadoAsync(EstadoPrestamo estado, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando préstamos en estado {Estado}", estado); return await _dbSet.Where(p => p.Estado == estado).OrderByDescending(p => p.FechaPrestamo).ToListAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error al listar préstamos estado {Estado}", estado); throw; }
        }

        public async Task<IReadOnlyCollection<Prestamo>> ObtenerPorRangoAsync(DateTime desde, DateTime hasta, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando préstamos entre {Desde} y {Hasta}", desde, hasta); return await _dbSet.Where(p => p.FechaPrestamo >= desde && p.FechaPrestamo <= hasta).ToListAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error en rango fechas"); throw; }
        }

        public async Task<IReadOnlyCollection<Prestamo>> ObtenerActivosVencidosAsync(DateTime fechaReferencia, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando préstamos vencidos a {Fecha}", fechaReferencia); return await _dbSet.Where(p => p.Estado == EstadoPrestamo.Activo && p.FechaEsperadaDevolucion < fechaReferencia).ToListAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error obteniendo vencidos"); throw; }
        }

        public async Task<IReadOnlyCollection<Prestamo>> ObtenerActivosProximosAVencerAsync(
            DateTime desde,
            DateTime hasta,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Consultando préstamos próximos a vencer entre {Desde} y {Hasta}", desde, hasta);
                return await _dbSet
                    .Where(p => p.Estado == EstadoPrestamo.Activo &&
                                p.FechaEsperadaDevolucion >= desde &&
                                p.FechaEsperadaDevolucion <= hasta)
                    .OrderBy(p => p.FechaEsperadaDevolucion)
                    .ToListAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo préstamos próximos a vencer");
                throw;
            }
        }

        public async Task<bool> TieneVencidosPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Verificando préstamos vencidos del usuario {UsuarioId}", usuarioId); return await _dbSet.AnyAsync(p => p.UsuarioId == usuarioId && (p.Estado == EstadoPrestamo.Vencido || (p.Estado == EstadoPrestamo.Activo && p.FechaEsperadaDevolucion < DateTime.UtcNow)), ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error verificando vencidos usuario {Id}", usuarioId); throw; }
        }

        public async Task<int> ContarActivosPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Contando préstamos activos del usuario {UsuarioId}", usuarioId); return await _dbSet.CountAsync(p => p.UsuarioId == usuarioId && p.Estado == EstadoPrestamo.Activo, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error contando activos usuario {Id}", usuarioId); throw; }
        }
    }
}
