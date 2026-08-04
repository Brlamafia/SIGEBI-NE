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
        public EjemplarRepository(SigebiContext context, ILogger<EjemplarRepository> logger)
            : base(context, logger) { }

        public async Task<Ejemplar?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando ejemplar {EjemplarId}", id); return await _dbSet.SingleOrDefaultAsync(e => e.Id == id, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error buscando Ejemplar ID {Id}", id); throw; }
        }

        public async Task<Ejemplar?> ObtenerDisponiblePorLibroAsync(int libroId, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Buscando ejemplar disponible del libro {LibroId}", libroId);
                return await _dbSet
                    .Where(e => e.LibroId == libroId && e.Estado == EstadoEjemplar.Disponible)
                    .OrderBy(e => e.Id)
                    .FirstOrDefaultAsync(ct);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error buscando ejemplar disponible para libro {LibroId}", libroId); throw; }
        }

        public async Task<Ejemplar?> ObtenerDisponibleParaPrestamoAsync(
            int libroId,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation(
                    "Bloqueando un ejemplar disponible del libro {LibroId} para préstamo",
                    libroId);
                return await _dbSet
                    .FromSqlInterpolated($$"""
                        SELECT *
                        FROM "Ejemplares"
                        WHERE id_libro = {{libroId}}
                          AND estado = 'Disponible'
                        ORDER BY id_ejemplar
                        FOR UPDATE SKIP LOCKED
                        LIMIT 1
                        """)
                    .SingleOrDefaultAsync(ct);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Error bloqueando un ejemplar disponible para el libro {LibroId}",
                    libroId);
                throw;
            }
        }

        public async Task<IReadOnlyCollection<Ejemplar>> ObtenerPorLibroAsync(int libroId, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Consultando ejemplares del libro {LibroId}", libroId);
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
                _logger.LogInformation("Consultando ejemplares del libro {LibroId} en estado {Estado}", libroId, estado);
                return await _dbSet
                    .AsNoTracking()
                    .Where(e => e.LibroId == libroId && e.Estado == estado)
                    .OrderBy(e => e.Id)
                    .ToListAsync(ct);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error listando ejemplares libro {LibroId} con estado {Estado}", libroId, estado); throw; }
        }

        public async Task AgregarRangoAsync(IEnumerable<Ejemplar> ejemplares, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Agregando un rango de ejemplares"); await _dbSet.AddRangeAsync(ejemplares, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error agregando rango de ejemplares"); throw; }
        }

        public void EliminarRango(IEnumerable<Ejemplar> ejemplares)
        {
            try { _logger.LogInformation("Eliminando un rango de ejemplares"); _dbSet.RemoveRange(ejemplares); }
            catch (Exception ex) { _logger.LogError(ex, "Error eliminando rango de ejemplares"); throw; }
        }

        public new void Actualizar(Ejemplar ejemplar)
        {
            try { _logger.LogInformation("Actualizando ejemplar {EjemplarId}", ejemplar.Id); base.Actualizar(ejemplar); }
            catch (Exception ex) { _logger.LogError(ex, "Error actualizando ejemplar {Id}", ejemplar.Id); throw; }
        }
    }
}
