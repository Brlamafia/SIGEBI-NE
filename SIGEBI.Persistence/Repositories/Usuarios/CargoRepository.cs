using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;

namespace SIGEBI.Persistence.Repositories.Usuarios
{
    // Patrón Repository y SRP: concentra exclusivamente la persistencia de cargos.
    public class CargoRepository : MutableRepository<Cargo>, ICargoRepository
    {
        public CargoRepository(SigebiContext context) : base(context) { }

        public async Task<Cargo?> ObtenerPorIdAsync(
            int id,
            CancellationToken cancellationToken = default)
            => await _dbSet.FindAsync([id], cancellationToken);

        public Task<Cargo?> ObtenerPorNombreAsync(
            string nombre,
            CancellationToken cancellationToken = default)
            => _dbSet.SingleOrDefaultAsync(c => c.Nombre == nombre, cancellationToken);

        public async Task<IReadOnlyCollection<Cargo>> ObtenerTodosAsync(
            CancellationToken cancellationToken = default)
            => await _dbSet
                .OrderBy(c => c.Nombre)
                .ToListAsync(cancellationToken);
    }
}
