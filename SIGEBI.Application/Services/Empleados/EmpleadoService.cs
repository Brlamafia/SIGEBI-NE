using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Empleados;
using SIGEBI.Application.Interfaces.Empleados;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using System.Data;

namespace SIGEBI.Application.Services.Empleados
{
    public class EmpleadoService : BaseService<Empleado, EmpleadoDto>, IEmpleadoService
    {
        private readonly IRepository<Empleado> _empleadoRepository;
        private readonly IEmpleadoRepository _empleados;
        private readonly IUsuarioRepository _usuarios;
        private readonly ICargoRepository _cargos;
        private readonly IAuditoriaWriter _auditoria;
        private readonly IUsuarioActual _usuarioActual;
        private readonly IUnitOfWork _unitOfWork;

        public EmpleadoService(
            IRepository<Empleado> empleadoRepository,
            IEmpleadoRepository empleados,
            IUsuarioRepository usuarios,
            ICargoRepository cargos,
            IAuditoriaWriter auditoria,
            IUsuarioActual usuarioActual,
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : base(empleadoRepository, mapper)
        {
            _empleadoRepository = empleadoRepository;
            _empleados = empleados;
            _usuarios = usuarios;
            _cargos = cargos;
            _auditoria = auditoria;
            _usuarioActual = usuarioActual;
            _unitOfWork = unitOfWork;
        }

        public async Task<EmpleadoDto> CrearAsync(SaveEmpleadoDto dto, CancellationToken ct = default)
        {
            Empleado? empleado = null;
            await _unitOfWork.EjecutarEnTransaccionAsync(async cancellationToken =>
            {
                if (await _usuarios.ObtenerPorIdAsync(dto.UsuarioId, cancellationToken) is null)
                    throw new NotFoundException(nameof(Usuario), dto.UsuarioId);
                if (await _cargos.ObtenerPorIdAsync(dto.CargoId, cancellationToken) is null)
                    throw new NotFoundException(nameof(Cargo), dto.CargoId);
                if (await _empleados.ObtenerPorUsuarioIdAsync(dto.UsuarioId, cancellationToken) is not null)
                    throw new BusinessRuleException("El usuario ya posee un perfil de empleado.");
                empleado = new Empleado(dto.UsuarioId, dto.CargoId);
                await _empleadoRepository.AgregarAsync(empleado, cancellationToken);
                await AuditarAsync(
                    AccionAuditoria.Registrar,
                    $"Empleado creado para el usuario {dto.UsuarioId}.",
                    cancellationToken);
            }, IsolationLevel.Serializable, ct);
            return _mapper.Map<EmpleadoDto>(empleado);
        }

        public async Task<EmpleadoDto> ActualizarAsync(
            int id,
            UpdateEmpleadoDto dto,
            CancellationToken ct = default)
        {
            Empleado? empleado = null;
            await _unitOfWork.EjecutarEnTransaccionAsync(async cancellationToken =>
            {
                empleado = await _empleados.ObtenerPorIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException(nameof(Empleado), id);
                if (await _cargos.ObtenerPorIdAsync(dto.CargoId, cancellationToken) is null)
                    throw new NotFoundException(nameof(Cargo), dto.CargoId);
                empleado.ActualizarCargo(dto.CargoId);
                await _empleadoRepository.ActualizarAsync(empleado);
                await AuditarAsync(
                    AccionAuditoria.Editar,
                    $"Cargo del empleado {id} actualizado.",
                    cancellationToken);
            }, IsolationLevel.Serializable, ct);
            return _mapper.Map<EmpleadoDto>(empleado);
        }

        public override async Task DeleteAsync(int id)
        {
            await _unitOfWork.EjecutarEnTransaccionAsync(async cancellationToken =>
            {
                var empleado = await _empleados.ObtenerPorIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException(nameof(Empleado), id);
                if (await _empleados.TieneOperacionesAsync(id, cancellationToken))
                    throw new BusinessRuleException(
                        "El empleado no puede eliminarse porque posee operaciones históricas.");
                await _empleadoRepository.EliminarAsync(empleado);
                await AuditarAsync(
                    AccionAuditoria.Eliminar,
                    $"Perfil de empleado {id} eliminado.",
                    cancellationToken);
            }, IsolationLevel.Serializable);
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
                ModuloAuditoria.Administracion,
                accion,
                descripcion,
                cancellationToken: cancellationToken);
        }
    }
}
