using AutoMapper;
using SIGEBI.Domain.Interfaces.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIGEBI.Application.Base
{
    public class BaseService<TEntity, TDto> : IBaseService<TDto> where TEntity : class where TDto : class
    {
        // Regresamos a IRepository
        protected readonly IRepository<TEntity> _repository;
        protected readonly IMapper _mapper;

        public BaseService(IRepository<TEntity> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public virtual async Task<IEnumerable<TDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<TDto>>(entities);
        }

        public virtual async Task<TDto> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return _mapper.Map<TDto>(entity);
        }
    }
}