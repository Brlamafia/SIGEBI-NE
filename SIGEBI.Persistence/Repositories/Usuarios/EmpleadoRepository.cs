using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SIGEBI.Persistence.Repositories.Usuarios
{
    public class EmpleadoRepository : MutableRepository<Empleado>, IEmpleadoRepository
    {
        public EmpleadoRepository(SigebiContext context, ILogger<BaseRepository<Empleado>> logger) : base(context, logger) { }

        public async Task<Empleado?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            try { return await _dbSet.Include(e => e.Usuario).SingleOrDefaultAsync(e => e.Id == id, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error Empleado ID {Id}", id); throw; }
        }

        public async Task<Empleado?> ObtenerPorUsuarioIdAsync(int usuarioId, CancellationToken ct = default)
        {
            try { return await _dbSet.Include(e => e.Usuario).SingleOrDefaultAsync(e => e.UsuarioId == usuarioId, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error Empleado UsuarioID {Id}", usuarioId); throw; }
        }
    }
}