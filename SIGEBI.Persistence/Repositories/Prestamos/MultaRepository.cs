using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // B.R: Logger
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;
using System; // B.R: System para Exception
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SIGEBI.Persistence.Repositories.Prestamos
{
    // Patrón Repository y SRP: concentra las consultas y persistencia de multas.
    public class MultaRepository : MutableRepository<Multa>, IMultaRepository
    {
        public MultaRepository(SigebiContext context, ILogger<BaseRepository<Multa>> logger)
            : base(context, logger) { }

        public async Task<Multa?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                return await _dbSet.FindAsync(new object[] { id }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener multa por ID: {Id}", id);
                throw;
            }
        }

        public async Task<IReadOnlyCollection<Multa>> ObtenerPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
        {
            try
            {
                return await _dbSet
                    .Where(m => m.UsuarioId == usuarioId)
                    .OrderByDescending(m => m.FechaGeneracion)
                    .ToListAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar multas del usuario: {UsuarioId}", usuarioId);
                throw;
            }
        }

        public async Task<IReadOnlyCollection<Multa>> ObtenerPorEstadoAsync(EstadoMulta estado, CancellationToken ct = default)
        {
            try
            {
                return await _dbSet
                    .Where(m => m.Estado == estado)
                    .OrderByDescending(m => m.FechaGeneracion)
                    .ToListAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar multas por estado: {Estado}", estado);
                throw;
            }
        }

        public async Task<bool> TienePendientesPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
        {
            try
            {
                return await _dbSet.AnyAsync(
                    m => m.UsuarioId == usuarioId && m.Estado == EstadoMulta.Pendiente,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar multas pendientes del usuario: {UsuarioId}", usuarioId);
                throw;
            }
        }
    }
}