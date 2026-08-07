using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Notificaciones;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIGEBI.Application.Interfaces.Notificaciones
{
    public interface INotificacionService : IBaseService<NotificacionDto>
    {
        Task<IEnumerable<NotificacionDto>> ObtenerPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<NotificacionDto>> ObtenerPorUsuarioAsync(
            int usuarioId,
            int pagina,
            int tamanoPagina,
            CancellationToken cancellationToken = default);
        Task<IEnumerable<NotificacionDto>> ObtenerNoLeidasPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default);
        Task<bool> EnviarNotificacionAsync(
            SaveNotificacionDto dto,
            CancellationToken cancellationToken = default);
        Task<bool> EnviarSiNoExisteAsync(
            SaveNotificacionDto dto,
            string textoIdentificador,
            DateTime desde,
            CancellationToken cancellationToken = default);
        Task<bool> MarcarComoLeidaAsync(
            int notificacionId,
            CancellationToken cancellationToken = default);
        Task<int> MarcarTodasComoLeidasAsync(
            CancellationToken cancellationToken = default);
    }
}
