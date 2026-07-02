using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;

namespace SIGEBI.Persistence.Repositories.Prestamos
{
    // Patrón Repository y SRP: concentra las consultas y persistencia de multas.
    public class MultaRepository : MutableRepository<Multa>, IMultaRepository
    {
        public MultaRepository(SigebiContext context) : base(context) { }

        public async Task<Multa?> ObtenerPorIdAsync(
            int id,
            CancellationToken cancellationToken = default)
            => await _dbSet.FindAsync([id], cancellationToken);

        public async Task<IReadOnlyCollection<Multa>> ObtenerPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default)
            => await _dbSet
                .Where(m => m.UsuarioId == usuarioId)
                .OrderByDescending(m => m.FechaGeneracion)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyCollection<Multa>> ObtenerPorEstadoAsync(
            EstadoMulta estado,
            CancellationToken cancellationToken = default)
            => await _dbSet
                .Where(m => m.Estado == estado)
                .OrderByDescending(m => m.FechaGeneracion)
                .ToListAsync(cancellationToken);

        public Task<bool> TienePendientesPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default)
            => _dbSet.AnyAsync(
                m => m.UsuarioId == usuarioId && m.Estado == EstadoMulta.Pendiente,
                cancellationToken);
    }
}
