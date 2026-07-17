using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Enums;
using SIGEBI.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AuditoriaEntidad = SIGEBI.Domain.Entities.Auditoria.Auditoria;

namespace SIGEBI.Persistence.Repositories.Auditoria
{
    public sealed class AuditoriaRepository : IAuditoriaRepository
    {
        private readonly SigebiContext _context;
        private readonly DbSet<AuditoriaEntidad> _auditorias;
        private readonly ILogger<AuditoriaRepository> _logger;

        public AuditoriaRepository(SigebiContext context, ILogger<AuditoriaRepository> logger)
        {
            _context = context;
            _auditorias = context.Set<AuditoriaEntidad>();
            _logger = logger;
        }

        public async Task<AuditoriaEntidad?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            try { return await _auditorias.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error buscando Auditoría ID {Id}", id); throw; }
        }

        public async Task<IReadOnlyCollection<AuditoriaEntidad>> ObtenerTodasAsync(CancellationToken ct = default)
        {
            try { return await _auditorias.AsNoTracking().OrderByDescending(a => a.Fecha).ToListAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error listando todas las auditorías"); throw; }
        }

        public async Task<IReadOnlyCollection<AuditoriaEntidad>> ObtenerPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
        {
            try { return await _auditorias.AsNoTracking().Where(a => a.UsuarioResponsableId == usuarioId).OrderByDescending(a => a.Fecha).ToListAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error listando auditorías usuario {Id}", usuarioId); throw; }
        }

        public async Task<IReadOnlyCollection<AuditoriaEntidad>> ObtenerPorModuloAsync(ModuloAuditoria modulo, CancellationToken ct = default)
        {
            try { return await _auditorias.AsNoTracking().Where(a => a.Modulo == modulo).OrderByDescending(a => a.Fecha).ToListAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error listando auditorías módulo {M}", modulo); throw; }
        }

        public async Task<IReadOnlyCollection<AuditoriaEntidad>> ObtenerPorRangoAsync(DateTime desde, DateTime hasta, CancellationToken ct = default)
        {
            try
            {
                if (hasta < desde) throw new ArgumentException("Rango de fechas inválido");
                return await _auditorias.AsNoTracking().Where(a => a.Fecha >= desde && a.Fecha <= hasta).OrderByDescending(a => a.Fecha).ToListAsync(ct);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error en rango auditoría {Desde} - {Hasta}", desde, hasta); throw; }
        }

        public async Task AgregarAsync(AuditoriaEntidad auditoria, CancellationToken ct = default)
        {
            try { await _auditorias.AddAsync(auditoria, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error al registrar nueva auditoría"); throw; }
        }
    }
}