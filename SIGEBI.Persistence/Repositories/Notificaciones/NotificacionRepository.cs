using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Entities.Notificaciones;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;

namespace SIGEBI.Persistence.Repositories.Notificaciones;

public sealed class NotificacionRepository : MutableRepository<Notificacion>, INotificacionRepository
{
    public NotificacionRepository(SigebiContext context) : base(context)
    {
    }

    public async Task<Notificacion?> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken = default)
        => await _dbSet.FindAsync([id], cancellationToken);

    public async Task<IEnumerable<Notificacion>> ObtenerPorUsuarioAsync(int usuarioId)
        => await _dbSet
            .Where(notificacion => notificacion.UsuarioId == usuarioId)
            .OrderByDescending(notificacion => notificacion.FechaEnvio)
            .ToListAsync();

    public async Task<IEnumerable<Notificacion>> ObtenerNoLeidasPorUsuarioAsync(int usuarioId)
        => await _dbSet
            .Where(notificacion =>
                notificacion.UsuarioId == usuarioId && !notificacion.Leida)
            .OrderByDescending(notificacion => notificacion.FechaEnvio)
            .ToListAsync();
}
