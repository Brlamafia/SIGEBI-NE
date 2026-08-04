using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Roles;
using SIGEBI.Application.Interfaces.Roles;
using SIGEBI.Domain.Entities.Usuarios; // Ajusta si tu entidad Rol está en otro namespace
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace SIGEBI.Application.Services.Roles
{
    public class RolService : BaseService<Rol, RolDto>, IRolService
    {
        private readonly IRepository<Rol> _rolRepository; // O IRolRepository si tienes uno específico
        private readonly IRepository<Usuario> _usuarioRepository;
        private readonly IUsuarioRepository _usuarios;
        private readonly IRepository<Permiso> _permisos;
        private readonly IAuditoriaWriter _auditoria;
        private readonly IUsuarioActual _usuarioActual;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "administracion:roles";

        // Inyectamos el repositorio y el mapper para pasarlos a la clase base
        public RolService(
            IRepository<Rol> rolRepository,
            IRepository<Usuario> usuarioRepository,
            IUsuarioRepository usuarios,
            IRepository<Permiso> permisos,
            IAuditoriaWriter auditoria,
            IUsuarioActual usuarioActual,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMemoryCache cache)
            : base(rolRepository, mapper)
        {
            _rolRepository = rolRepository;
            _usuarioRepository = usuarioRepository;
            _usuarios = usuarios;
            _permisos = permisos;
            _auditoria = auditoria;
            _usuarioActual = usuarioActual;
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        public override async Task<IEnumerable<RolDto>> GetAllAsync()
        {
            var roles = await _cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return (await base.GetAllAsync()).ToArray();
            });
            return roles ?? Array.Empty<RolDto>();
        }

        public override async Task<RolDto> AddAsync<TSaveDto>(TSaveDto dto)
        {
            var creado = await base.AddAsync(dto);
            _cache.Remove(CacheKey);
            await AuditarAsync(AccionAuditoria.Registrar, $"Rol {creado.Nombre} creado.");
            return creado;
        }

        public override async Task UpdateAsync<TUpdateDto>(int id, TUpdateDto dto)
        {
            await base.UpdateAsync(id, dto);
            _cache.Remove(CacheKey);
            await AuditarAsync(AccionAuditoria.Editar, $"Rol {id} actualizado.");
        }

        public override async Task DeleteAsync(int id)
        {
            await base.DeleteAsync(id);
            _cache.Remove(CacheKey);
            await AuditarAsync(AccionAuditoria.Eliminar, $"Rol {id} eliminado.");
        }

        public async Task AsignarAUsuarioAsync(AsignarRolDto dto, CancellationToken ct = default)
        {
            var usuario = await _usuarios.ObtenerPorIdConRolesAsync(dto.UsuarioId, ct)
                ?? throw new NotFoundException(nameof(Usuario), dto.UsuarioId);
            var rol = await _rolRepository.GetByIdAsync(dto.RolId)
                ?? throw new NotFoundException(nameof(Rol), dto.RolId);
            usuario.AsignarRol(rol);
            await _usuarioRepository.ActualizarAsync(usuario);
            await AuditarAsync(AccionAuditoria.Editar, $"Rol {dto.RolId} asignado al usuario {dto.UsuarioId}.", ct);
        }

        public async Task RemoverDeUsuarioAsync(AsignarRolDto dto, CancellationToken ct = default)
        {
            var usuario = await _usuarios.ObtenerPorIdConRolesAsync(dto.UsuarioId, ct)
                ?? throw new NotFoundException(nameof(Usuario), dto.UsuarioId);
            var rol = usuario.Roles.FirstOrDefault(r => r.Id == dto.RolId)
                ?? throw new BusinessRuleException("El usuario no posee ese rol.");
            usuario.RemoverRol(rol);
            await _usuarioRepository.ActualizarAsync(usuario);
            await AuditarAsync(AccionAuditoria.Editar, $"Rol {dto.RolId} removido del usuario {dto.UsuarioId}.", ct);
        }

        public async Task<PermisoDto> CrearPermisoAsync(
            SavePermisoDto dto,
            CancellationToken ct = default)
        {
            var permiso = new Permiso(dto.Nombre, dto.Codigo);
            await _permisos.AgregarAsync(permiso, ct);
            await AuditarAsync(AccionAuditoria.Registrar, $"Permiso {permiso.Codigo} creado.", ct);
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
            _cache.Remove(CacheKey);
            await AuditarAsync(AccionAuditoria.Editar, $"Permiso {permiso.Codigo} asignado al rol {rol.Id}.", ct);
        }

        public async Task RemoverPermisoAsync(AsignarPermisoDto dto, CancellationToken ct = default)
        {
            var rol = await _rolRepository.GetByIdAsync(dto.RolId)
                ?? throw new NotFoundException(nameof(Rol), dto.RolId);
            var permiso = rol.Permisos.FirstOrDefault(p => p.Id == dto.PermisoId)
                ?? throw new BusinessRuleException("El rol no posee ese permiso.");
            rol.RemoverPermiso(permiso);
            await _rolRepository.ActualizarAsync(rol);
            _cache.Remove(CacheKey);
            await AuditarAsync(AccionAuditoria.Editar, $"Permiso {permiso.Codigo} removido del rol {rol.Id}.", ct);
        }

        private async Task AuditarAsync(
            AccionAuditoria accion,
            string descripcion,
            CancellationToken cancellationToken = default)
        {
            if (!_usuarioActual.EstaAutenticado)
                throw new BusinessRuleException("No se pudo determinar el usuario responsable.");
            await _auditoria.RegistrarAsync(
                _usuarioActual.UsuarioId,
                ModuloAuditoria.Administracion,
                accion,
                descripcion,
                cancellationToken: cancellationToken);
            await _unitOfWork.GuardarCambiosAsync(cancellationToken);
        }
    }
}
