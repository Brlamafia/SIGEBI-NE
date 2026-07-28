using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Empleados;

namespace SIGEBI.Application.Interfaces.Empleados
{
    public interface IEmpleadoService : IBaseService<EmpleadoDto>
    {
        Task<EmpleadoDto> CrearAsync(SaveEmpleadoDto dto, CancellationToken ct = default);
        Task<EmpleadoDto> ActualizarAsync(int id, UpdateEmpleadoDto dto, CancellationToken ct = default);
    }
}
