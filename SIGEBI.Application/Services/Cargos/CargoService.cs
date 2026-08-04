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
        private readonly IEmpleadoRepository _empleados;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "administracion:cargos";

        public CargoService(
            IRepository<Cargo> cargoRepository,
            IEmpleadoRepository empleados,
            IMapper mapper,
            IMemoryCache cache)
            : base(cargoRepository, mapper)
        {
            _cargoRepository = cargoRepository;
            _empleados = empleados;
            _cache = cache;
        }

        public override async Task<IEnumerable<CargoDto>> GetAllAsync()
        {
            var cargos = await _cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return (await base.GetAllAsync()).ToArray();
            });
            return await EnriquecerAsync(cargos ?? Array.Empty<CargoDto>());
        }

        public override async Task<IReadOnlyCollection<CargoDto>> GetPageAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var cargos = await base.GetPageAsync(page, pageSize, cancellationToken);
            return await EnriquecerAsync(cargos, cancellationToken);
        }

        public override async Task<CargoDto> GetByIdAsync(int id)
        {
            var cargo = await base.GetByIdAsync(id);
            return (await EnriquecerAsync([cargo])).Single();
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

        private async Task<IReadOnlyCollection<CargoDto>> EnriquecerAsync(
            IEnumerable<CargoDto> cargos,
            CancellationToken cancellationToken = default)
        {
            var resultado = cargos.ToArray();
            var empleados = await _empleados.ObtenerTodosConDetallesAsync(
                cancellationToken);
            foreach (var cargo in resultado)
            {
                var asignados = empleados
                    .Where(empleado => empleado.CargoId == cargo.Id)
                    .Select(empleado => empleado.Usuario)
                    .Where(usuario => usuario is not null)
                    .ToArray();
                cargo.PersonalAsignado = asignados.Length == 0
                    ? "Sin personal asignado"
                    : string.Join(", ", asignados.Select(usuario =>
                        $"{usuario!.Nombre} {usuario.Apellido}".Trim()));
                cargo.CorreosAsociados = asignados.Length == 0
                    ? "Sin correos asociados"
                    : string.Join(", ", asignados.Select(usuario => usuario!.Email));
            }
            return resultado;
        }
    }
}
