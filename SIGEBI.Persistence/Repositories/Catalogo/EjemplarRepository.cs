using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SIGEBI.Persistence.Repositories.Catalogo
{
    // B.R: Ahora hereda de MutableRepository para unificar el manejo de errores
    public sealed class EjemplarRepository : MutableRepository<Ejemplar>, IEjemplarRepository
    {
        public EjemplarRepository(SigebiContext context, ILogger<BaseRepository<Ejemplar>> logger)
            : base(context, logger) { }

        public async Task<Ejemplar?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            try { return await _dbSet.SingleOrDefaultAsync(e => e.Id == id, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error buscando Ejemplar ID {Id}", id); throw; }
        }

        public async Task<Ejemplar?> ObtenerDisponiblePorLibroAsync(int libroId, CancellationToken ct = default)
        {
            try
            {
                return await _dbSet
                    .Where(e => e.LibroId == libroId && e.Estado == EstadoEjemplar.Disponible)
                    .OrderBy(e => e.Id)
                    .FirstOrDefaultAsync(ct);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error buscando ejemplar disponible para libro {LibroId}", libroId); throw; }
        }

        public async Task<IReadOnlyCollection<Ejemplar>> ObtenerPorLibroAsync(int libroId, CancellationToken ct = default)
        {
            try
            {
                return await _dbSet
                    .AsNoTracking()
                    .Where(e => e.LibroId == libroId)
                    .OrderBy(e => e.Codigo)
                    .ToListAsync(ct);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error listando ejemplares del libro {LibroId}", libroId); throw; }
        }

        public async Task<IReadOnlyCollection<Ejemplar>> ObtenerPorLibroYEstadoAsync(int libroId, EstadoEjemplar estado, CancellationToken ct = default)
        {
            try
            {
                return await _dbSet
                    .Where(e => e.LibroId == libroId && e.Estado == estado)
                    .OrderBy(e => e.Id)
                    .ToListAsync(ct);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error listando ejemplares libro {LibroId} con estado {Estado}", libroId, estado); throw; }
        }

        public async Task AgregarRangoAsync(IEnumerable<Ejemplar> ejemplares, CancellationToken ct = default)
        {
            try { await _dbSet.AddRangeAsync(ejemplares, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error agregando rango de ejemplares"); throw; }
        }

        public void EliminarRango(IEnumerable<Ejemplar> ejemplares)
        {
            try { _dbSet.RemoveRange(ejemplares); }
            catch (Exception ex) { _logger.LogError(ex, "Error eliminando rango de ejemplares"); throw; }
        }

        public new void Actualizar(Ejemplar ejemplar)
        {
            try { base.Actualizar(ejemplar); }
            catch (Exception ex) { _logger.LogError(ex, "Error actualizando ejemplar {Id}", ejemplar.Id); throw; }
        }
    }
}