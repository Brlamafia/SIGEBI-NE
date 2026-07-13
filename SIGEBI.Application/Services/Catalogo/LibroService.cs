using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Catalogo; // Asegura que este using coincida con tu carpeta de DTOs
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Catalogo;
using SIGEBI.Domain.Entities;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Interfaces.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIGEBI.Application.Services.Catalogo
{
    public class LibroService : BaseService<Libro, LibroDto>, ILibroService
    {
        private readonly IRepository<Libro> _libroRepository;
        private readonly IRepository<SolicitudPrestamo> _solicitudRepository;

        // Inyectamos el repositorio de solicitudes para poder verificar si el libro está prestado
        public LibroService(IRepository<Libro> repository, IRepository<SolicitudPrestamo> solicitudRepository, IMapper mapper)
            : base(repository, mapper)
        {
            _libroRepository = repository;
            _solicitudRepository = solicitudRepository;
        }

        // Regla de Negocio: Candado de borrado
        public override async Task DeleteAsync(int id)
        {
            var solicitudes = await _solicitudRepository.GetAllAsync();
            var libroPrestado = solicitudes.Any(s => s.LibroId == id && (s.Estado.ToString() == "Pendiente" || s.Estado.ToString() == "Aprobada"));

            if (libroPrestado)
            {
                throw new BusinessRuleException("Imposible descatalogar: Este libro tiene copias prestadas o solicitudes pendientes.");
            }

            await base.DeleteAsync(id);
        }

        // Regla de Negocio: Buscador inteligente
        public async Task<IEnumerable<LibroDto>> BuscarLibrosAsync(string termino)
        {
            var libros = await _libroRepository.GetAllAsync();

            if (string.IsNullOrWhiteSpace(termino))
                return _mapper.Map<IEnumerable<LibroDto>>(libros);

            termino = termino.ToLower();
            var resultados = libros.Where(l =>
                (l.Titulo != null && l.Titulo.ToLower().Contains(termino)) ||
                (l.Autor != null && l.Autor.ToLower().Contains(termino)) ||
                (l.ISBN != null && l.ISBN.Contains(termino))
            ).ToList();

            return _mapper.Map<IEnumerable<LibroDto>>(resultados);
        }
    }
}