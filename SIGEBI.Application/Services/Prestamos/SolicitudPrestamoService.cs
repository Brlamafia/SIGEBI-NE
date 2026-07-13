using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Notificaciones;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.SolicitudesPrestamo;
using SIGEBI.Domain.Entities;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Entities.Usuarios;
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

        public SolicitudPrestamoService(
            IRepository<SolicitudPrestamo> repository,
            IRepository<Libro> libroRepository,
            IRepository<Usuario> usuarioRepository,
            INotificacionService notificacionService,
            IMapper mapper) : base(repository, mapper)
        {
            _solicitudRepository = repository;
            _libroRepository = libroRepository;
            _usuarioRepository = usuarioRepository;
            _notificacionService = notificacionService;
        }

        // Resolviendo CS0535: Métodos faltantes exigidos por tu interfaz ISolicitudPrestamoService
        public async Task<IEnumerable<SolicitudPrestamoDto>> ObtenerPorUsuarioAsync(int usuarioId)
        {
            var todas = await _solicitudRepository.GetAllAsync();
            var filtradas = todas.Where(s => s.UsuarioId == usuarioId).ToList();
            return _mapper.Map<IEnumerable<SolicitudPrestamoDto>>(filtradas);
        }

        public async Task<IEnumerable<SolicitudPrestamoDto>> ObtenerPorEstadoAsync(string estado)
        {
            var todas = await _solicitudRepository.GetAllAsync();
            // Convertimos el Enum a string de forma segura para comparar
            var filtradas = todas.Where(s => s.Estado.ToString().Equals(estado, StringComparison.OrdinalIgnoreCase)).ToList();
            return _mapper.Map<IEnumerable<SolicitudPrestamoDto>>(filtradas);
        }

        public async Task<bool> RegistrarSolicitudAsync(SaveSolicitudPrestamoDto dto)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(dto.UsuarioId);
            if (usuario == null) throw new BusinessRuleException("El usuario especificado no existe.");

            // Resolviendo CS0019: Comparación segura extrayendo el string del Enum 'EstadoUsuario'
            if (usuario.Estado.ToString() != "Activo")
                throw new BusinessRuleException("El usuario no está activo y no puede solicitar préstamos.");

            var libro = await _libroRepository.GetByIdAsync(dto.LibroId);
            if (libro == null) throw new BusinessRuleException("El libro solicitado no existe.");

            // Resolviendo CS1061: Utilizamos el motor base para añadir, evadiendo problemas de extensión
            var result = await base.AddAsync(dto);

            // Resolviendo CS0117: Adaptado a las propiedades reales de tu DTO
            await _notificacionService.EnviarNotificacionAsync(new SaveNotificacionDto
            {
                UsuarioId = dto.UsuarioId,
                Mensaje = $"Tu solicitud para el libro ha sido registrada y está en espera de evaluación."
            });

            return true;
        }

        // Resolviendo CS0535: Adaptado a la firma exacta que pide tu interfaz
        public async Task<bool> EvaluarSolicitudAsync(UpdateSolicitudPrestamoDto dto)
        {
            // Resolviendo CS0272: Al usar el BaseService, AutoMapper se encarga de inyectar 
            // las actualizaciones, respetando el encapsulamiento de tu Entidad de Dominio.
            // Asegúrate de que UpdateSolicitudPrestamoDto contenga la propiedad 'Id'.
            await base.UpdateAsync(dto.Id, dto);

            return true;
        }
    }
}