using SIGEBI.Application.Dtos.Multas;
using SIGEBI.Application.Dtos.Prestamos;

namespace SIGEBI.Application.Interfaces.Prestamos;

public interface IPrestamoIncidenciaService
{
    Task<MultaDto?> RegistrarDevolucionAsync(
        RegistrarDevolucionDto dto,
        CancellationToken cancellationToken = default);
    Task<MultaDto> RegistrarPerdidaAsync(
        RegistrarPerdidaDto dto,
        CancellationToken cancellationToken = default);
    Task<MultaDto> RegistrarDevolucionConDanioAsync(
        RegistrarDanioDto dto,
        CancellationToken cancellationToken = default);
}
