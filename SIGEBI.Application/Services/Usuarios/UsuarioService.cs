using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Usuarios;
using SIGEBI.Application.Interfaces.Usuarios;
using SIGEBI.Domain.Entities;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Application.Services.Usuarios
{
    public class UsuarioService : BaseService<Usuario, UsuarioDto>, IUsuarioService
    {
        public UsuarioService(IRepository<Usuario> repository, IMapper mapper)
            : base(repository, mapper)
        {
        }
    }
}