using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SIGEBI.Domain.Entities.Catalogo;

namespace SIGEBI.Domain.Interfaces.Repositories
{
    // B.R Contrato para el repositorio de libros, desacoplando la persistencia de la lógica de negocio.
    public interface ILibroRepository : IRepository<Libro>
    {
        Task<Libro?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Libro>> ObtenerPorIdsAsync(
            IReadOnlyCollection<int> ids,
            CancellationToken cancellationToken = default);
        Task<Libro?> ObtenerPorIsbnAsync(string isbn, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Libro>> BuscarPorCriterioAsync(string criterio, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Libro>> BuscarAsync(
            string? termino,
            string? genero,
            string? editorial,
            bool? disponible = null,
            int? skip = null,
            int? take = null,
            CancellationToken cancellationToken = default);
    }
}
