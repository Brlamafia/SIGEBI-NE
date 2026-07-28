using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Roles;
using SIGEBI.Application.Interfaces.Roles;
using SIGEBI.Domain.Entities.Usuarios; // Ajusta si tu entidad Rol está en otro namespace
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Application.Exceptions;

namespace SIGEBI.Application.Services.Roles
{
    public class RolService : BaseService<Rol, RolDto>, IRolService
    {
        private readonly IRepository<Rol> _rolRepository; // O IRolRepository si tienes uno específico
        private readonly IRepository<Usuario> _usuarioRepository;
        private readonly IUsuarioRepository _usuarios;
        private readonly IRepository<Permiso> _permisos;

        // Inyectamos el repositorio y el mapper para pasarlos a la clase base
        public RolService(
            IRepository<Rol> rolRepository,
            IRepository<Usuario> usuarioRepository,
            IUsuarioRepository usuarios,
            IRepository<Permiso> permisos,
            IMapper mapper)
            : base(rolRepository, mapper)
        {
            _rolRepository = rolRepository;
            _usuarioRepository = usuarioRepository;
            _usuarios = usuarios;
            _permisos = permisos;
        }

        public async Task AsignarAUsuarioAsync(AsignarRolDto dto, CancellationToken ct = default)
        {
            var usuario = await _usuarios.ObtenerPorIdConRolesAsync(dto.UsuarioId, ct)
                ?? throw new NotFoundException(nameof(Usuario), dto.UsuarioId);
            var rol = await _rolRepository.GetByIdAsync(dto.RolId)
                ?? throw new NotFoundException(nameof(Rol), dto.RolId);
            usuario.AsignarRol(rol);
            await _usuarioRepository.ActualizarAsync(usuario);
        }

        public async Task RemoverDeUsuarioAsync(AsignarRolDto dto, CancellationToken ct = default)
        {
            var usuario = await _usuarios.ObtenerPorIdConRolesAsync(dto.UsuarioId, ct)
                ?? throw new NotFoundException(nameof(Usuario), dto.UsuarioId);
            var rol = usuario.Roles.FirstOrDefault(r => r.Id == dto.RolId)
                ?? throw new BusinessRuleException("El usuario no posee ese rol.");
            usuario.RemoverRol(rol);
            await _usuarioRepository.ActualizarAsync(usuario);
        }

        public async Task<PermisoDto> CrearPermisoAsync(
            SavePermisoDto dto,
            CancellationToken ct = default)
        {
            var permiso = new Permiso(dto.Nombre, dto.Codigo);
            await _permisos.AgregarAsync(permiso, ct);
            return _mapper.Map<PermisoDto>(permiso);
        }

        public async Task AsignarPermisoAsync(AsignarPermisoDto dto, CancellationToken ct = default)
        {
            var rol = await _rolRepository.GetByIdAsync(dto.RolId)
                ?? throw new NotFoundException(nameof(Rol), dto.RolId);
            var permiso = await _permisos.GetByIdAsync(dto.PermisoId)
                ?? throw new NotFoundException(nameof(Permiso), dto.PermisoId);
            rol.AsignarPermiso(permiso);
            await _rolRepository.ActualizarAsync(rol);
        }

        public async Task RemoverPermisoAsync(AsignarPermisoDto dto, CancellationToken ct = default)
        {
            var rol = await _rolRepository.GetByIdAsync(dto.RolId)
                ?? throw new NotFoundException(nameof(Rol), dto.RolId);
            var permiso = rol.Permisos.FirstOrDefault(p => p.Id == dto.PermisoId)
                ?? throw new BusinessRuleException("El rol no posee ese permiso.");
            rol.RemoverPermiso(permiso);
            await _rolRepository.ActualizarAsync(rol);
        }
    }
}
