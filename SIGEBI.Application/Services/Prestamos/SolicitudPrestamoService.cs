using AutoMapper;
using Microsoft.Extensions.Logging;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Notificaciones;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.SolicitudesPrestamo;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Exceptions;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Policies;
using System.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIGEBI.Application.Services.Prestamos
{
    public class SolicitudPrestamoService : BaseService<SolicitudPrestamo, SolicitudPrestamoDto>, ISolicitudPrestamoService
    {
        private readonly ISolicitudPrestamoRepository _solicitudRepository;
        private readonly IRepository<Libro> _libroRepository;
        private readonly IRepository<Usuario> _usuarioRepository;
        private readonly INotificacionService _notificacionService;
        private readonly IMultaRepository _multas;
        private readonly IPrestamoRepository _prestamos;
        private readonly IInventarioRepository _inventarios;
        private readonly IAuditoriaWriter _auditoria;
        private readonly IUnitOfWork _unitOfWork;
        private readonly PoliticaPrestamos _politica;
        private readonly ILogger<SolicitudPrestamoService> _logger;

        public SolicitudPrestamoService(
            ISolicitudPrestamoRepository repository,
            IRepository<Libro> libroRepository,
            IRepository<Usuario> usuarioRepository,
            INotificacionService notificacionService,
            IMultaRepository multas,
            IPrestamoRepository prestamos,
            IInventarioRepository inventarios,
            IAuditoriaWriter auditoria,
            IUnitOfWork unitOfWork,
            PoliticaPrestamos politica,
            IMapper mapper,
            ILogger<SolicitudPrestamoService> logger) : base(repository, mapper)
        {
            _solicitudRepository = repository;
            _libroRepository = libroRepository;
            _usuarioRepository = usuarioRepository;
            _notificacionService = notificacionService;
            _multas = multas;
            _prestamos = prestamos;
            _inventarios = inventarios;
            _auditoria = auditoria;
            _unitOfWork = unitOfWork;
            _politica = politica;
            _logger = logger;
        }

        public async Task<IEnumerable<SolicitudPrestamoDto>> ObtenerPorUsuarioAsync(int usuarioId)
        {
            var solicitudes = await _solicitudRepository.ObtenerPorUsuarioAsync(usuarioId);
            return _mapper.Map<IEnumerable<SolicitudPrestamoDto>>(solicitudes);
        }

        public async Task<IEnumerable<SolicitudPrestamoDto>> ObtenerPorEstadoAsync(string estado)
        {
            if (!Enum.TryParse<EstadoSolicitud>(estado, true, out var parsed) ||
                !Enum.IsDefined(parsed))
            {
                throw new BusinessRuleException("El estado de solicitud no es válido.");
            }

            var solicitudes = await _solicitudRepository.ObtenerPorEstadoAsync(parsed);
            return _mapper.Map<IEnumerable<SolicitudPrestamoDto>>(solicitudes);
        }

        public async Task<bool> RegistrarSolicitudAsync(SaveSolicitudPrestamoDto dto)
        {
            try
            {
                await _unitOfWork.EjecutarEnTransaccionAsync(async cancellationToken =>
                {
                    var usuario = await _usuarioRepository.GetByIdAsync(dto.UsuarioId)
                        ?? throw new BusinessRuleException("El usuario especificado no existe.");
                    if (usuario.Estado != EstadoUsuario.Activo)
                        throw new BusinessRuleException("El usuario no está activo y no puede solicitar préstamos.");

                    var libro = await _libroRepository.GetByIdAsync(dto.LibroId)
                        ?? throw new BusinessRuleException("El libro solicitado no existe.");
                    if (libro.Estado.Equals("Descatalogado", StringComparison.OrdinalIgnoreCase))
                        throw new BusinessRuleException("El libro solicitado está descatalogado.");

                    var inventario = await _inventarios.ObtenerPorLibroIdAsync(dto.LibroId, cancellationToken);
                    if (inventario is null || !inventario.TieneDisponibilidad)
                        throw new BusinessRuleException("No existen ejemplares disponibles para este libro.");
                    if (await _multas.TienePendientesPorUsuarioAsync(dto.UsuarioId, cancellationToken))
                        throw new BusinessRuleException("El usuario tiene multas pendientes.");
                    if (await _prestamos.TieneVencidosPorUsuarioAsync(dto.UsuarioId, cancellationToken))
                        throw new BusinessRuleException("El usuario tiene préstamos vencidos.");
                    if (await _prestamos.ContarActivosPorUsuarioAsync(dto.UsuarioId, cancellationToken)
                        >= _politica.ObtenerCondiciones(usuario.TipoUsuario).LimitePrestamos)
                        throw new BusinessRuleException("El usuario alcanzó su límite de préstamos activos.");
                    if ((await _solicitudRepository.ObtenerPorUsuarioAsync(
                            dto.UsuarioId,
                            cancellationToken))
                        .Any(s => s.LibroId == dto.LibroId && s.Estado == EstadoSolicitud.Pendiente))
                        throw new BusinessRuleException("Ya existe una solicitud pendiente para este libro.");

                    await base.AddAsync(dto);
                    await _notificacionService.EnviarNotificacionAsync(new SaveNotificacionDto
                    {
                        UsuarioId = dto.UsuarioId,
                        TipoEvento = "Informacion",
                        Mensaje = "Tu solicitud fue registrada y está en espera de evaluación."
                    }, cancellationToken);
                    await _auditoria.RegistrarAsync(
                        dto.UsuarioId,
                        ModuloAuditoria.Solicitudes,
                        AccionAuditoria.Registrar,
                        $"Solicitud registrada para el libro {dto.LibroId}.",
                        cancellationToken: cancellationToken);
                }, IsolationLevel.Serializable);

                return true;
            }
            catch (BusinessRuleException ex)
            {
                _logger.LogWarning("Intento de solicitud no válido: {Msg}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar solicitud para usuario {Uid}", dto.UsuarioId);
                throw;
            }
        }

        public async Task<bool> EvaluarSolicitudAsync(UpdateSolicitudPrestamoDto dto)
        {
            try
            {
                var solicitud = await _solicitudRepository.GetByIdAsync(dto.Id)
                    ?? throw new BusinessRuleException("La solicitud especificada no existe.");

                if (dto.Estado.Equals("Aprobada", StringComparison.OrdinalIgnoreCase))
                    solicitud.Aprobar();
                else if (dto.Estado.Equals("Rechazada", StringComparison.OrdinalIgnoreCase))
                    solicitud.Rechazar(dto.MotivoRechazo ?? string.Empty);
                else
                    throw new BusinessRuleException("El estado debe ser Aprobada o Rechazada.");

                await _unitOfWork.EjecutarEnTransaccionAsync(async cancellationToken =>
                {
                    await _solicitudRepository.ActualizarAsync(solicitud);
                    await _notificacionService.EnviarNotificacionAsync(new SaveNotificacionDto
                    {
                        UsuarioId = solicitud.UsuarioId,
                        TipoEvento = dto.Estado.Equals("Aprobada", StringComparison.OrdinalIgnoreCase)
                            ? "Informacion"
                            : "Alerta",
                        Mensaje = dto.Estado.Equals("Aprobada", StringComparison.OrdinalIgnoreCase)
                            ? $"Tu solicitud #{solicitud.Id} fue aprobada."
                            : $"Tu solicitud #{solicitud.Id} fue rechazada. Motivo: {dto.MotivoRechazo}"
                    }, cancellationToken);
                    await _auditoria.RegistrarAsync(
                        solicitud.UsuarioId,
                        ModuloAuditoria.Solicitudes,
                        dto.Estado.Equals("Aprobada", StringComparison.OrdinalIgnoreCase)
                            ? AccionAuditoria.Aprobar
                            : AccionAuditoria.Rechazar,
                        $"Solicitud {solicitud.Id} evaluada como {solicitud.Estado}.",
                        cancellationToken: cancellationToken);
                }, IsolationLevel.Serializable);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al evaluar solicitud {Id}", dto.Id);
                throw;
            }
        }

        public async Task CancelarAsync(
            int solicitudId,
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                var solicitud = await _solicitudRepository.ObtenerPorIdAsync(solicitudId, ct)
                    ?? throw new BusinessRuleException("La solicitud especificada no existe.");
                if (solicitud.UsuarioId != usuarioId)
                    throw new BusinessRuleException("La solicitud no pertenece al usuario autenticado.");

                solicitud.Cancelar();
                _solicitudRepository.Actualizar(solicitud);
                await _auditoria.RegistrarAsync(
                    usuarioId,
                    ModuloAuditoria.Solicitudes,
                    AccionAuditoria.Cancelar,
                    $"Solicitud {solicitudId} cancelada por el usuario.",
                    cancellationToken: ct);
            }, IsolationLevel.Serializable, cancellationToken);
        }
    }
}
