using SIGEBI.Application.Dtos.Inventario;

namespace SIGEBI.Application.Interfaces.Inventario
{
    public interface IInventarioService
    {
        Task<IReadOnlyCollection<InventarioDto>> ObtenerTodosAsync(
            CancellationToken cancellationToken = default);

        Task<InventarioDto> CrearAsync(
            CrearInventarioDto dto,
            CancellationToken cancellationToken = default);

        Task<InventarioDto> ObtenerPorIdAsync(
            int inventarioId,
            CancellationToken cancellationToken = default);

        Task<InventarioDto> ObtenerPorLibroIdAsync(
            int libroId,
            CancellationToken cancellationToken = default);

        Task<InventarioDto> AjustarCantidadTotalAsync(
            AjustarInventarioDto dto,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<EjemplarDto>> ObtenerEjemplaresPorLibroAsync(
            int libroId,
            CancellationToken cancellationToken = default);

        Task<EjemplarDto> CambiarEstadoEjemplarAsync(
            CambiarEstadoEjemplarDto dto,
            CancellationToken cancellationToken = default);
    }
}
