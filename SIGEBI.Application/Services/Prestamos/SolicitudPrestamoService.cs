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
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Exceptions;
using SIGEBI.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIGEBI.Application.Services.Prestamos
{
    public class SolicitudPrestamoService : BaseService<SolicitudPrestamo, SolicitudPrestamoDto>, ISolicitudPrestamoService
    {
        private readonly IRepository<SolicitudPrestamo> _solicitudRepository;
        private readonly IRepository<Libro> _libroRepository;
        private readonly IRepository<Usuario> _usuarioRepository;
        private readonly INotificacionService _notificacionService;
        private readonly ILogger<SolicitudPrestamoService> _logger;

        public SolicitudPrestamoService(
            IRepository<SolicitudPrestamo> repository,
            IRepository<Libro> libroRepository,
            IRepository<Usuario> usuarioRepository,
            INotificacionService notificacionService,
            IMapper mapper,
            ILogger<SolicitudPrestamoService> logger) : base(repository, mapper)
        {
            _solicitudRepository = repository;
            _libroRepository = libroRepository;
            _usuarioRepository = usuarioRepository;
            _notificacionService = notificacionService;
            _logger = logger;
        }

        public async Task<IEnumerable<SolicitudPrestamoDto>> ObtenerPorUsuarioAsync(int usuarioId)
        {
            var todas = await _solicitudRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<SolicitudPrestamoDto>>(todas.Where(s => s.UsuarioId == usuarioId));
        }

        public async Task<IEnumerable<SolicitudPrestamoDto>> ObtenerPorEstadoAsync(string estado)
        {
            var todas = await _solicitudRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<SolicitudPrestamoDto>>(todas.Where(s => s.Estado.ToString().Equals(estado, StringComparison.OrdinalIgnoreCase)));
        }

        public async Task<bool> RegistrarSolicitudAsync(SaveSolicitudPrestamoDto dto)
        {
            try
            {
                var usuario = await _usuarioRepository.GetByIdAsync(dto.UsuarioId);
                if (usuario == null) throw new BusinessRuleException("El usuario especificado no existe.");
                if (usuario.Estado.ToString() != "Activo")
                    throw new BusinessRuleException("El usuario no está activo y no puede solicitar préstamos.");

                var libro = await _libroRepository.GetByIdAsync(dto.LibroId);
                if (libro == null) throw new BusinessRuleException("El libro solicitado no existe.");

                await base.AddAsync(dto);

                await _notificacionService.EnviarNotificacionAsync(new SaveNotificacionDto
                {
                    UsuarioId = dto.UsuarioId,
                    Mensaje = $"Tu solicitud para el libro ha sido registrada y está en espera de evaluación."
                });

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

                await _solicitudRepository.ActualizarAsync(solicitud);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al evaluar solicitud {Id}", dto.Id);
                throw;
            }
        }
    }
}