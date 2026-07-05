using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Application.Interfaces.Notificaciones;
using SIGEBI.Domain.Entities.Notificaciones;
using SIGEBI.Domain.Interfaces.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIGEBI.Application.Services.Notificaciones
{
    public class NotificacionService : BaseService<Notificacion, NotificacionDto>, INotificacionService
    {
        private readonly INotificacionRepository _notificacionRepository;

        // 1. Se agregó IMapper al constructor y se pasó a la base
        public NotificacionService(INotificacionRepository notificacionRepository, IMapper mapper)
            : base(notificacionRepository, mapper)
        {
            _notificacionRepository = notificacionRepository;
        }

        public async Task<IEnumerable<NotificacionDto>> ObtenerPorUsuarioAsync(int usuarioId)
        {
            var lista = await _notificacionRepository.ObtenerPorUsuarioAsync(usuarioId);

            // 2. Mapeo real usando AutoMapper
            return _mapper.Map<IEnumerable<NotificacionDto>>(lista);
        }

        public async Task<IEnumerable<NotificacionDto>> ObtenerNoLeidasPorUsuarioAsync(int usuarioId)
        {
            var lista = await _notificacionRepository.ObtenerNoLeidasPorUsuarioAsync(usuarioId);

            // 2. Mapeo real usando AutoMapper
            return _mapper.Map<IEnumerable<NotificacionDto>>(lista);
        }

        public async Task<bool> EnviarNotificacionAsync(SaveNotificacionDto dto)
        {
            // Lógica para instanciar la entidad Notificacion y guardarla
            return true;
        }

        public async Task<bool> MarcarComoLeidaAsync(int notificacionId)
        {
            // Lógica para actualizar el estado de la notificación
            return true;
        }
    }
}