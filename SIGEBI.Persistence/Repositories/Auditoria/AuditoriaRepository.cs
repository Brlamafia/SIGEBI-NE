using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Enums;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;
using AuditoriaEntidad = SIGEBI.Domain.Entities.Auditoria.Auditoria;

namespace SIGEBI.Persistence.Repositories.Auditoria
{
    // Patrón Repository e inmutabilidad: permite registrar y consultar auditorías, nunca modificarlas.
    public class AuditoriaRepository : BaseRepository<AuditoriaEntidad>, IAuditoriaRepository
    {
        public AuditoriaRepository(SigebiContext context) : base(context) { }

        public async Task<AuditoriaEntidad?> ObtenerPorIdAsync(
            int id,
            CancellationToken cancellationToken = default)
            => await _dbSet.FindAsync([id], cancellationToken);

        public async Task<IReadOnlyCollection<AuditoriaEntidad>> ObtenerPorUsuarioAsync(
            int usuarioResponsableId,
            CancellationToken cancellationToken = default)
            => await _dbSet
                .Where(a => a.UsuarioResponsableId == usuarioResponsableId)
                .OrderByDescending(a => a.Fecha)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyCollection<AuditoriaEntidad>> ObtenerPorModuloAsync(
            ModuloAuditoria modulo,
            CancellationToken cancellationToken = default)
            => await _dbSet
                .Where(a => a.Modulo == modulo)
                .OrderByDescending(a => a.Fecha)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyCollection<AuditoriaEntidad>> ObtenerPorRangoAsync(
            DateTime fechaDesde,
            DateTime fechaHasta,
            CancellationToken cancellationToken = default)
        {
            if (fechaHasta < fechaDesde)
            {
                throw new ArgumentException(
                    "La fecha final no puede ser anterior a la fecha inicial.",
                    nameof(fechaHasta));
            }

            return await _dbSet
                .Where(a => a.Fecha >= fechaDesde && a.Fecha <= fechaHasta)
                .OrderByDescending(a => a.Fecha)
                .ToListAsync(cancellationToken);
        }
    }
}
