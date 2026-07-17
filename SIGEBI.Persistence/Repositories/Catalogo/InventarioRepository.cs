using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // B.R: Importamos Logger
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;
using System; // B.R: Importamos Exception
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SIGEBI.Persistence.Repositories.Catalogo
{
    // Patrón Repository: separa el acceso al inventario de las reglas del dominio.
    public class InventarioRepository : MutableRepository<Inventario>, IInventarioRepository
    {
        // B.R: Inyectamos el logger y lo pasamos a MutableRepository
        public InventarioRepository(SigebiContext context, ILogger<BaseRepository<Inventario>> logger)
            : base(context, logger) { }

        public async Task<Inventario?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                return await _dbSet.FindAsync(new object[] { id }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener inventario por ID: {Id}", id);
                throw;
            }
        }

        public async Task<IReadOnlyCollection<Inventario>> ObtenerTodosAsync(CancellationToken ct = default)
        {
            try
            {
                return await _dbSet.OrderBy(i => i.LibroId).ToListAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar todo el inventario");
                throw;
            }
        }

        public async Task<Inventario?> ObtenerPorLibroIdAsync(int libroId, CancellationToken ct = default)
        {
            try
            {
                return await _dbSet.SingleOrDefaultAsync(i => i.LibroId == libroId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener inventario por LibroID: {LibroId}", libroId);
                throw;
            }
        }
    }
}