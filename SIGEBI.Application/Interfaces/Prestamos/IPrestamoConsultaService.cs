using SIGEBI.Application.Dtos.Prestamos;

namespace SIGEBI.Application.Interfaces.Prestamos;

public interface IPrestamoConsultaService
{
    Task<PrestamoDto> ObtenerPorIdAsync(int prestamoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorLibroAsync(int libroId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorEjemplarAsync(int ejemplarId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PrestamoDto>> ObtenerDevolucionesPorUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PrestamoDto>> ObtenerDevolucionesPorLibroAsync(int libroId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorEstadoAsync(string estado, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorRangoAsync(DateTime fechaDesde, DateTime fechaHasta, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PrestamoDto>> ObtenerActivosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PrestamoDto>> ObtenerVencidosAsync(CancellationToken cancellationToken = default);
}
