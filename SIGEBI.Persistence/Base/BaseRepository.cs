// B.R
// Patrón Repository: base común para implementaciones de persistencia.
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using SIGEBI.Persistence.Context;

namespace SIGEBI.Persistence.Base
{
    // B.R: Clase base para centralizar el acceso al Contexto y DbSet
    // DRY: centraliza el acceso compartido al contexto y al DbSet.
    public abstract class BaseRepository<T> where T : class
    {
        protected readonly SigebiContext _context;
        protected readonly DbSet<T> _dbSet;

        protected BaseRepository(SigebiContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task AgregarAsync(T entidad, CancellationToken ct = default)
            => await _dbSet.AddAsync(entidad, ct);

    }
}
