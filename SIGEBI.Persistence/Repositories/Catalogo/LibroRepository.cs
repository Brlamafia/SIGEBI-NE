// B.R
using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SIGEBI.Persistence.Repositories.Catalogo
{
    public class LibroRepository : BaseRepository<Libro>, ILibroRepository
    {
        public LibroRepository(SigebiContext context) : base(context) { }

        public async Task<IReadOnlyCollection<Libro>> BuscarPorCriterioAsync(string criterio, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(l => l.Titulo.Contains(criterio) || l.Autor.Contains(criterio))
                .ToListAsync(ct);
        }

        public async Task<Libro?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
            => await _dbSet.FindAsync(new object[] { id }, ct);

        public async Task<Libro?> ObtenerPorIsbnAsync(string isbn, CancellationToken ct = default)
            => await _dbSet.FirstOrDefaultAsync(l => l.ISBN == isbn, ct);

    } 
}