using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Application.Interfaces.Notificaciones;
using SIGEBI.Domain.Entities.Notificaciones;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Application.Exceptions;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;

namespace SIGEBI.Application.Services.Notificaciones
{
    public class NotificacionService : BaseService<Notificacion, NotificacionDto>, INotificacionService
    {
        private readonly INotificacionRepository _notificacionRepository;
        private readonly IAuditoriaWriter _auditoria;
        private readonly IUsuarioActual _usuarioActual;
        private readonly IUnitOfWork _unitOfWork;

        public NotificacionService(
            INotificacionRepository notificacionRepository,
            IAuditoriaWriter auditoria,
            IUsuarioActual usuarioActual,
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : base(notificacionRepository, mapper)
        {
            _notificacionRepository = notificacionRepository;
            _auditoria = auditoria;
            _usuarioActual = usuarioActual;
            _unitOfWork = unitOfWork;
        }

        public override async Task<NotificacionDto> AddAsync<TSaveDto>(TSaveDto dto)
        {
            if (dto is not SaveNotificacionDto notificacion)
                throw new ArgumentException("El contrato de notificación no es válido.", nameof(dto));
            NotificacionDto? creada = null;
            await _unitOfWork.EjecutarEnTransaccionAsync(async cancellationToken =>
            {
                creada = await base.AddAsync(notificacion);
                await AuditarAsync(
                    AccionAuditoria.Registrar,
                    $"Notificación manual enviada al usuario {notificacion.UsuarioId}.",
                    cancellationToken);
            });
            return creada ?? throw new InvalidOperationException("No se pudo crear la notificación.");
        }

        public async Task<IEnumerable<NotificacionDto>> ObtenerPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            var lista = await _notificacionRepository.ObtenerPorUsuarioAsync(usuarioId, cancellationToken);
            return _mapper.Map<IEnumerable<NotificacionDto>>(lista);
        }

        public async Task<IReadOnlyCollection<NotificacionDto>> ObtenerPorUsuarioAsync(
            int usuarioId,
            int pagina,
            int tamanoPagina,
            CancellationToken cancellationToken = default)
        {
            if (pagina <= 0)
                throw new ArgumentOutOfRangeException(nameof(pagina));
            if (tamanoPagina is <= 0 or > 200)
                throw new ArgumentOutOfRangeException(nameof(tamanoPagina));
            var lista = await _notificacionRepository.ObtenerPorUsuarioAsync(
                usuarioId,
                (pagina - 1) * tamanoPagina,
                tamanoPagina,
                cancellationToken);
            return _mapper.Map<IReadOnlyCollection<NotificacionDto>>(lista);
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
            if (notificacion.UsuarioId != _usuarioActual.UsuarioId &&
                !_usuarioActual.TieneRol("Administrador") &&
                !_usuarioActual.TieneRol("Auditor"))
            {
                throw new BusinessRuleException(
                    "La notificación no pertenece al usuario autenticado.");
            }

            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                notificacion.MarcarComoLeida();
                await _notificacionRepository.ActualizarAsync(notificacion);
                await AuditarAsync(
                    AccionAuditoria.ActualizarEstado,
                    $"Notificación {notificacionId} marcada como leída.",
                    ct);
            }, cancellationToken);

            return true;
        }

        private Task AuditarAsync(
            AccionAuditoria accion,
            string descripcion,
            CancellationToken cancellationToken)
        {
            if (!_usuarioActual.EstaAutenticado)
                throw new BusinessRuleException("No se pudo determinar el usuario responsable.");
            return _auditoria.RegistrarAsync(
                _usuarioActual.UsuarioId,
                ModuloAuditoria.Notificaciones,
                accion,
                descripcion,
                cancellationToken: cancellationToken);
        }
    }
}
