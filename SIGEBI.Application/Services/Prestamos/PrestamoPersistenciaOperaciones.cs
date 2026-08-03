using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Interfaces.Repositories;
using InventarioCatalogo = SIGEBI.Domain.Entities.Catalogo.Inventario;

namespace SIGEBI.Application.Services.Prestamos;

// Fachada de persistencia para los agregados que cambian juntos en un préstamo.
public sealed class PrestamoPersistenciaOperaciones(
    IPrestamoRepository prestamos,
    IMultaRepository multas,
    IInventarioRepository inventarios,
    IEjemplarRepository ejemplares) : IPrestamoPersistenciaOperaciones
{
    public Task AgregarPrestamoAsync(
        Prestamo prestamo,
        CancellationToken cancellationToken = default) =>
        prestamos.AgregarAsync(prestamo, cancellationToken);

    public Task<IReadOnlyCollection<Multa>> ObtenerMultasPorUsuarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default) =>
        multas.ObtenerPorUsuarioAsync(usuarioId, cancellationToken);

    public Task AgregarMultaAsync(
        Multa multa,
        CancellationToken cancellationToken = default) =>
        multas.AgregarAsync(multa, cancellationToken);

    public void Actualizar(
        Prestamo prestamo,
        InventarioCatalogo inventario,
        Ejemplar ejemplar)
    {
        prestamos.Actualizar(prestamo);
        inventarios.Actualizar(inventario);
        ejemplares.Actualizar(ejemplar);
    }
}
