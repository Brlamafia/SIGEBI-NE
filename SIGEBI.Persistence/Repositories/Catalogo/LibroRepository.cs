// ... existing code ...
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // B.R: Agregamos el namespace del Logger
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;
using System; // B.R: Agregamos System para manejar Exceptions
using System.Collections.Generic;
using System.Linq;
using System.Threading;
// ... existing code ...
namespace SIGEBI.Persistence.Repositories.Catalogo
{
    public class LibroRepository : MutableRepository<Libro>, ILibroRepository
    {
        // B.R: Pedimos el Logger en el constructor y se lo pasamos a la clase base (MutableRepository)
        public LibroRepository(SigebiContext context, ILogger<LibroRepository> logger) : base(context, logger) { }

        public async Task<IReadOnlyCollection<Libro>> BuscarPorCriterioAsync(string criterio, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Buscando libros con el criterio: {Criterio}", criterio);
                return await _dbSet
                    .Where(l => l.Titulo.Contains(criterio) || l.Autor.Contains(criterio))
                    .ToListAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar libros con el criterio: {Criterio}", criterio);
                throw;
            }
        }

        public async Task<IReadOnlyCollection<Libro>> BuscarAsync(
            string? termino,
            string? genero,
            string? editorial,
            bool? disponible = null,
            int? skip = null,
            int? take = null,
            CancellationToken ct = default)
        {
            try
            {
                IQueryable<Libro> query = _dbSet.AsNoTracking();
                if (!string.IsNullOrWhiteSpace(termino))
                {
                    var pattern = $"%{termino.Trim()}%";
                    query = query.Where(libro =>
                        EF.Functions.ILike(libro.Titulo, pattern) ||
                        EF.Functions.ILike(libro.Autor, pattern) ||
                        EF.Functions.ILike(libro.ISBN, pattern));
                }

                if (!string.IsNullOrWhiteSpace(genero))
                    query = query.Where(libro =>
                        EF.Functions.ILike(libro.Genero, $"%{genero.Trim()}%"));
                if (!string.IsNullOrWhiteSpace(editorial))
                    query = query.Where(libro =>
                        EF.Functions.ILike(libro.Editorial, $"%{editorial.Trim()}%"));
                if (disponible.HasValue)
                {
                    query = disponible.Value
                        ? query.Where(libro => _context.Inventario.Any(
                            inventory =>
                                inventory.LibroId == libro.Id &&
                                inventory.CantidadDisponible > 0))
                        : query.Where(libro => !_context.Inventario.Any(
                            inventory =>
                                inventory.LibroId == libro.Id &&
                                inventory.CantidadDisponible > 0));
                }

                query = query.OrderBy(libro => libro.Titulo);
                if (skip.HasValue)
                    query = query.Skip(skip.Value);
                if (take.HasValue)
                    query = query.Take(take.Value);
                return await query.ToListAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al filtrar el catálogo");
                throw;
            }
        }

        public async Task<Libro?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Consultando libro {LibroId}", id);
                return await _dbSet.FindAsync(new object[] { id }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el libro con ID: {Id}", id);
                throw;
            }
        }

        public async Task<Libro?> ObtenerPorIsbnAsync(string isbn, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Consultando libro con ISBN {ISBN}", isbn);
                return await _dbSet.FirstOrDefaultAsync(l => l.ISBN == isbn, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el libro con ISBN: {ISBN}", isbn);
                throw;
            }
        }
    }
}
