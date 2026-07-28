using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Empleados;
using SIGEBI.Application.Interfaces.Empleados;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Application.Exceptions;

namespace SIGEBI.Application.Services.Empleados
{
    public class EmpleadoService : BaseService<Empleado, EmpleadoDto>, IEmpleadoService
    {
        private readonly IRepository<Empleado> _empleadoRepository;
        private readonly IEmpleadoRepository _empleados;
        private readonly IUsuarioRepository _usuarios;
        private readonly ICargoRepository _cargos;

        public EmpleadoService(
            IRepository<Empleado> empleadoRepository,
            IEmpleadoRepository empleados,
            IUsuarioRepository usuarios,
            ICargoRepository cargos,
            IMapper mapper)
            : base(empleadoRepository, mapper)
        {
            _empleadoRepository = empleadoRepository;
            _empleados = empleados;
            _usuarios = usuarios;
            _cargos = cargos;
        }

        public async Task<EmpleadoDto> CrearAsync(SaveEmpleadoDto dto, CancellationToken ct = default)
        {
            if (await _usuarios.ObtenerPorIdAsync(dto.UsuarioId, ct) is null)
                throw new NotFoundException(nameof(Usuario), dto.UsuarioId);
            if (await _cargos.ObtenerPorIdAsync(dto.CargoId, ct) is null)
                throw new NotFoundException(nameof(Cargo), dto.CargoId);
            if (await _empleados.ObtenerPorUsuarioIdAsync(dto.UsuarioId, ct) is not null)
                throw new BusinessRuleException("El usuario ya posee un perfil de empleado.");
            var empleado = new Empleado(dto.UsuarioId, dto.CargoId);
            await _empleadoRepository.AgregarAsync(empleado, ct);
            return _mapper.Map<EmpleadoDto>(empleado);
        }

        public async Task<EmpleadoDto> ActualizarAsync(
            int id,
            UpdateEmpleadoDto dto,
            CancellationToken ct = default)
        {
            var empleado = await _empleados.ObtenerPorIdAsync(id, ct)
                ?? throw new NotFoundException(nameof(Empleado), id);
            if (await _cargos.ObtenerPorIdAsync(dto.CargoId, ct) is null)
                throw new NotFoundException(nameof(Cargo), dto.CargoId);
            empleado.ActualizarCargo(dto.CargoId);
            await _empleadoRepository.ActualizarAsync(empleado);
            return _mapper.Map<EmpleadoDto>(empleado);
        }
    }
}
