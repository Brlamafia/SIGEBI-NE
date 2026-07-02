// B.R
using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;
using System.Threading;
using System.Threading.Tasks;

namespace SIGEBI.Persistence.Repositories.Usuarios
{
    public class UsuarioRepository : MutableRepository<Usuario>, IUsuarioRepository
    {
        public UsuarioRepository(SigebiContext context) : base(context) { }

        // SECCIÓN: Consultas específicas
        public async Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
            => await _dbSet.FindAsync(new object[] { id }, ct);

        public async Task<Usuario?> ObtenerPorCedulaAsync(string cedula, CancellationToken ct = default)
            => await _dbSet.FirstOrDefaultAsync(u => u.Cedula == cedula, ct);
        public async Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken ct = default)
            => await _dbSet.FirstOrDefaultAsync(u => u.Email == email, ct);
    }
}
