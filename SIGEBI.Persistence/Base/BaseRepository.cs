// B.R
using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Context;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SIGEBI.Persistence.Base
{
    // B.R: Clase base para centralizar el acceso al Contexto y DbSet
    // DRY: centraliza el acceso compartido al contexto y al DbSet.
    // Ahora implementa IRepository<T>
    public abstract class BaseRepository<T> : IRepository<T> where T : class
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

        // Implementación de los métodos requeridos por IRepository
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<T> GetByIdAsync(int id)
        {
            // FindAsync es la forma óptima de buscar por llave primaria en EF Core
            return await _dbSet.FindAsync(id);
        }
    }
}