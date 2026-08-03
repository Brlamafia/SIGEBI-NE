using SIGEBI.Application.Dtos.Prestamos;

namespace SIGEBI.Application.Interfaces.Prestamos;

public interface IPrestamoRegistroService
{
    Task<PrestamoDto> RegistrarPrestamoAsync(
        RegistrarPrestamoDto dto,
        CancellationToken cancellationToken = default);
}
