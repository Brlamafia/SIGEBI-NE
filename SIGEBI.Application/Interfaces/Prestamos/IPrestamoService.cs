using SIGEBI.Application.Dtos.Multas;
using SIGEBI.Application.Dtos.Prestamos;

namespace SIGEBI.Application.Interfaces.Prestamos
{
    public interface IPrestamoService
    {
        Task<PrestamoDto> ObtenerPorIdAsync(
            int prestamoId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorEstadoAsync(
            string estado,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorRangoAsync(
            DateTime fechaDesde,
            DateTime fechaHasta,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<PrestamoDto>> ObtenerActivosAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<PrestamoDto>> ObtenerVencidosAsync(
            CancellationToken cancellationToken = default);

        Task<int> ActualizarPrestamosVencidosAsync(
            ActualizarPrestamosVencidosDto dto,
            CancellationToken cancellationToken = default);

        Task<PrestamoDto> RegistrarPrestamoAsync(
            RegistrarPrestamoDto dto,
            CancellationToken cancellationToken = default);

        Task RechazarSolicitudAsync(
            RechazarSolicitudPrestamoDto dto,
            CancellationToken cancellationToken = default);

        Task<MultaDto?> RegistrarDevolucionAsync(
            RegistrarDevolucionDto dto,
            CancellationToken cancellationToken = default);

        Task CancelarPrestamoAsync(
            CancelarPrestamoDto dto,
            CancellationToken cancellationToken = default);

        Task<MultaDto> RegistrarPerdidaAsync(
            RegistrarPerdidaDto dto,
            CancellationToken cancellationToken = default);

        Task<MultaDto> RegistrarDevolucionConDanioAsync(
            RegistrarDanioDto dto,
            CancellationToken cancellationToken = default);
    }
}
