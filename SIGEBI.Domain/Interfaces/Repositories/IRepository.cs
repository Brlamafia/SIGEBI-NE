using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SIGEBI.Domain.Interfaces.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
        Task<IReadOnlyCollection<T>> GetPageAsync(
            int skip,
            int take,
            CancellationToken ct = default);
        Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
        Task AgregarAsync(T entity, CancellationToken ct = default);
        Task ActualizarAsync(T entity, CancellationToken ct = default);
        Task EliminarAsync(T entity, CancellationToken ct = default);
    }
}
