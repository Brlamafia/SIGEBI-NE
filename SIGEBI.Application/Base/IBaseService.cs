using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIGEBI.Application.Base
{
    public interface IBaseService<TDto> where TDto : class
    {
        Task<IEnumerable<TDto>> GetAllAsync();
        Task<TDto> GetByIdAsync(int id);
        Task<TDto> AddAsync<TSaveDto>(TSaveDto dto) where TSaveDto : class;
        Task UpdateAsync<TUpdateDto>(int id, TUpdateDto dto) where TUpdateDto : class;
        Task DeleteAsync(int id);
    }
}