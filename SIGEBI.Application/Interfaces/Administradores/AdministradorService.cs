using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Administradores;
using SIGEBI.Application.Interfaces.Administradores;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Application.Services.Administradores
{
    public class AdministradorService : BaseService<Administrador, AdministradorDto>, IAdministradorService
    {
        private readonly IRepository<Administrador> _administradorRepository;

        public AdministradorService(IRepository<Administrador> administradorRepository, IMapper mapper)
            : base(administradorRepository, mapper)
        {
            _administradorRepository = administradorRepository;
        }
    }
}