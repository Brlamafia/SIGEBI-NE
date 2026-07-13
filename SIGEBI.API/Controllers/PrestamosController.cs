using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.SolicitudesPrestamo;
using System.Threading.Tasks;

namespace SIGEBI.API.Controllers
{
    [Authorize] // Candado: Nadie entra sin el token JWT
    [ApiController]
    [Route("api/[controller]")]
    public class PrestamosController : ControllerBase
    {
        private readonly ISolicitudPrestamoService _solicitudService;
        private readonly IPrestamoService _prestamoService;

        // Inyectamos las interfaces que conectamos en el ApplicationDependency
        public PrestamosController(
            ISolicitudPrestamoService solicitudService,
            IPrestamoService prestamoService)
        {
            _solicitudService = solicitudService;
            _prestamoService = prestamoService;
        }

        // 1. Obtener el historial de préstamos de un usuario específico
        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> GetHistorialUsuario(int usuarioId)
        {
            var solicitudes = await _solicitudService.ObtenerPorUsuarioAsync(usuarioId);
            return Ok(solicitudes);
        }

        // 2. Registrar una nueva solicitud (El usuario pide un libro)
        [HttpPost("solicitar")]
        public async Task<IActionResult> SolicitarLibro([FromBody] SaveSolicitudPrestamoDto dto)
        {
            if (dto == null) return BadRequest("La solicitud no puede estar vacía.");

            // Llama a la lógica real que inyectamos hoy (que valida multas y stock)
            await _solicitudService.RegistrarSolicitudAsync(dto);

            return Ok(new { Mensaje = "Solicitud registrada con éxito. Pendiente de evaluación." });
        }

        // 3. Evaluar la solicitud (El bibliotecario aprueba o rechaza)
        [HttpPut("evaluar")]
        public async Task<IActionResult> EvaluarSolicitud([FromBody] UpdateSolicitudPrestamoDto dto)
        {
            if (dto == null) return BadRequest("Los datos de evaluación no pueden estar vacíos.");

            // Llama a la lógica que descuenta el inventario y manda la notificación
            await _solicitudService.EvaluarSolicitudAsync(dto);

            return Ok(new { Mensaje = "La solicitud ha sido evaluada correctamente." });
        }
    }
}