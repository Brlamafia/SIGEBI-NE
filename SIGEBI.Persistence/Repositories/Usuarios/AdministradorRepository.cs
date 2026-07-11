using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;

namespace SIGEBI.Persistence.Repositories.Usuarios
{
    // Patrón Repository y SRP: encapsula el acceso a datos administrativos.
    public class AdministradorRepository : MutableRepository<Administrador>, IAdministradorRepository
    {
        public AdministradorRepository(SigebiContext context) : base(context) { }

        public async Task<Administrador?> ObtenerPorIdAsync(
            int id,
            CancellationToken cancellationToken = default)
            => await _dbSet
                .Include(a => a.Usuario)
                .Include(a => a.Cargo)
                .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

        public Task<Administrador?> ObtenerPorUsuarioIdAsync(
            int usuarioId,
            CancellationToken cancellationToken = default)
            => _dbSet
                .Include(a => a.Usuario)
                .Include(a => a.Cargo)
                .SingleOrDefaultAsync(a => a.UsuarioId == usuarioId, cancellationToken);

        public async Task<IReadOnlyCollection<Administrador>> ObtenerTodosAsync(
            CancellationToken cancellationToken = default)
            => await _dbSet
                .Include(a => a.Usuario)
                .Include(a => a.Cargo)
                .OrderBy(a => a.Id)
                .ToListAsync(cancellationToken);
    }
}
