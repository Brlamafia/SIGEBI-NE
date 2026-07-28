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
using SIGEBI.Domain.Interfaces;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIGEBI.Application.Services.Catalogo
{
    public class LibroService : BaseService<Libro, LibroDto>, ILibroService
    {
        private readonly ILibroRepository _libroRepository;
        private readonly IPrestamoRepository _prestamos;
        private readonly IInventarioService _inventarioService;
        private readonly IAuditoriaWriter _auditoria;
        private readonly IUsuarioActual _usuarioActual;
        private readonly IUnitOfWork _unitOfWork;

        // Inyectamos el repositorio de solicitudes para poder verificar si el libro está prestado
        public LibroService(
            ILibroRepository repository,
            IPrestamoRepository prestamos,
            IInventarioService inventarioService,
            IAuditoriaWriter auditoria,
            IUsuarioActual usuarioActual,
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : base(repository, mapper)
        {
            _libroRepository = repository;
            _prestamos = prestamos;
            _inventarioService = inventarioService;
            _auditoria = auditoria;
            _usuarioActual = usuarioActual;
            _unitOfWork = unitOfWork;
        }

        public override async Task<LibroDto> AddAsync<TSaveDto>(TSaveDto dto)
        {
            if (dto is not SaveLibroDto datos)
                throw new ArgumentException("El contrato de creación del libro no es válido.");
            if (datos.NumeroEjemplares <= 0)
                throw new BusinessRuleException("Debe registrar al menos un ejemplar.");
            LibroDto? creado = null;
            await _unitOfWork.EjecutarEnTransaccionAsync(async cancellationToken =>
            {
                creado = await base.AddAsync(datos);
                await _inventarioService.CrearAsync(new CrearInventarioDto
                {
                    LibroId = creado.Id,
                    CantidadTotal = datos.NumeroEjemplares,
                    UsuarioResponsableId = _usuarioActual.UsuarioId,
                    Motivo = "Registro inicial del catálogo"
                }, cancellationToken);
                await _auditoria.RegistrarAsync(
                    _usuarioActual.UsuarioId,
                    ModuloAuditoria.Catalogo,
                    AccionAuditoria.Registrar,
                    $"Libro {creado.Id} registrado: {creado.Titulo}.",
                    cancellationToken: cancellationToken);
            }, IsolationLevel.Serializable);

            if (creado is null)
                throw new InvalidOperationException("No se pudo crear el libro.");
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
            var libro = await _libroRepository.GetByIdAsync(id)
                ?? throw new NotFoundException(nameof(Libro), id);
            var prestamos = await _prestamos.ObtenerPorLibroAsync(id);
            var libroPrestado = prestamos.Any(prestamo =>
                prestamo.Estado is EstadoPrestamo.Activo or EstadoPrestamo.Vencido);

            if (libroPrestado)
            {
                throw new BusinessRuleException("Imposible descatalogar: Este libro tiene copias prestadas o solicitudes pendientes.");
            }

            await _unitOfWork.EjecutarEnTransaccionAsync(async cancellationToken =>
            {
                libro.Descatalogar();
                await _libroRepository.ActualizarAsync(libro);
                await _auditoria.RegistrarAsync(
                    _usuarioActual.UsuarioId,
                    ModuloAuditoria.Catalogo,
                    AccionAuditoria.ActualizarEstado,
                    $"Libro {id} descatalogado.",
                    cancellationToken: cancellationToken);
            }, IsolationLevel.Serializable);
        }

        // Regla de Negocio: Buscador inteligente
        public async Task<IEnumerable<LibroDto>> BuscarLibrosAsync(
            string? termino = null,
            string? genero = null,
            string? editorial = null,
            bool? disponible = null,
            int? skip = null,
            int? take = null,
            CancellationToken cancellationToken = default)
        {
            var libros = await _libroRepository.BuscarAsync(
                termino,
                genero,
                editorial,
                disponible,
                skip,
                take,
                cancellationToken);
            var inventarios = await _inventarioService.ObtenerPorLibrosAsync(
                libros.Select(libro => libro.Id).ToArray(),
                cancellationToken);
            var porLibro = inventarios.ToDictionary(i => i.LibroId);

            var resultados = libros.Select(libro =>
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
