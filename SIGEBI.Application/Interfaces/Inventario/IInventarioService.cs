using SIGEBI.Application.Dtos.Inventario;

namespace SIGEBI.Application.Interfaces.Inventario
{
    public interface IInventarioService
    {
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
    }
}
