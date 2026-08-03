using SIGEBI.Application.Dtos.Prestamos;

namespace SIGEBI.Application.Interfaces.Prestamos;

public interface ISolicitudPrestamoDecisionService
{
    Task RechazarSolicitudAsync(
        RechazarSolicitudPrestamoDto dto,
        CancellationToken cancellationToken = default);
}
