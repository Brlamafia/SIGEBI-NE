using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Prestamos; // Necesario para mapear el historial
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Dtos.Usuarios;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Usuarios;
using SIGEBI.Domain.Entities;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIGEBI.Application.Services.Usuarios
{
    public class UsuarioService : BaseService<Usuario, UsuarioDto>, IUsuarioService
    {
        private readonly IRepository<Usuario> _usuarioRepository;
        private readonly IRepository<SolicitudPrestamo> _solicitudesRepository;

        // Inyectamos el repositorio de SolicitudPrestamo para hacer el JOIN
        public UsuarioService(
            IRepository<Usuario> repository,
            IRepository<SolicitudPrestamo> solicitudesRepository,
            IMapper mapper)
            : base(repository, mapper)
        {
            _usuarioRepository = repository;
            _solicitudesRepository = solicitudesRepository;
        }

        public override async Task<UsuarioDto> AddAsync<TSaveDto>(TSaveDto dto)
        {
            var entity = _mapper.Map<Usuario>(dto);

            // Regla de Negocio 1: Validar que no se duplique la cédula o el correo al crear
            var usuariosExistentes = await _usuarioRepository.GetAllAsync();
            if (usuariosExistentes.Any(u => u.Email == entity.Email || u.Cedula == entity.Cedula))
            {
                throw new BusinessRuleException("Ya existe un usuario registrado con esta cédula o correo electrónico.");
            }

            return await base.AddAsync(dto);
        }

        public override async Task UpdateAsync<TUpdateDto>(int id, TUpdateDto dto)
        {
            if (dto is not UpdateUsuarioDto datos)
                throw new ArgumentException("El contrato de actualización de usuario no es válido.", nameof(dto));

            // Regla de Negocio 2: Validar duplicados ignorando al usuario que estamos editando
            var usuariosExistentes = await _usuarioRepository.GetAllAsync();

            if (usuariosExistentes.Any(u =>
                string.Equals(u.Email, datos.Email, StringComparison.OrdinalIgnoreCase)
                && u.Id != id))
            {
                throw new BusinessRuleException("El correo electrónico ya está en uso por otro usuario.");
            }

            await base.UpdateAsync(id, dto);
        }

        // Regla de Negocio 3: Historial completo consolidado
        public async Task<object> ConsultarHistorialCompletoAsync(int usuarioId)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
            if (usuario == null) throw new BusinessRuleException("Usuario no encontrado.");

            // Hacemos el cruce manual con las solicitudes
            var todasLasSolicitudes = await _solicitudesRepository.GetAllAsync();
            var misPrestamos = todasLasSolicitudes.Where(s => s.UsuarioId == usuarioId).ToList();

            // Devolvemos un objeto anónimo estructurado para evitar crear DTOs de madrugada
            return new
            {
                Usuario = _mapper.Map<UsuarioDto>(usuario),
                TotalPrestamosActivos = misPrestamos.Count(s => s.Estado.ToString() == "Aprobada"),
                TotalSolicitudes = misPrestamos.Count,
                Historial = _mapper.Map<IEnumerable<SolicitudPrestamoDto>>(misPrestamos)
            };
        }
    }
}
