using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Catalogo;

namespace SIGEBI.Application.Interfaces.Catalogo
{
    public interface ILibroService : IBaseService<LibroDto>
    {
        Task<IEnumerable<LibroDto>> BuscarLibrosAsync(string termino);
    }
}
