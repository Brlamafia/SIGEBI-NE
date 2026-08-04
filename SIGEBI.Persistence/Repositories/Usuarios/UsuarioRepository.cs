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

        public async Task<IReadOnlyCollection<Usuario>> ObtenerPorIdsAsync(
            IReadOnlyCollection<int> ids,
            CancellationToken ct = default)
        {
            if (ids.Count == 0)
                return Array.Empty<Usuario>();

            try
            {
                _logger.LogInformation("Consultando {Cantidad} usuarios en un solo lote", ids.Count);
                return await _dbSet
                    .AsNoTracking()
                    .Where(usuario => ids.Contains(usuario.Id))
                    .ToListAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el lote de usuarios");
                throw;
            }
        }

        public async Task<Usuario?> ObtenerPorCedulaAsync(string cedula, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando usuario por cédula"); return await _dbSet.FirstOrDefaultAsync(u => u.Cedula == cedula, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error buscando usuario por cédula {Cedula}", cedula); throw; }
        }

        public async Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken ct = default)
        {
            var emailNormalizado = email.Trim().ToLowerInvariant();
            try { _logger.LogInformation("Consultando usuario por correo"); return await _dbSet.AsSingleQuery().Include(u => u.Roles).ThenInclude(r => r.Permisos).FirstOrDefaultAsync(u => u.Email == emailNormalizado, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error buscando usuario por email {Email}", email); throw; }
        }

        public Task<Usuario?> ObtenerPorIdConRolesAsync(int id, CancellationToken ct = default) =>
            _dbSet.AsSingleQuery().Include(u => u.Roles).ThenInclude(r => r.Permisos)
                .FirstOrDefaultAsync(u => u.Id == id, ct);

        public async Task<IReadOnlyCollection<Usuario>> ObtenerPaginaAsync(
            int skip,
            int take,
            CancellationToken ct = default)
        {
            if (skip < 0)
                throw new ArgumentOutOfRangeException(nameof(skip));
            if (take is <= 0 or > 200)
                throw new ArgumentOutOfRangeException(nameof(take));

            return await _dbSet
                .AsNoTracking()
                .OrderBy(usuario => usuario.Apellido)
                .ThenBy(usuario => usuario.Nombre)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }

        public async Task<bool> TieneRelacionesAsync(int usuarioId, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation(
                    "Verificando relaciones antes de eliminar el usuario {UsuarioId}",
                    usuarioId);
                var relaciones = _context.SolicitudesPrestamo
                    .Where(s => s.UsuarioId == usuarioId)
                    .Select(_ => 1)
                    .Concat(_context.Prestamos.Where(p => p.UsuarioId == usuarioId).Select(_ => 1))
                    .Concat(_context.Multas.Where(m => m.UsuarioId == usuarioId).Select(_ => 1))
                    .Concat(_context.Notificaciones.Where(n => n.UsuarioId == usuarioId).Select(_ => 1))
                    .Concat(_context.Auditoria.Where(a => a.UsuarioResponsableId == usuarioId).Select(_ => 1))
                    .Concat(_context.Empleados.Where(e => e.UsuarioId == usuarioId).Select(_ => 1))
                    .Concat(_context.Administradores.Where(a => a.UsuarioId == usuarioId).Select(_ => 1));
                return await relaciones.AnyAsync(ct);
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
