using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Notificaciones;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIGEBI.Application.Interfaces.Notificaciones
{
    public interface INotificacionService : IBaseService<NotificacionDto>
    {
        Task<IEnumerable<NotificacionDto>> ObtenerPorUsuarioAsync(int usuarioId);
        Task<IEnumerable<NotificacionDto>> ObtenerNoLeidasPorUsuarioAsync(int usuarioId);
        Task<bool> EnviarNotificacionAsync(SaveNotificacionDto dto);
        Task<bool> MarcarComoLeidaAsync(int notificacionId);
    }
}