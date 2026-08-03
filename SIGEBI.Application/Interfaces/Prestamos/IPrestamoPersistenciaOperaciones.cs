using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Prestamos;
using InventarioCatalogo = SIGEBI.Domain.Entities.Catalogo.Inventario;

namespace SIGEBI.Application.Interfaces.Prestamos;

public interface IPrestamoPersistenciaOperaciones
{
    Task AgregarPrestamoAsync(
        Prestamo prestamo,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Multa>> ObtenerMultasPorUsuarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);
    Task AgregarMultaAsync(
        Multa multa,
        CancellationToken cancellationToken = default);
    void Actualizar(
        Prestamo prestamo,
        InventarioCatalogo inventario,
        Ejemplar ejemplar);
}
