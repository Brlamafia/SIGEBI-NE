using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIGEBI.Application.Base
{
    public interface IBaseService<TDto> where TDto : class
    {
        Task<IEnumerable<TDto>> GetAllAsync();
        Task<TDto> GetByIdAsync(int id);
    }
}