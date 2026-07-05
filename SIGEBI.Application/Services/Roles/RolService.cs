using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Roles;
using SIGEBI.Application.Interfaces.Roles;
using SIGEBI.Domain.Entities.Usuarios; // Ajusta si tu entidad Rol está en otro namespace
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Application.Services.Roles
{
    public class RolService : BaseService<Rol, RolDto>, IRolService
    {
        private readonly IRepository<Rol> _rolRepository; // O IRolRepository si tienes uno específico

        // Inyectamos el repositorio y el mapper para pasarlos a la clase base
        public RolService(IRepository<Rol> rolRepository, IMapper mapper)
            : base(rolRepository, mapper)
        {
            _rolRepository = rolRepository;
        }
    }
}