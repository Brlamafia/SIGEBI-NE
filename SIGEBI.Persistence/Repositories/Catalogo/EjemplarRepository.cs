using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Context;

namespace SIGEBI.Persistence.Repositories.Catalogo;

public sealed class EjemplarRepository(SigebiContext context) : IEjemplarRepository
{
    private readonly DbSet<Ejemplar> _ejemplares = context.Set<Ejemplar>();

    public Task<Ejemplar?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        => _ejemplares.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<Ejemplar?> ObtenerDisponiblePorLibroAsync(
        int libroId,
        CancellationToken cancellationToken = default)
        => _ejemplares
            .Where(e => e.LibroId == libroId && e.Estado == EstadoEjemplar.Disponible)
            .OrderBy(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Ejemplar>> ObtenerPorLibroAsync(
        int libroId,
        CancellationToken cancellationToken = default)
        => await _ejemplares
            .AsNoTracking()
            .Where(e => e.LibroId == libroId)
            .OrderBy(e => e.Codigo)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Ejemplar>> ObtenerPorLibroYEstadoAsync(
        int libroId,
        EstadoEjemplar estado,
        CancellationToken cancellationToken = default)
        => await _ejemplares
            .Where(e => e.LibroId == libroId && e.Estado == estado)
            .OrderBy(e => e.Id)
            .ToListAsync(cancellationToken);

    public Task AgregarRangoAsync(
        IEnumerable<Ejemplar> ejemplares,
        CancellationToken cancellationToken = default)
        => _ejemplares.AddRangeAsync(ejemplares, cancellationToken);

    public void Actualizar(Ejemplar ejemplar) => _ejemplares.Update(ejemplar);
    public void EliminarRango(IEnumerable<Ejemplar> ejemplares) => _ejemplares.RemoveRange(ejemplares);
}
