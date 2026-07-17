using AutoMapper;
using Microsoft.Extensions.Logging; // B.R: Importante
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Dtos.Usuarios;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Usuarios;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIGEBI.Application.Services.Usuarios
{
    public class UsuarioService : BaseService<Usuario, UsuarioDto>, IUsuarioService
    {
        private readonly IRepository<Usuario> _usuarioRepository;
        private readonly IRepository<SolicitudPrestamo> _solicitudesRepository;
        private readonly ILogger<UsuarioService> _logger; // B.R: Logger

        public UsuarioService(
            IRepository<Usuario> repository,
            IRepository<SolicitudPrestamo> solicitudesRepository,
            IMapper mapper,
            ILogger<UsuarioService> logger) // B.R: Inyección
            : base(repository, mapper)
        {
            _usuarioRepository = repository;
            _solicitudesRepository = solicitudesRepository;
            _logger = logger;
        }

        public override async Task<UsuarioDto> AddAsync<TSaveDto>(TSaveDto dto)
        {
            var entity = _mapper.Map<Usuario>(dto);

            var usuariosExistentes = await _usuarioRepository.GetAllAsync();
            if (usuariosExistentes.Any(u => u.Email == entity.Email || u.Cedula == entity.Cedula))
            {
                // B.R: Logueamos la advertencia de negocio
                _logger.LogWarning("Intento de registro duplicado para Email: {Email} o Cedula: {Cedula}", entity.Email, entity.Cedula);
                throw new BusinessRuleException("Ya existe un usuario registrado con esta cédula o correo electrónico.");
            }

            return await base.AddAsync(dto);
        }

        public override async Task UpdateAsync<TUpdateDto>(int id, TUpdateDto dto)
        {
            if (dto is not UpdateUsuarioDto datos)
                throw new ArgumentException("El contrato de actualización de usuario no es válido.", nameof(dto));

            var usuariosExistentes = await _usuarioRepository.GetAllAsync();

            if (usuariosExistentes.Any(u => string.Equals(u.Email, datos.Email, StringComparison.OrdinalIgnoreCase) && u.Id != id))
            {
                _logger.LogWarning("Intento de actualizar correo a uno duplicado. Usuario ID: {Id}", id);
                throw new BusinessRuleException("El correo electrónico ya está en uso por otro usuario.");
            }

            await base.UpdateAsync(id, dto);
        }

        public async Task<object> ConsultarHistorialCompletoAsync(int usuarioId)
        {
            try
            {
                var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
                if (usuario == null)
                {
                    _logger.LogWarning("Consulta de historial fallida: Usuario ID {Id} no encontrado.", usuarioId);
                    throw new BusinessRuleException("Usuario no encontrado.");
                }

                var todasLasSolicitudes = await _solicitudesRepository.GetAllAsync();
                var misPrestamos = todasLasSolicitudes.Where(s => s.UsuarioId == usuarioId).ToList();

                return new
                {
                    Usuario = _mapper.Map<UsuarioDto>(usuario),
                    TotalPrestamosActivos = misPrestamos.Count(s => s.Estado.ToString() == "Aprobada"),
                    TotalSolicitudes = misPrestamos.Count,
                    Historial = _mapper.Map<IEnumerable<SolicitudPrestamoDto>>(misPrestamos)
                };
            }
            catch (Exception ex) when (ex is not BusinessRuleException)
            {
                _logger.LogError(ex, "Error crítico consultando historial del usuario {Id}", usuarioId);
                throw;
            }
        }
    }
}