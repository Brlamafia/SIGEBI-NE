using SIGEBI.Domain.Entities.Usuarios;

namespace SIGEBI.Domain.Interfaces.Repositories
{
    // Inversión de Dependencias (DIP): la abstracción pertenece al núcleo del sistema.
    public interface IAdministradorRepository
    {
        Task<Administrador?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Administrador?> ObtenerPorUsuarioIdAsync(int usuarioId, CancellationToken cancellationToken = default);
        Task AgregarAsync(Administrador administrador, CancellationToken cancellationToken = default);
        void Actualizar(Administrador administrador);
    }
}
