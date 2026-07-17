using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Enums;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SIGEBI.Persistence.Repositories.Prestamos
{
    public class SolicitudPrestamoRepository : MutableRepository<SolicitudPrestamo>, ISolicitudPrestamoRepository
    {
        public SolicitudPrestamoRepository(SigebiContext context, ILogger<BaseRepository<SolicitudPrestamo>> logger) : base(context, logger) { }

        public async Task<SolicitudPrestamo?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            try { return await _dbSet.FindAsync(new object[] { id }, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error ID {Id}", id); throw; }
        }

        public async Task<IEnumerable<SolicitudPrestamo>> ObtenerPorUsuarioAsync(int usuarioId)
        {
            try { return await _dbSet.Where(s => s.UsuarioId == usuarioId).ToListAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Error usuario {Id}", usuarioId); throw; }
        }

        public async Task<IEnumerable<SolicitudPrestamo>> ObtenerPorEstadoAsync(EstadoSolicitud estado)
        {
            try { return await _dbSet.Where(s => s.Estado == estado).ToListAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Error estado {E}", estado); throw; }
        }
    }
}