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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIGEBI.Application.Services.Catalogo
{
    public class LibroService : BaseService<Libro, LibroDto>, ILibroService
    {
        private readonly ILibroRepository _libroRepository;
        private readonly IPrestamoRepository _prestamos;
        private readonly ISolicitudPrestamoRepository _solicitudes;
        private readonly IInventarioRepository _inventarios;
        private readonly IEjemplarRepository _ejemplares;
        private readonly IInventarioService _inventarioService;
        private readonly IAuditoriaWriter _auditoria;
        private readonly IUsuarioActual _usuarioActual;
        private readonly IUnitOfWork _unitOfWork;

        // Inyectamos el repositorio de solicitudes para poder verificar si el libro está prestado
        public LibroService(
            ILibroRepository repository,
            IPrestamoRepository prestamos,
            ISolicitudPrestamoRepository solicitudes,
            IInventarioRepository inventarios,
            IEjemplarRepository ejemplares,
            IInventarioService inventarioService,
            IAuditoriaWriter auditoria,
            IUsuarioActual usuarioActual,
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : base(repository, mapper)
        {
            _libroRepository = repository;
            _prestamos = prestamos;
            _solicitudes = solicitudes;
            _inventarios = inventarios;
            _ejemplares = ejemplares;
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
                // Se guarda primero para que PostgreSQL asigne el ID del libro antes
                // de registrar su inventario inicial dentro de la misma transacción.
                await _unitOfWork.GuardarCambiosAsync(cancellationToken);
                var libroCreado = await _libroRepository.ObtenerPorIsbnAsync(
                    datos.ISBN,
                    cancellationToken)
                    ?? throw new InvalidOperationException(
                        "El libro recién registrado no pudo recuperarse.");
                creado = _mapper.Map<LibroDto>(libroCreado);
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
            return await GetByIdAsync(creado.Id);
        }

        public override async Task<LibroDto> GetByIdAsync(int id)
        {
            var libro = await _libroRepository.ObtenerPorIdAsync(id)
                ?? throw new NotFoundException(nameof(Libro), id);
            var inventario = (await _inventarioService.ObtenerPorLibrosAsync([id]))
                .SingleOrDefault(item => item.LibroId == id);

            return CrearDtoConInventario(libro, inventario);
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

        // Regla de negocio: solo se permite borrar libros sin historial asociado.
        public override async Task DeleteAsync(int id)
        {
            var libro = await _libroRepository.GetByIdAsync(id)
                ?? throw new NotFoundException(nameof(Libro), id);
            var prestamos = await _prestamos.ObtenerPorLibroAsync(id);
            var solicitudes = await _solicitudes.ObtenerPorLibroAsync(id);

            if (prestamos.Any() || solicitudes.Any())
            {
                throw new BusinessRuleException(
                    "No se puede eliminar el libro porque posee préstamos o solicitudes asociados.");
            }

            await _unitOfWork.EjecutarEnTransaccionAsync(async cancellationToken =>
            {
                var ejemplares = await _ejemplares.ObtenerPorLibroAsync(id, cancellationToken);
                _ejemplares.EliminarRango(ejemplares);

                var inventario = await _inventarios.ObtenerPorLibroIdAsync(id, cancellationToken);
                if (inventario is not null)
                    await _inventarios.EliminarAsync(inventario, cancellationToken);

                await _libroRepository.EliminarAsync(libro, cancellationToken);
                await _auditoria.RegistrarAsync(
                    _usuarioActual.UsuarioId,
                    ModuloAuditoria.Catalogo,
                    AccionAuditoria.Eliminar,
                    $"Libro {id} eliminado del catálogo.",
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
                porLibro.TryGetValue(libro.Id, out var inventario);
                return CrearDtoConInventario(libro, inventario);
            });
            if (disponible.HasValue)
                resultados = resultados.Where(l => l.Disponible == disponible.Value);
            return resultados.OrderBy(l => l.Titulo).ToArray();
        }

        private LibroDto CrearDtoConInventario(
            Libro libro,
            InventarioDto? inventario)
        {
            var result = _mapper.Map<LibroDto>(libro);
            result.Descripcion = string.IsNullOrWhiteSpace(libro.Descripcion)
                ? ObtenerDescripcion(libro)
                : libro.Descripcion;
            if (inventario is not null)
            {
                result.CantidadTotal = inventario.CantidadTotal;
                result.CantidadDisponible = inventario.CantidadDisponible;
                result.CantidadPrestada = inventario.CantidadPrestada;
            }

            // La disponibilidad que ve el personal se obtiene del inventario actual.
            // "Activo" es un valor heredado de datos anteriores y no describe el
            // estado operativo de un libro; solo se conserva "Descatalogado" como
            // estado administrativo explícito.
            if (!string.Equals(result.Estado, "Descatalogado", StringComparison.OrdinalIgnoreCase))
                result.Estado = result.CantidadDisponible > 0
                    ? "Disponible"
                    : "Sin disponibilidad";

            return result;
        }

        private static string ObtenerDescripcion(Libro libro) =>
            NormalizarTitulo(libro.Titulo) switch
            {
                "cien anos de soledad" =>
                    "Relata la historia de la familia Buendía a través de varias generaciones en Macondo, explorando la soledad, el destino, el amor y la memoria.",
                "el principito" =>
                    "Un aviador conoce a un pequeño príncipe procedente de otro planeta y descubre, mediante sus viajes, el valor de la amistad, el amor y aquello que realmente importa.",
                "don quijote de la mancha" =>
                    "Narra las aventuras de Alonso Quijano, quien decide convertirse en caballero andante junto a Sancho Panza, mezclando imaginación, humor y crítica social.",
                "1984" =>
                    "Presenta una sociedad totalitaria vigilada por el Gran Hermano, donde Winston Smith intenta conservar su libertad de pensamiento frente al control absoluto del Estado.",
                "la sombra del viento" =>
                    "Daniel Sempere descubre un libro olvidado que lo conduce a investigar la vida de su autor y a desentrañar un misterio entre las calles de la Barcelona de posguerra.",
                "breve historia del tiempo" =>
                    "Explica de forma accesible conceptos como el origen del universo, los agujeros negros, el espacio, el tiempo y las principales preguntas de la cosmología moderna.",
                "rayuela" =>
                    "Sigue la búsqueda personal y amorosa de Horacio Oliveira entre París y Buenos Aires mediante una estructura experimental que permite distintas formas de lectura.",
                "fahrenheit 451" =>
                    "En una sociedad donde los libros están prohibidos, el bombero Guy Montag comienza a cuestionar su misión y descubre el poder transformador del conocimiento.",
                "orgullo y prejuicio" =>
                    "Explora la relación entre Elizabeth Bennet y el señor Darcy, marcada por primeras impresiones, diferencias sociales, orgullo y aprendizaje personal.",
                "cronica de una muerte anunciada" =>
                    "Reconstruye las horas previas al asesinato de Santiago Nasar, un crimen conocido por todo el pueblo que nadie consigue impedir.",
                _ =>
                    $"{libro.Titulo}, de {libro.Autor}, es una obra de {ObtenerGenero(libro)} que invita a conocer su historia, personajes y temas principales."
            };

        private static string ObtenerGenero(Libro libro) =>
            string.IsNullOrWhiteSpace(libro.Genero)
                ? "interés general"
                : libro.Genero.ToLowerInvariant();

        private static string NormalizarTitulo(string titulo)
        {
            var normalized = titulo.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) !=
                    UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC).Trim();
        }
    }
}
