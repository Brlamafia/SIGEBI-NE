using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SIGEBI.Persistence.Repositories.Usuarios
{
    public class CargoRepository : MutableRepository<Cargo>, ICargoRepository
    {
        public CargoRepository(SigebiContext context, ILogger<BaseRepository<Cargo>> logger) : base(context, logger) { }

        public async Task<Cargo?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            try { return await _dbSet.FindAsync(new object[] { id }, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error Cargo ID {Id}", id); throw; }
        }

        public async Task<Cargo?> ObtenerPorNombreAsync(string nombre, CancellationToken ct = default)
        {
            try { return await _dbSet.SingleOrDefaultAsync(c => c.Nombre == nombre, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error Cargo Nombre {Nombre}", nombre); throw; }
        }

        public async Task<IReadOnlyCollection<Cargo>> ObtenerTodosAsync(CancellationToken ct = default)
        {
            try { return await _dbSet.ToListAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Error listando cargos"); throw; }
        }
    }
}