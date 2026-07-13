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
            // Solución CS0272: Obtenemos el DTO en lugar de la Entidad de dominio.
            // Los DTOs sí permiten modificar sus propiedades públicamente.
            var dto = await base.GetByIdAsync(notificacionId);

            if (dto == null)
                throw new BusinessRuleException("La notificación solicitada no existe.");

            // Modificamos el estado en el DTO
            dto.Leida = true;

            // Solución CS1061: Enviamos el DTO actualizado al motor base para que lo persista.
            await base.UpdateAsync(notificacionId, dto);

            return true;
        }
    }
}