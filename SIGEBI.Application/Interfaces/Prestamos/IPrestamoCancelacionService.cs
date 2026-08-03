using SIGEBI.Application.Dtos.Prestamos;

namespace SIGEBI.Application.Interfaces.Prestamos;

public interface IPrestamoCancelacionService
{
    Task CancelarPrestamoAsync(
        CancelarPrestamoDto dto,
        CancellationToken cancellationToken = default);
}
