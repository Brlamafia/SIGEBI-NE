using SIGEBI.Application.Dtos.Auditoria;

namespace SIGEBI.Application.Interfaces.Auditoria
{
    public interface IAuditoriaService
    {
        Task<AuditoriaDto> ObtenerPorIdAsync(
            int auditoriaId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<AuditoriaDto>> ObtenerPorUsuarioAsync(
            int usuarioResponsableId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<AuditoriaDto>> ObtenerPorModuloAsync(
            string modulo,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<AuditoriaDto>> ObtenerPorRangoAsync(
            DateTime fechaDesde,
            DateTime fechaHasta,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<AuditoriaDto>> FiltrarAsync(
            FiltroAuditoriaDto filtro,
            CancellationToken cancellationToken = default);
    }
}
