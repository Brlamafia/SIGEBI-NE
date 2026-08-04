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
        private const int TamanoPaginaCompatibilidad = 200;
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
            try { _logger.LogInformation("Consultando auditoría {AuditoriaId}", id); return await _auditorias.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error buscando Auditoría ID {Id}", id); throw; }
        }

        public async Task<IReadOnlyCollection<AuditoriaEntidad>> ObtenerTodasAsync(CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando la primera página de auditoría"); return await FiltrarPaginaAsync(0, TamanoPaginaCompatibilidad, ct: ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error listando todas las auditorías"); throw; }
        }

        public async Task<IReadOnlyCollection<AuditoriaEntidad>> ObtenerPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando auditoría del usuario {UsuarioId}", usuarioId); return await FiltrarPaginaAsync(0, TamanoPaginaCompatibilidad, usuarioResponsableId: usuarioId, ct: ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error listando auditorías usuario {Id}", usuarioId); throw; }
        }

        public async Task<IReadOnlyCollection<AuditoriaEntidad>> ObtenerPorModuloAsync(ModuloAuditoria modulo, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando auditoría del módulo {Modulo}", modulo); return await FiltrarPaginaAsync(0, TamanoPaginaCompatibilidad, modulo: modulo, ct: ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error listando auditorías módulo {M}", modulo); throw; }
        }

        public async Task<IReadOnlyCollection<AuditoriaEntidad>> ObtenerPorRangoAsync(DateTime desde, DateTime hasta, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Consultando auditoría entre {Desde} y {Hasta}", desde, hasta);
                if (hasta < desde) throw new ArgumentException("Rango de fechas inválido");
                return await FiltrarPaginaAsync(0, TamanoPaginaCompatibilidad, fechaDesde: desde, fechaHasta: hasta, ct: ct);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error en rango auditoría {Desde} - {Hasta}", desde, hasta); throw; }
        }

        public async Task<IReadOnlyCollection<AuditoriaEntidad>> FiltrarPaginaAsync(
            int skip,
            int take,
            int? usuarioResponsableId = null,
            ModuloAuditoria? modulo = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            CancellationToken ct = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(skip);
            if (take is <= 0 or > 200)
                throw new ArgumentOutOfRangeException(nameof(take));
            if (fechaDesde.HasValue != fechaHasta.HasValue)
                throw new ArgumentException("Debe indicar el rango de fechas completo.");
            if (fechaDesde.HasValue && fechaHasta < fechaDesde)
                throw new ArgumentException("Rango de fechas inválido.");

            try
            {
                IQueryable<AuditoriaEntidad> query = _auditorias.AsNoTracking();
                if (usuarioResponsableId.HasValue)
                    query = query.Where(a => a.UsuarioResponsableId == usuarioResponsableId.Value);
                if (modulo.HasValue)
                    query = query.Where(a => a.Modulo == modulo.Value);
                if (fechaDesde.HasValue)
                    query = query.Where(a =>
                        a.Fecha >= fechaDesde.Value && a.Fecha <= fechaHasta!.Value);

                _logger.LogInformation(
                    "Consultando auditoría paginada. Desplazamiento {Skip}, tamaño {Take}",
                    skip,
                    take);
                return await query
                    .OrderByDescending(a => a.Fecha)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando auditoría paginada");
                throw;
            }
        }

        public async Task AgregarAsync(AuditoriaEntidad auditoria, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Registrando auditoría de {Modulo}/{Accion}", auditoria.Modulo, auditoria.Accion); await _auditorias.AddAsync(auditoria, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error al registrar nueva auditoría"); throw; }
        }

        public async Task AgregarRangoAsync(
            IEnumerable<AuditoriaEntidad> auditorias,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(auditorias);
            var lote = auditorias as IReadOnlyCollection<AuditoriaEntidad>
                ?? auditorias.ToArray();
            if (lote.Count == 0)
                return;

            try
            {
                _logger.LogInformation(
                    "Registrando un lote de {Cantidad} eventos de auditoría",
                    lote.Count);
                await _auditorias.AddRangeAsync(lote, ct);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al registrar el lote de auditoría");
                throw;
            }
        }
    }
}
