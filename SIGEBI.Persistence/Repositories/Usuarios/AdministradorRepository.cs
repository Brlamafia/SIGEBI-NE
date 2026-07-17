using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SIGEBI.Persistence.Repositories.Usuarios
{
    public class AdministradorRepository : MutableRepository<Administrador>, IAdministradorRepository
    {
        public AdministradorRepository(SigebiContext context, ILogger<BaseRepository<Administrador>> logger) : base(context, logger) { }

        public async Task<Administrador?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            try { return await _dbSet.Include(a => a.Usuario).Include(a => a.Cargo).SingleOrDefaultAsync(a => a.Id == id, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error Admin ID {Id}", id); throw; }
        }

        public async Task<Administrador?> ObtenerPorUsuarioIdAsync(int usuarioId, CancellationToken ct = default)
        {
            try { return await _dbSet.Include(a => a.Usuario).SingleOrDefaultAsync(a => a.UsuarioId == usuarioId, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error Admin UsuarioID {Id}", usuarioId); throw; }
        }

        public async Task<IReadOnlyCollection<Administrador>> ObtenerTodosAsync(CancellationToken ct = default)
        {
            try { return await _dbSet.Include(a => a.Usuario).ToListAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Error listando Admins"); throw; }
        }
    }
}