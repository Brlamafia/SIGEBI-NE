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
        public EmpleadoRepository(SigebiContext context, ILogger<EmpleadoRepository> logger) : base(context, logger) { }

        public async Task<Empleado?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando empleado {EmpleadoId}", id); return await _dbSet.AsNoTracking().Include(e => e.Usuario).Include(e => e.Cargo).SingleOrDefaultAsync(e => e.Id == id, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error Empleado ID {Id}", id); throw; }
        }

        public async Task<Empleado?> ObtenerPorUsuarioIdAsync(int usuarioId, CancellationToken ct = default)
        {
            try { _logger.LogInformation("Consultando empleado del usuario {UsuarioId}", usuarioId); return await _dbSet.Include(e => e.Usuario).SingleOrDefaultAsync(e => e.UsuarioId == usuarioId, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error Empleado UsuarioID {Id}", usuarioId); throw; }
        }

        public async Task<IReadOnlyCollection<Empleado>> ObtenerPaginaAsync(
            int skip,
            int take,
            CancellationToken ct = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(skip);
            if (take is <= 0 or > 200)
                throw new ArgumentOutOfRangeException(nameof(take));

            return await _dbSet
                .AsNoTracking()
                .Include(empleado => empleado.Usuario)
                .Include(empleado => empleado.Cargo)
                .OrderBy(empleado => empleado.Usuario!.Apellido)
                .ThenBy(empleado => empleado.Usuario!.Nombre)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyCollection<Empleado>> ObtenerTodosConDetallesAsync(
            CancellationToken ct = default) =>
            await _dbSet
                .AsNoTracking()
                .Include(empleado => empleado.Usuario)
                .Include(empleado => empleado.Cargo)
                .OrderBy(empleado => empleado.Usuario!.Apellido)
                .ThenBy(empleado => empleado.Usuario!.Nombre)
                .ToListAsync(ct);

        public async Task<bool> TieneOperacionesAsync(
            int empleadoId,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation(
                    "Verificando operaciones asociadas al empleado {EmpleadoId}",
                    empleadoId);
                var tieneOperaciones = await _context.Prestamos.AnyAsync(
                    p => p.EmpleadoPrestamoId == empleadoId ||
                         p.EmpleadoDevolucionId == empleadoId,
                    ct)
                    || await _context.Multas.AnyAsync(
                    m => m.EmpleadoResolucionId == empleadoId,
                    ct);
                _logger.LogInformation(
                    "Verificación del empleado {EmpleadoId} completada. Tiene operaciones: {TieneOperaciones}",
                    empleadoId,
                    tieneOperaciones);
                return tieneOperaciones;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al verificar operaciones del empleado {EmpleadoId}",
                    empleadoId);
                throw;
            }
        }
    }
}
