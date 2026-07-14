using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Enums;
using SIGEBI.Persistence.Context;
using AuditoriaEntidad = SIGEBI.Domain.Entities.Auditoria.Auditoria;

namespace SIGEBI.Persistence.Repositories.Auditoria
{
    // Patrón Repository e inmutabilidad: permite registrar y consultar auditorías, nunca modificarlas.
    public sealed class AuditoriaRepository : IAuditoriaRepository
    {
        private readonly SigebiContext _context;
        private readonly DbSet<AuditoriaEntidad> _auditorias;

        public AuditoriaRepository(SigebiContext context)
        {
            _context = context;
            _auditorias = context.Set<AuditoriaEntidad>();
        }

        public async Task<AuditoriaEntidad?> ObtenerPorIdAsync(
            int id,
            CancellationToken cancellationToken = default)
            => await _auditorias.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

        public async Task<IReadOnlyCollection<AuditoriaEntidad>> ObtenerTodasAsync(
            CancellationToken cancellationToken = default)
            => await _auditorias.AsNoTracking()
                .OrderByDescending(a => a.Fecha)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyCollection<AuditoriaEntidad>> ObtenerPorUsuarioAsync(
            int usuarioResponsableId,
            CancellationToken cancellationToken = default)
            => await _auditorias.AsNoTracking()
                .Where(a => a.UsuarioResponsableId == usuarioResponsableId)
                .OrderByDescending(a => a.Fecha)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyCollection<AuditoriaEntidad>> ObtenerPorModuloAsync(
            ModuloAuditoria modulo,
            CancellationToken cancellationToken = default)
            => await _auditorias.AsNoTracking()
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

            return await _auditorias.AsNoTracking()
                .Where(a => a.Fecha >= fechaDesde && a.Fecha <= fechaHasta)
                .OrderByDescending(a => a.Fecha)
                .ToListAsync(cancellationToken);
        }

        public async Task AgregarAsync(
            AuditoriaEntidad auditoria,
            CancellationToken cancellationToken = default)
            => await _auditorias.AddAsync(auditoria, cancellationToken);
    }
}
