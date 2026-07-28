using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Interfaces.Inventario;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.Catalogo;
using SIGEBI.Application.Dtos.Reportes;

namespace SIGEBI.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReportesController : ControllerBase
    {
        private readonly IInventarioService _inventarioService;
        private readonly IPrestamoService _prestamoService;
        private readonly ILibroService _libroService;
        private readonly IMultaService _multaService;

        public ReportesController(
            IInventarioService inventarioService,
            IPrestamoService prestamoService,
            ILibroService libroService,
            IMultaService multaService)
        {
            _inventarioService = inventarioService;
            _prestamoService = prestamoService;
            _libroService = libroService;
            _multaService = multaService;
        }

        [Authorize(Roles = "Administrador,Auditor,Bibliotecario")]
        [HttpGet("inventario")]
        public async Task<IActionResult> GetReporteInventario()
        {
            return Ok(await _inventarioService.ObtenerTodosAsync(HttpContext.RequestAborted));
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
            var totalCopias = libros.Values.Sum(l => l.CantidadTotal);
            var disponibles = libros.Values.Sum(l => l.CantidadDisponible);
            var recursos = prestamos.GroupBy(p => p.LibroId)
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
                .Take(10)
                .ToArray();
            return Ok(new ReporteCatalogoDto
            {
                Desde = desde,
                Hasta = hasta,
                DisponibilidadPromedioPorcentaje = totalCopias == 0
                    ? 0
                    : Math.Round(disponibles * 100m / totalCopias, 2),
                RecursosMasSolicitados = recursos,
                DemandaPorCategoria = recursos
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
            var estados = new[] { "Pendiente", "Pagada", "Resuelta" };
            var colecciones = await Task.WhenAll(estados.Select(
                estado => _multaService.ObtenerPorEstadoAsync(estado, cancellationToken)));
            var multas = colecciones.SelectMany(x => x)
                .Where(m => m.FechaGeneracion >= desde && m.FechaGeneracion <= hasta)
                .DistinctBy(m => m.Id)
                .ToArray();
            return Ok(new ReporteMultasDto
            {
                Desde = desde,
                Hasta = hasta,
                Generadas = multas.Length,
                Pendientes = multas.Count(m => m.Estado == "Pendiente"),
                Pagadas = multas.Count(m => m.Estado == "Pagada"),
                Resueltas = multas.Count(m => m.Estado == "Resuelta"),
                MontoTotal = multas.Sum(m => m.Monto)
            });
        }

        private static void ValidarRango(DateTime desde, DateTime hasta)
        {
            if (desde == default || hasta == default || desde > hasta)
                throw new ArgumentException("Debe especificar un rango de fechas válido.");
        }
    }
}
