using System.Threading;
using System.Threading.Tasks;
using SIGEBI.Domain.Entities.Usuarios;

namespace SIGEBI.Domain.Interfaces.Repositories
{
    // B.R Contrato para el repositorio de usuarios, desacoplando la persistencia de la lógica de negocio.
    public interface IUsuarioRepository
    {
        Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Usuario?> ObtenerPorCedulaAsync(string cedula, CancellationToken cancellationToken = default);
        Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken cancellationToken = default);
        Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default);
        void Actualizar(Usuario usuario);
    }
}