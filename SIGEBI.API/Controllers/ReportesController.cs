using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Interfaces.Inventario;
using SIGEBI.Application.Interfaces.Prestamos;

namespace SIGEBI.API.Controllers
{
    [Authorize] // Candado: Nadie entra sin token
    [ApiController]
    [Route("api/[controller]")]
    public class ReportesController : ControllerBase
    {
        private readonly IInventarioService _inventarioService;
        private readonly IPrestamoService _prestamoService;

        public ReportesController(
            IInventarioService inventarioService,
            IPrestamoService prestamoService)
        {
            _inventarioService = inventarioService;
            _prestamoService = prestamoService;
        }

        [HttpGet("inventario")]
        public async Task<IActionResult> GetReporteInventario()
        {
            return Ok(await _inventarioService.ObtenerTodosAsync(HttpContext.RequestAborted));
        }

        [HttpGet("prestamos-fecha")]
        public async Task<IActionResult> GetPrestamosPorFecha([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
        {
            return Ok(await _prestamoService.ObtenerPorRangoAsync(
                desde,
                hasta,
                HttpContext.RequestAborted));
        }
    }
}
