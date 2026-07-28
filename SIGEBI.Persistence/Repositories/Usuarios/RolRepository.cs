using Microsoft.Extensions.Logging;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace SIGEBI.Persistence.Repositories.Usuarios
{
    public sealed class RolRepository : MutableRepository<Rol>, IRepository<Rol>
    {
        public RolRepository(SigebiContext context, ILogger<RolRepository> logger) : base(context, logger) { }

        public override Task<Rol?> GetByIdAsync(int id) =>
            _dbSet.Include(r => r.Permisos).FirstOrDefaultAsync(r => r.Id == id);
    }
}
