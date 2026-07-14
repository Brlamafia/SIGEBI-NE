using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;

namespace SIGEBI.Persistence.Repositories.Catalogo
{
    // Patrón Repository: separa el acceso al inventario de las reglas del dominio.
    public class InventarioRepository : MutableRepository<Inventario>, IInventarioRepository
    {
        public InventarioRepository(SigebiContext context) : base(context) { }

        public async Task<Inventario?> ObtenerPorIdAsync(
            int id,
            CancellationToken cancellationToken = default)
            => await _dbSet.FindAsync([id], cancellationToken);

        public async Task<IReadOnlyCollection<Inventario>> ObtenerTodosAsync(
            CancellationToken cancellationToken = default)
            => await _dbSet
                .OrderBy(i => i.LibroId)
                .ToListAsync(cancellationToken);

        public Task<Inventario?> ObtenerPorLibroIdAsync(
            int libroId,
            CancellationToken cancellationToken = default)
            => _dbSet.SingleOrDefaultAsync(i => i.LibroId == libroId, cancellationToken);
    }
}
