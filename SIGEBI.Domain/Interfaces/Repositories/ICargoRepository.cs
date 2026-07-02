using SIGEBI.Domain.Entities.Usuarios;

namespace SIGEBI.Domain.Interfaces.Repositories
{
    // Inversión de Dependencias (DIP): Domain define cómo acceder a cargos sin conocer EF Core.
    public interface ICargoRepository
    {
        Task<Cargo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Cargo?> ObtenerPorNombreAsync(string nombre, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Cargo>> ObtenerTodosAsync(CancellationToken cancellationToken = default);
        Task AgregarAsync(Cargo cargo, CancellationToken cancellationToken = default);
        void Actualizar(Cargo cargo);
    }
}
