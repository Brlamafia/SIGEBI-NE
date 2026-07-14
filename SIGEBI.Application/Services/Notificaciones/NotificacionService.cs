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

        public async Task<IEnumerable<NotificacionDto>> ObtenerPorUsuarioAsync(int usuarioId)
        {
            var lista = await _notificacionRepository.ObtenerPorUsuarioAsync(usuarioId);
            return _mapper.Map<IEnumerable<NotificacionDto>>(lista);
        }

        public async Task<IEnumerable<NotificacionDto>> ObtenerNoLeidasPorUsuarioAsync(int usuarioId)
        {
            var lista = await _notificacionRepository.ObtenerNoLeidasPorUsuarioAsync(usuarioId);
            return _mapper.Map<IEnumerable<NotificacionDto>>(lista);
        }

        public async Task<bool> EnviarNotificacionAsync(SaveNotificacionDto dto)
        {
            // Solución CS1061 y CS0272: El BaseService recibe el DTO, AutoMapper lo convierte
            // y utiliza el repositorio genérico interno que sí tiene permisos para guardar.
            await base.AddAsync(dto);
            return true;
        }

        public async Task<bool> MarcarComoLeidaAsync(int notificacionId)
        {
            var notificacion = await _notificacionRepository.ObtenerPorIdAsync(notificacionId)
                ?? throw new BusinessRuleException("La notificación solicitada no existe.");

            notificacion.MarcarComoLeida();
            await _notificacionRepository.ActualizarAsync(notificacion);

            return true;
        }
    }
}
