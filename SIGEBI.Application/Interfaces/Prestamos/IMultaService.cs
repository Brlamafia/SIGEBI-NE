using SIGEBI.Application.Dtos.Multas;

namespace SIGEBI.Application.Interfaces.Prestamos
{
    public interface IMultaService
    {
        Task<MultaDto> ObtenerPorIdAsync(
            int multaId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<MultaDto>> ObtenerPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<MultaDto>> ObtenerPorEstadoAsync(
            string estado,
            CancellationToken cancellationToken = default);

        Task<bool> TienePendientesPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default);

        Task MarcarComoPagadaAsync(
            PagarMultaDto dto,
            CancellationToken cancellationToken = default);

        Task ResolverAsync(
            ResolverMultaDto dto,
            CancellationToken cancellationToken = default);
    }
}
