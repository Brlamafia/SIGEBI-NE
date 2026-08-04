using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.Catalogo;
using SIGEBI.Application.Dtos.Reportes;
using SIGEBI.Application.Interfaces.Usuarios;

namespace SIGEBI.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReportesController : ControllerBase
    {
        private readonly IPrestamoService _prestamoService;
        private readonly ILibroService _libroService;
        private readonly IMultaService _multaService;
        private readonly IUsuarioService _usuarioService;

        public ReportesController(
            IPrestamoService prestamoService,
            ILibroService libroService,
            IMultaService multaService,
            IUsuarioService usuarioService)
        {
            _prestamoService = prestamoService;
            _libroService = libroService;
            _multaService = multaService;
            _usuarioService = usuarioService;
        }

        [Authorize(Roles = "Administrador,Auditor,Bibliotecario")]
        [HttpGet("inventario")]
        public async Task<IActionResult> GetReporteInventario()
        {
            var libros = await _libroService.BuscarLibrosAsync(
                cancellationToken: HttpContext.RequestAborted);
            return Ok(libros.Select(libro => new InventarioReporteDto(
                    libro.Id,
                    libro.Titulo,
                    libro.Genero ?? "Sin categoría",
                    libro.CantidadTotal,
                    libro.CantidadDisponible,
                    libro.CantidadPrestada))
                .OrderBy(item => item.Categoria)
                .ThenBy(item => item.Titulo));
        }

        [Authorize(Roles = "Administrador,Auditor")]
        [HttpGet("catalogo")]
        public async Task<IActionResult> GetReporteCatalogo(
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta,
            CancellationToken cancellationToken)
        {
            ValidarRango(desde, hasta);
            var libros = (await _libroService.BuscarLibrosAsync(
                cancellationToken: cancellationToken)).ToDictionary(l => l.Id);
            var prestamos = await _prestamoService.ObtenerPorRangoAsync(
                desde, hasta, cancellationToken);
            var disponibilidadPromedio = libros.Count == 0
                ? 0
                : libros.Values.Average(libro => libro.CantidadTotal == 0
                    ? 0
                    : libro.CantidadDisponible * 100m / libro.CantidadTotal);
            var todosRecursos = prestamos.GroupBy(p => p.LibroId)
                .Select(g =>
                {
                    libros.TryGetValue(g.Key, out var libro);
                    return new RecursoSolicitadoDto(
                        g.Key,
                        libro?.Titulo ?? $"Libro {g.Key}",
                        libro?.Genero ?? "Sin categoría",
                        g.Count());
                })
                .OrderByDescending(r => r.Solicitudes)
                .ThenBy(r => r.Titulo)
                .ToArray();
            return Ok(new ReporteCatalogoDto
            {
                Desde = desde,
                Hasta = hasta,
                DisponibilidadPromedioPorcentaje = Math.Round(disponibilidadPromedio, 2),
                RecursosMasSolicitados = todosRecursos.Take(10).ToArray(),
                DemandaPorCategoria = todosRecursos
                    .GroupBy(r => r.Genero)
                    .Select(g => new DemandaCategoriaDto(g.Key, g.Sum(x => x.Solicitudes)))
                    .OrderByDescending(x => x.Prestamos)
                    .ToArray()
            });
        }

        [Authorize(Roles = "Administrador,Auditor")]
        [HttpGet("prestamos-fecha")]
        public async Task<IActionResult> GetPrestamosPorFecha([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
        {
            ValidarRango(desde, hasta);
            var prestamos = await _prestamoService.ObtenerPorRangoAsync(
                desde,
                hasta,
                HttpContext.RequestAborted);
            var devueltos = prestamos.Where(p => p.FechaRealDevolucion.HasValue).ToArray();
            var puntuales = devueltos.Count(p =>
                p.FechaRealDevolucion!.Value <= p.FechaEsperadaDevolucion);
            return Ok(new ReportePrestamosDto
            {
                Desde = desde,
                Hasta = hasta,
                TotalPrestamos = prestamos.Count,
                DevolucionesPuntuales = puntuales,
                PrestamosVencidos = prestamos.Count(p =>
                    p.Estado.Equals("Vencido", StringComparison.OrdinalIgnoreCase) ||
                    (!p.FechaRealDevolucion.HasValue && p.FechaEsperadaDevolucion < DateTime.UtcNow)),
                TasaDevolucionPuntualPorcentaje = devueltos.Length == 0
                    ? 0
                    : Math.Round(puntuales * 100m / devueltos.Length, 2)
            });
        }

        [Authorize(Roles = "Administrador,Auditor")]
        [HttpGet("multas")]
        public async Task<IActionResult> GetReporteMultas(
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta,
            CancellationToken cancellationToken)
        {
            ValidarRango(desde, hasta);
            var multas = (await _multaService.ObtenerPorRangoAsync(
                    desde,
                    hasta,
                    cancellationToken))
                .ToArray();
            var usuarios = (await _usuarioService.ObtenerPorIdsAsync(
                    multas.Select(multa => multa.UsuarioId).Distinct().ToArray(),
                    cancellationToken))
                .ToDictionary(usuario => usuario.Id);
            return Ok(new ReporteMultasDto
            {
                Desde = desde,
                Hasta = hasta,
                Generadas = multas.Length,
                Pendientes = multas.Count(m => m.Estado == "Pendiente"),
                Pagadas = multas.Count(m => m.Estado == "Pagada"),
                Resueltas = multas.Count(m => m.Estado == "Resuelta"),
                MontoTotal = multas.Sum(m => m.Monto),
                PorTipoUsuario = multas
                    .GroupBy(m => usuarios.TryGetValue(m.UsuarioId, out var usuario)
                        ? usuario.TipoUsuario
                        : "Desconocido")
                    .Select(g => new MultasPorTipoUsuarioDto(
                        g.Key,
                        g.Count(),
                        g.Sum(m => m.Monto)))
                    .OrderByDescending(g => g.Monto)
                    .ToArray()
            });
        }

        private static void ValidarRango(DateTime desde, DateTime hasta)
        {
            if (desde == default || hasta == default || desde > hasta)
                throw new ArgumentException("Debe especificar un rango de fechas válido.");
        }
    }
}
