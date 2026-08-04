using SIGEBI.Domain.Entities.Usuarios;

namespace SIGEBI.Domain.Interfaces.Repositories
{
    // Inversión de Dependencias (DIP): Domain define el contrato sin depender de Entity Framework.
    public interface IEmpleadoRepository
    {
        Task<Empleado?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Empleado?> ObtenerPorUsuarioIdAsync(int usuarioId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Empleado>> ObtenerPaginaAsync(
            int skip,
            int take,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Empleado>> ObtenerTodosConDetallesAsync(
            CancellationToken cancellationToken = default);
        Task<bool> TieneOperacionesAsync(int empleadoId, CancellationToken cancellationToken = default);
        Task AgregarAsync(Empleado empleado, CancellationToken cancellationToken = default);
        void Actualizar(Empleado empleado);
    }
}
