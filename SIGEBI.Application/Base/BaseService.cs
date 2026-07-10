using AutoMapper;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Application.Exceptions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIGEBI.Application.Base
{
    public class BaseService<TEntity, TDto> : IBaseService<TDto>
        where TEntity : class
        where TDto : class
    {
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
            if (entity == null) throw new NotFoundException(typeof(TEntity).Name, id);
            return _mapper.Map<TDto>(entity);
        }

        public virtual async Task<TDto> AddAsync<TSaveDto>(TSaveDto dto) where TSaveDto : class
        {
            var entity = _mapper.Map<TEntity>(dto);
            await _repository.AgregarAsync(entity);
            return _mapper.Map<TDto>(entity);
        }

        public virtual async Task UpdateAsync<TUpdateDto>(int id, TUpdateDto dto) where TUpdateDto : class
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) throw new NotFoundException(typeof(TEntity).Name, id);

            _mapper.Map(dto, entity);
            await _repository.ActualizarAsync(entity);
        }

        public virtual async Task DeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) throw new NotFoundException(typeof(TEntity).Name, id);

            await _repository.EliminarAsync(entity);
        }
    }
}