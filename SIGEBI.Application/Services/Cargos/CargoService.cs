using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Cargos;
using SIGEBI.Application.Interfaces.Cargos;
using SIGEBI.Domain.Entities.Usuarios; 
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Application.Services.Cargos
{
    public class CargoService : BaseService<Cargo, CargoDto>, ICargoService
    {
        private readonly IRepository<Cargo> _cargoRepository;

        public CargoService(IRepository<Cargo> cargoRepository, IMapper mapper)
            : base(cargoRepository, mapper)
        {
            _cargoRepository = cargoRepository;
        }
    }
}