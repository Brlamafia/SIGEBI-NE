using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Empleados;
using SIGEBI.Application.Interfaces.Empleados;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Application.Services.Empleados
{
    public class EmpleadoService : BaseService<Empleado, EmpleadoDto>, IEmpleadoService
    {
        private readonly IRepository<Empleado> _empleadoRepository;

        public EmpleadoService(IRepository<Empleado> empleadoRepository, IMapper mapper)
            : base(empleadoRepository, mapper)
        {
            _empleadoRepository = empleadoRepository;
        }
    }
}