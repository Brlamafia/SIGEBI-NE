// B.R
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SIGEBI.Domain.Interfaces.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(int id);
        Task AgregarAsync(T entidad, CancellationToken ct = default);
    }
}