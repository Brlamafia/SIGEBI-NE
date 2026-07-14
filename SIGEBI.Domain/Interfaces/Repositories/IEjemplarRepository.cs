using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Enums;

namespace SIGEBI.Domain.Interfaces.Repositories;

public interface IEjemplarRepository
{
    Task<Ejemplar?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Ejemplar?> ObtenerDisponiblePorLibroAsync(int libroId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Ejemplar>> ObtenerPorLibroAsync(
        int libroId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Ejemplar>> ObtenerPorLibroYEstadoAsync(
        int libroId,
        EstadoEjemplar estado,
        CancellationToken cancellationToken = default);
    Task AgregarRangoAsync(IEnumerable<Ejemplar> ejemplares, CancellationToken cancellationToken = default);
    void Actualizar(Ejemplar ejemplar);
    void EliminarRango(IEnumerable<Ejemplar> ejemplares);
}
