using Microsoft.Extensions.Logging;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;

namespace SIGEBI.Persistence.Repositories.Usuarios;

public sealed class PermisoRepository : MutableRepository<Permiso>
{
    public PermisoRepository(SigebiContext context, ILogger<PermisoRepository> logger)
        : base(context, logger) { }
}
