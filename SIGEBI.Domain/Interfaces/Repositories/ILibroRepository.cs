using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SIGEBI.Domain.Entities.Catalogo;

namespace SIGEBI.Domain.Interfaces.Repositories
{
    // B.R Contrato para el repositorio de libros, desacoplando la persistencia de la lógica de negocio.
    public interface ILibroRepository
    {
        Task<Libro?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Libro?> ObtenerPorIsbnAsync(string isbn, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Libro>> BuscarPorCriterioAsync(string criterio, CancellationToken cancellationToken = default);
        Task AgregarAsync(Libro libro, CancellationToken cancellationToken = default);
        void Actualizar(Libro libro);
    }
}