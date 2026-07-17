using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SIGEBI.Domain.Entities.Notificaciones;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SIGEBI.Persistence.Repositories.Notificaciones
{
    public sealed class NotificacionRepository : MutableRepository<Notificacion>, INotificacionRepository
    {
        public NotificacionRepository(SigebiContext context, ILogger<BaseRepository<Notificacion>> logger) : base(context, logger) { }

        public async Task<Notificacion?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            try { return await _dbSet.FindAsync(new object[] { id }, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error ID {Id}", id); throw; }
        }

        public async Task<IEnumerable<Notificacion>> ObtenerPorUsuarioAsync(int usuarioId)
        {
            try { return await _dbSet.Where(n => n.UsuarioId == usuarioId).OrderByDescending(n => n.FechaEnvio).ToListAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Error usuario {Id}", usuarioId); throw; }
        }

        public async Task<IEnumerable<Notificacion>> ObtenerNoLeidasPorUsuarioAsync(int usuarioId)
        {
            try { return await _dbSet.Where(n => n.UsuarioId == usuarioId && !n.Leida).ToListAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Error leidas usuario {Id}", usuarioId); throw; }
        }
    }
}