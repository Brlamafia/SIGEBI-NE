// B.R
using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Context;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SIGEBI.Persistence.Base
{
    public abstract class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly SigebiContext _context;
        protected readonly DbSet<T> _dbSet;

        protected BaseRepository(SigebiContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<T> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task AgregarAsync(T entity, CancellationToken ct = default)
        {
            await _dbSet.AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);
        }

        public virtual async Task ActualizarAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public virtual async Task EliminarAsync(T entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}