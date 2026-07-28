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
using SIGEBI.Application.Security;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.Notificaciones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIGEBI.Application.Services.Usuarios
{
    public class UsuarioService : BaseService<Usuario, UsuarioDto>, IUsuarioService
    {
        private readonly IRepository<Usuario> _usuarioRepository;
        private readonly IUsuarioRepository _usuarios;
        private readonly IRepository<SolicitudPrestamo> _solicitudesRepository;
        private readonly ILogger<UsuarioService> _logger; // B.R: Logger
        private readonly IPrestamoService _prestamos;
        private readonly IMultaService _multas;
        private readonly INotificacionService _notificaciones;

        public UsuarioService(
            IRepository<Usuario> repository,
            IUsuarioRepository usuarios,
            IRepository<SolicitudPrestamo> solicitudesRepository,
            IPrestamoService prestamos,
            IMultaService multas,
            INotificacionService notificaciones,
            IMapper mapper,
            ILogger<UsuarioService> logger) // B.R: Inyección
            : base(repository, mapper)
        {
            _usuarioRepository = repository;
            _usuarios = usuarios;
            _solicitudesRepository = solicitudesRepository;
            _prestamos = prestamos;
            _multas = multas;
            _notificaciones = notificaciones;
            _logger = logger;
        }

        public override async Task<UsuarioDto> AddAsync<TSaveDto>(TSaveDto dto)
        {
            if (dto is not SaveUsuarioDto datos)
                throw new ArgumentException("El contrato de creación de usuario no es válido.", nameof(dto));
            return await CrearAsync(datos);
        }

        public async Task<UsuarioDto> CrearAsync(
            SaveUsuarioDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            await ValidarUnicidadAsync(dto.Email, dto.Cedula, null, cancellationToken);
            var entity = _mapper.Map<Usuario>(dto);
            if (!string.IsNullOrWhiteSpace(dto.Password))
                entity.EstablecerContrasenaHash(PasswordHasher.Hash(dto.Password));
            else
                throw new BusinessRuleException("Debe asignar una contraseña inicial al usuario.");

            await _usuarios.AgregarAsync(entity, cancellationToken);
            _logger.LogInformation("Usuario {UsuarioId} creado correctamente.", entity.Id);
            return _mapper.Map<UsuarioDto>(entity);
        }

        public override async Task UpdateAsync<TUpdateDto>(int id, TUpdateDto dto)
        {
            if (dto is not UpdateUsuarioDto datos)
                throw new ArgumentException("El contrato de actualización de usuario no es válido.", nameof(dto));

            await ActualizarAsync(id, datos);
        }

        public async Task<UsuarioDto> ActualizarAsync(
            int usuarioId,
            UpdateUsuarioDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            var usuario = await _usuarios.ObtenerPorIdAsync(usuarioId, cancellationToken)
                ?? throw new NotFoundException(nameof(Usuario), usuarioId);
            await ValidarUnicidadAsync(
                dto.Email,
                dto.Cedula,
                usuarioId,
                cancellationToken);

            usuario.ActualizarDatos(
                dto.Nombre,
                dto.Apellido,
                dto.Cedula,
                dto.Telefono,
                dto.Email,
                dto.TipoUsuario,
                dto.Estado);
            await _usuarioRepository.ActualizarAsync(usuario);
            _logger.LogInformation("Usuario {UsuarioId} actualizado correctamente.", usuarioId);
            return _mapper.Map<UsuarioDto>(usuario);
        }

        public async Task EliminarAsync(
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarios.ObtenerPorIdAsync(usuarioId, cancellationToken)
                ?? throw new NotFoundException(nameof(Usuario), usuarioId);
            if (await _usuarios.TieneRelacionesAsync(usuarioId, cancellationToken))
                throw new BusinessRuleException(
                    "El usuario no puede eliminarse porque posee préstamos, solicitudes, multas, notificaciones, auditorías o perfiles asociados. Puede cambiar su estado a Inactivo.");

            await _usuarioRepository.EliminarAsync(usuario);
            _logger.LogInformation("Usuario {UsuarioId} eliminado correctamente.", usuarioId);
        }

        private async Task ValidarUnicidadAsync(
            string email,
            string cedula,
            int? usuarioActualId,
            CancellationToken cancellationToken)
        {
            var porEmail = await _usuarios.ObtenerPorEmailAsync(email.Trim(), cancellationToken);
            if (porEmail is not null && porEmail.Id != usuarioActualId)
            {
                _logger.LogWarning("Correo duplicado al guardar usuario: {Email}", email);
                throw new BusinessRuleException("El correo electrónico ya está registrado.");
            }

            var porCedula = await _usuarios.ObtenerPorCedulaAsync(cedula.Trim(), cancellationToken);
            if (porCedula is not null && porCedula.Id != usuarioActualId)
            {
                _logger.LogWarning("Cédula duplicada al guardar usuario: {Cedula}", cedula);
                throw new BusinessRuleException("La cédula ya está registrada.");
            }
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
                var solicitudes = todasLasSolicitudes.Where(s => s.UsuarioId == usuarioId).ToList();
                var prestamos = await _prestamos.ObtenerPorUsuarioAsync(usuarioId);
                var multas = await _multas.ObtenerPorUsuarioAsync(usuarioId);
                var notificaciones = await _notificaciones.ObtenerPorUsuarioAsync(usuarioId);

                return new
                {
                    Usuario = _mapper.Map<UsuarioDto>(usuario),
                    TotalPrestamosActivos = prestamos.Count(p =>
                        p.Estado is "Activo" or "Vencido"),
                    TotalSolicitudes = solicitudes.Count,
                    Solicitudes = _mapper.Map<IEnumerable<SolicitudPrestamoDto>>(solicitudes),
                    Prestamos = prestamos,
                    Multas = multas,
                    Notificaciones = notificaciones
                };
            }
            catch (Exception ex) when (ex is not BusinessRuleException)
            {
                _logger.LogError(ex, "Error crítico consultando historial del usuario {Id}", usuarioId);
                throw;
            }
        }

        public async Task CambiarPasswordAsync(
            int usuarioId,
            string passwordActual,
            string passwordNueva,
            CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarios.ObtenerPorIdAsync(usuarioId, cancellationToken)
                ?? throw new NotFoundException(nameof(Usuario), usuarioId);
            if (!PasswordHasher.Verify(passwordActual, usuario.ContrasenaHash))
                throw new BusinessRuleException("La contraseña actual no es correcta.");
            usuario.EstablecerContrasenaHash(PasswordHasher.Hash(passwordNueva));
            await _usuarioRepository.ActualizarAsync(usuario);
        }
    }
}
