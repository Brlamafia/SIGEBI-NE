using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Cargos;
using SIGEBI.Application.Interfaces.Cargos;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace SIGEBI.Application.Services.Cargos
{
    public class CargoService : BaseService<Cargo, CargoDto>, ICargoService
    {
        private readonly IRepository<Cargo> _cargoRepository;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "administracion:cargos";

        public CargoService(
            IRepository<Cargo> cargoRepository,
            IMapper mapper,
            IMemoryCache cache)
            : base(cargoRepository, mapper)
        {
            _cargoRepository = cargoRepository;
            _cache = cache;
        }

        public override async Task<IEnumerable<CargoDto>> GetAllAsync()
        {
            var cargos = await _cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return (await base.GetAllAsync()).ToArray();
            });
            return cargos ?? Array.Empty<CargoDto>();
        }

        public override async Task<CargoDto> AddAsync<TSaveDto>(TSaveDto dto)
        {
            var cargo = await base.AddAsync(dto);
            _cache.Remove(CacheKey);
            return cargo;
        }

        public override async Task UpdateAsync<TUpdateDto>(int id, TUpdateDto dto)
        {
            await base.UpdateAsync(id, dto);
            _cache.Remove(CacheKey);
        }

        public override async Task DeleteAsync(int id)
        {
            await base.DeleteAsync(id);
            _cache.Remove(CacheKey);
        }
    }
}
