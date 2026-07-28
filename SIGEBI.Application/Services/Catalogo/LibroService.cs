using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Catalogo; // Asegura que este using coincida con tu carpeta de DTOs
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Catalogo;
using SIGEBI.Domain.Entities;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Application.Interfaces.Inventario;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Dtos.Inventario;
using SIGEBI.Domain.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIGEBI.Application.Services.Catalogo
{
    public class LibroService : BaseService<Libro, LibroDto>, ILibroService
    {
        private readonly IRepository<Libro> _libroRepository;
        private readonly IRepository<SolicitudPrestamo> _solicitudRepository;
        private readonly IInventarioService _inventarioService;
        private readonly IAuditoriaWriter _auditoria;
        private readonly IUsuarioActual _usuarioActual;

        // Inyectamos el repositorio de solicitudes para poder verificar si el libro está prestado
        public LibroService(
            IRepository<Libro> repository,
            IRepository<SolicitudPrestamo> solicitudRepository,
            IInventarioService inventarioService,
            IAuditoriaWriter auditoria,
            IUsuarioActual usuarioActual,
            IMapper mapper)
            : base(repository, mapper)
        {
            _libroRepository = repository;
            _solicitudRepository = solicitudRepository;
            _inventarioService = inventarioService;
            _auditoria = auditoria;
            _usuarioActual = usuarioActual;
        }

        public override async Task<LibroDto> AddAsync<TSaveDto>(TSaveDto dto)
        {
            if (dto is not SaveLibroDto datos)
                throw new ArgumentException("El contrato de creación del libro no es válido.");
            if (datos.NumeroEjemplares <= 0)
                throw new BusinessRuleException("Debe registrar al menos un ejemplar.");
            var creado = await base.AddAsync(datos);
            await _inventarioService.CrearAsync(new CrearInventarioDto
            {
                LibroId = creado.Id,
                CantidadTotal = datos.NumeroEjemplares,
                UsuarioResponsableId = _usuarioActual.UsuarioId,
                Motivo = "Registro inicial del catálogo"
            });
            await _auditoria.RegistrarAsync(
                _usuarioActual.UsuarioId,
                ModuloAuditoria.Catalogo,
                AccionAuditoria.Registrar,
                $"Libro {creado.Id} registrado: {creado.Titulo}.");
            return (await BuscarLibrosAsync(creado.ISBN)).Single();
        }

        public override async Task UpdateAsync<TUpdateDto>(int id, TUpdateDto dto)
        {
            await base.UpdateAsync(id, dto);
            await _auditoria.RegistrarAsync(
                _usuarioActual.UsuarioId,
                ModuloAuditoria.Catalogo,
                AccionAuditoria.Editar,
                $"Libro {id} actualizado.");
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
            await _auditoria.RegistrarAsync(
                _usuarioActual.UsuarioId,
                ModuloAuditoria.Catalogo,
                AccionAuditoria.Eliminar,
                $"Libro {id} retirado del catálogo.");
        }

        // Regla de Negocio: Buscador inteligente
        public async Task<IEnumerable<LibroDto>> BuscarLibrosAsync(
            string? termino = null,
            string? genero = null,
            string? editorial = null,
            bool? disponible = null,
            CancellationToken cancellationToken = default)
        {
            var libros = await _libroRepository.GetAllAsync();
            var inventarios = await _inventarioService.ObtenerTodosAsync(cancellationToken);
            var porLibro = inventarios.ToDictionary(i => i.LibroId);
            var consulta = libros.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(termino))
                consulta = consulta.Where(l =>
                    l.Titulo.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                    l.Autor.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                    l.ISBN.Contains(termino, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(genero))
                consulta = consulta.Where(l => l.Genero.Contains(genero, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(editorial))
                consulta = consulta.Where(l => l.Editorial.Contains(editorial, StringComparison.OrdinalIgnoreCase));

            var resultados = consulta.Select(libro =>
            {
                var result = _mapper.Map<LibroDto>(libro);
                if (porLibro.TryGetValue(libro.Id, out var inventario))
                {
                    result.CantidadTotal = inventario.CantidadTotal;
                    result.CantidadDisponible = inventario.CantidadDisponible;
                    result.CantidadPrestada = inventario.CantidadPrestada;
                }
                return result;
            });
            if (disponible.HasValue)
                resultados = resultados.Where(l => l.Disponible == disponible.Value);
            return resultados.OrderBy(l => l.Titulo).ToArray();
        }
    }
}
