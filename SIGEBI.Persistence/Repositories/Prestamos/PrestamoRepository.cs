using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;

namespace SIGEBI.Persistence.Repositories.Prestamos
{
    // Patrón Repository y SRP: encapsula únicamente el acceso a datos de préstamos.
    public class PrestamoRepository : MutableRepository<Prestamo>, IPrestamoRepository
    {
        public PrestamoRepository(SigebiContext context) : base(context) { }

        public async Task<Prestamo?> ObtenerPorIdAsync(
            int id,
            CancellationToken cancellationToken = default)
            => await _dbSet.FindAsync([id], cancellationToken);

        public async Task<IReadOnlyCollection<Prestamo>> ObtenerPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default)
            => await _dbSet
                .Where(p => p.UsuarioId == usuarioId)
                .OrderByDescending(p => p.FechaPrestamo)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyCollection<Prestamo>> ObtenerPorEstadoAsync(
            EstadoPrestamo estado,
            CancellationToken cancellationToken = default)
            => await _dbSet
                .Where(p => p.Estado == estado)
                .OrderByDescending(p => p.FechaPrestamo)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyCollection<Prestamo>> ObtenerActivosVencidosAsync(
            DateTime fechaReferencia,
            CancellationToken cancellationToken = default)
            => await _dbSet
                .Where(p => p.Estado == EstadoPrestamo.Activo
                    && p.FechaEsperadaDevolucion < fechaReferencia)
                .OrderBy(p => p.FechaEsperadaDevolucion)
                .ToListAsync(cancellationToken);

        public Task<int> ContarActivosPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default)
            => _dbSet.CountAsync(
                p => p.UsuarioId == usuarioId && p.Estado == EstadoPrestamo.Activo,
                cancellationToken);

        public Task<bool> TieneVencidosPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            var fechaActual = DateTime.UtcNow;

            return _dbSet.AnyAsync(
                p => p.UsuarioId == usuarioId
                    && (p.Estado == EstadoPrestamo.Vencido
                        || (p.Estado == EstadoPrestamo.Activo
                            && p.FechaEsperadaDevolucion < fechaActual)),
                cancellationToken);
        }
    }
}
