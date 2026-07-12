using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Catalogo;
using SIGEBI.Application.Interfaces.Catalogo;
using SIGEBI.Domain.Entities;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Application.Services.Catalogo
{
    public class LibroService : BaseService<Libro, LibroDto>, ILibroService
    {
        public LibroService(IRepository<Libro> repository, IMapper mapper)
            : base(repository, mapper)
        {
        }
    }
}