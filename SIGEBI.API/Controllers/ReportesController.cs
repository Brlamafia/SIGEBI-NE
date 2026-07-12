using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace SIGEBI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportesController : ControllerBase
    {
        // GET: api/Reportes/prestamos
        [HttpGet("prestamos")]
        public async Task<IActionResult> GetReportePrestamos([FromQuery] string fechaInicio, [FromQuery] string fechaFin)
        {
            // Lógica para devolver historial de préstamos en ese rango
            return Ok(new { Mensaje = "Reporte de préstamos generado" });
        }

        // GET: api/Reportes/inventario
        [HttpGet("inventario")]
        public async Task<IActionResult> GetReporteInventario()
        {
            // Lógica para devolver estado actual del catálogo (Disponibles vs Prestados)
            return Ok(new { Mensaje = "Reporte de inventario generado" });
        }

        // GET: api/Reportes/multas
        [HttpGet("multas")]
        public async Task<IActionResult> GetReporteMultas([FromQuery] string estado)
        {
            // Lógica para devolver multas (Pendientes, Pagadas)
            return Ok(new { Mensaje = "Reporte de multas generado" });
        }
    }
}