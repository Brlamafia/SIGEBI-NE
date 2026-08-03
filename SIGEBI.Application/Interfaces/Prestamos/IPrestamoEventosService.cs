using SIGEBI.Application.Models.Prestamos;

namespace SIGEBI.Application.Interfaces.Prestamos;

public interface IPrestamoEventosService
{
    Task AgregarRangoAsync(
        IReadOnlyCollection<PrestamoEventoAplicacion> eventos,
        CancellationToken cancellationToken = default);
    Task<int> AgregarRecordatoriosSiNoExistenAsync(
        IReadOnlyCollection<PrestamoRecordatorio> recordatorios,
        DateTime desde,
        CancellationToken cancellationToken = default);
}
