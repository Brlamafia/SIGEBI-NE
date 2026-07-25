using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Application.Interfaces.Notificaciones;
using SIGEBI.Domain.Entities.Notificaciones;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Application.Exceptions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIGEBI.Application.Services.Notificaciones
{
    public class NotificacionService : BaseService<Notificacion, NotificacionDto>, INotificacionService
    {
        private readonly INotificacionRepository _notificacionRepository;

        public NotificacionService(INotificacionRepository notificacionRepository, IMapper mapper)
            : base(notificacionRepository, mapper)
        {
            _notificacionRepository = notificacionRepository;
        }

        public async Task<IEnumerable<NotificacionDto>> ObtenerPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            var lista = await _notificacionRepository.ObtenerPorUsuarioAsync(usuarioId, cancellationToken);
            return _mapper.Map<IEnumerable<NotificacionDto>>(lista);
        }

        public async Task<IEnumerable<NotificacionDto>> ObtenerNoLeidasPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            var lista = await _notificacionRepository.ObtenerNoLeidasPorUsuarioAsync(usuarioId, cancellationToken);
            return _mapper.Map<IEnumerable<NotificacionDto>>(lista);
        }

        public async Task<bool> EnviarNotificacionAsync(
            SaveNotificacionDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            var entidad = _mapper.Map<Notificacion>(dto);
            await _notificacionRepository.AgregarAsync(entidad, cancellationToken);
            return true;
        }

        public async Task<bool> EnviarSiNoExisteAsync(
            SaveNotificacionDto dto,
            string textoIdentificador,
            DateTime desde,
            CancellationToken cancellationToken = default)
        {
            if (await _notificacionRepository.ExisteEventoAsync(
                    dto.UsuarioId,
                    textoIdentificador,
                    desde,
                    cancellationToken))
                return false;

            return await EnviarNotificacionAsync(dto, cancellationToken);
        }

        public async Task<bool> MarcarComoLeidaAsync(
            int notificacionId,
            CancellationToken cancellationToken = default)
        {
            var notificacion = await _notificacionRepository.ObtenerPorIdAsync(notificacionId, cancellationToken)
                ?? throw new BusinessRuleException("La notificación solicitada no existe.");

            notificacion.MarcarComoLeida();
            await _notificacionRepository.ActualizarAsync(notificacion);

            return true;
        }
    }
}
