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

        public override async Task<Rol?> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation(
                    "Consultando rol {RolId} con sus permisos",
                    id);
                var rol = await _dbSet
                    .AsNoTracking()
                    .Include(item => item.Permisos)
                    .FirstOrDefaultAsync(item => item.Id == id, ct);
                _logger.LogInformation(
                    "Consulta del rol {RolId} completada. Encontrado: {Encontrado}",
                    id,
                    rol is not null);
                return rol;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Error al consultar el rol {RolId} con sus permisos",
                    id);
                throw;
            }
        }
    }
}
