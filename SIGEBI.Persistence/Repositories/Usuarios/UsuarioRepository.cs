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
        public UsuarioRepository(SigebiContext context, ILogger<UsuarioRepository> logger)
            : base(context, logger) { }

        public async Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando usuario {UsuarioId}", id); return await _dbSet.FindAsync(new object[] { id }, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error buscando usuario ID {Id}", id); throw; }
        }

        public async Task<Usuario?> ObtenerPorCedulaAsync(string cedula, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando usuario por cédula"); return await _dbSet.FirstOrDefaultAsync(u => u.Cedula == cedula, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error buscando usuario por cédula {Cedula}", cedula); throw; }
        }

        public async Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando usuario por correo"); return await _dbSet.FirstOrDefaultAsync(u => u.Email == email, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error buscando usuario por email {Email}", email); throw; }
        }

        public async Task<bool> TieneRelacionesAsync(int usuarioId, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation(
                    "Verificando relaciones antes de eliminar el usuario {UsuarioId}",
                    usuarioId);
                return await _context.SolicitudesPrestamo.AnyAsync(s => s.UsuarioId == usuarioId, ct)
                    || await _context.Prestamos.AnyAsync(p => p.UsuarioId == usuarioId, ct)
                    || await _context.Multas.AnyAsync(m => m.UsuarioId == usuarioId, ct)
                    || await _context.Notificaciones.AnyAsync(n => n.UsuarioId == usuarioId, ct)
                    || await _context.Auditoria.AnyAsync(a => a.UsuarioResponsableId == usuarioId, ct)
                    || await _context.Empleados.AnyAsync(e => e.UsuarioId == usuarioId, ct)
                    || await _context.Administradores.AnyAsync(a => a.UsuarioId == usuarioId, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error verificando relaciones del usuario {UsuarioId}",
                    usuarioId);
                throw;
            }
        }
    }
}
