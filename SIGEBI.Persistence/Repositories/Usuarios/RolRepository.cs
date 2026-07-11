using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;

namespace SIGEBI.Persistence.Repositories.Usuarios;

public sealed class RolRepository : MutableRepository<Rol>, IRepository<Rol>
{
    public RolRepository(SigebiContext context) : base(context)
    {
    }
}
