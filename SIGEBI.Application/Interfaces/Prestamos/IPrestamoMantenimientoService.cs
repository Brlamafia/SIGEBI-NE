using SIGEBI.Application.Dtos.Prestamos;

namespace SIGEBI.Application.Interfaces.Prestamos;

public interface IPrestamoMantenimientoService
{
    Task<int> ActualizarPrestamosVencidosAsync(
        ActualizarPrestamosVencidosDto dto,
        CancellationToken cancellationToken = default);
    Task<int> GenerarRecordatoriosVencimientoAsync(
        DateTime fechaReferencia,
        CancellationToken cancellationToken = default);
}
