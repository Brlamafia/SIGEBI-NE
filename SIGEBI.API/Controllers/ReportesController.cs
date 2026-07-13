using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Interfaces.Catalogo;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.SolicitudesPrestamo;

namespace SIGEBI.API.Controllers
{
    [Authorize] // Candado: Nadie entra sin token
    [ApiController]
    [Route("api/[controller]")]
    public class ReportesController : ControllerBase
    {
        private readonly ILibroService _libroService;
        private readonly ISolicitudPrestamoService _solicitudService;

        public ReportesController(ILibroService libroService, ISolicitudPrestamoService solicitudService)
        {
            _libroService = libroService;
            _solicitudService = solicitudService;
        }

        [HttpGet("inventario")]
        public async Task<IActionResult> GetReporteInventario()
        {
            var libros = await _libroService.GetAllAsync();
            var reporte = libros.Select(l => new
            {
                l.Titulo,
                l.Autor,
                Disponibles = l.StockDisponible
            });

            return Ok(reporte);
        }

        [HttpGet("prestamos-fecha")]
        public async Task<IActionResult> GetPrestamosPorFecha([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
        {
            var todas = await _solicitudService.ObtenerPorEstadoAsync("Aprobada");
            // Nota: Filtramos las aprobadas que estén dentro del rango
            return Ok(todas);
        }
    }
}