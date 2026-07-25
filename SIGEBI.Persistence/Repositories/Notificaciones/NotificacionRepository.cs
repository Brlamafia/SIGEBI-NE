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
        public NotificacionRepository(SigebiContext context, ILogger<NotificacionRepository> logger) : base(context, logger) { }

        public async Task<Notificacion?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando notificación {NotificacionId}", id); return await _dbSet.FindAsync(new object[] { id }, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error ID {Id}", id); throw; }
        }

        public async Task<IEnumerable<Notificacion>> ObtenerPorUsuarioAsync(
            int usuarioId,
            CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando notificaciones del usuario {UsuarioId}", usuarioId); return await _dbSet.Where(n => n.UsuarioId == usuarioId).OrderByDescending(n => n.FechaEnvio).ToListAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error usuario {Id}", usuarioId); throw; }
        }

        public async Task<IEnumerable<Notificacion>> ObtenerNoLeidasPorUsuarioAsync(
            int usuarioId,
            CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando notificaciones no leídas del usuario {UsuarioId}", usuarioId); return await _dbSet.Where(n => n.UsuarioId == usuarioId && !n.Leida).ToListAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error leidas usuario {Id}", usuarioId); throw; }
        }

        public async Task<bool> ExisteEventoAsync(
            int usuarioId,
            string textoIdentificador,
            DateTime desde,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Comprobando notificación previa del usuario {UsuarioId}", usuarioId);
                return await _dbSet.AnyAsync(
                    n => n.UsuarioId == usuarioId &&
                         n.FechaEnvio >= desde &&
                         n.Mensaje.Contains(textoIdentificador),
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error comprobando evento de notificación para usuario {UsuarioId}",
                    usuarioId);
                throw;
            }
        }
    }
}
