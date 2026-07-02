using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;

namespace SIGEBI.Persistence.Repositories.Usuarios
{
    // Patrón Repository y SRP: aísla las operaciones de persistencia de empleados.
    public class EmpleadoRepository : MutableRepository<Empleado>, IEmpleadoRepository
    {
        public EmpleadoRepository(SigebiContext context) : base(context) { }

        public async Task<Empleado?> ObtenerPorIdAsync(
            int id,
            CancellationToken cancellationToken = default)
            => await _dbSet
                .Include(e => e.Usuario)
                .Include(e => e.Cargo)
                .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

        public Task<Empleado?> ObtenerPorUsuarioIdAsync(
            int usuarioId,
            CancellationToken cancellationToken = default)
            => _dbSet
                .Include(e => e.Usuario)
                .Include(e => e.Cargo)
                .SingleOrDefaultAsync(e => e.UsuarioId == usuarioId, cancellationToken);
    }
}
