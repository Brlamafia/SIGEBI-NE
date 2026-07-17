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
    public class UsuarioRepository : MutableRepository<Usuario>, IUsuarioRepository
    {
        public UsuarioRepository(SigebiContext context, ILogger<BaseRepository<Usuario>> logger)
            : base(context, logger) { }

        public async Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            try { return await _dbSet.FindAsync(new object[] { id }, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error buscando usuario ID {Id}", id); throw; }
        }

        public async Task<Usuario?> ObtenerPorCedulaAsync(string cedula, CancellationToken ct = default)
        {
            try { return await _dbSet.FirstOrDefaultAsync(u => u.Cedula == cedula, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error buscando usuario por cédula {Cedula}", cedula); throw; }
        }

        public async Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken ct = default)
        {
            try { return await _dbSet.FirstOrDefaultAsync(u => u.Email == email, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error buscando usuario por email {Email}", email); throw; }
        }
    }
}