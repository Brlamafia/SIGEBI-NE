using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Application.Interfaces.Notificaciones;
using SIGEBI.Application.Interfaces.Seguridad;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace SIGEBI.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificacionesController : ControllerBase
    {
        private readonly INotificacionService _notificacionService;
        private readonly IUsuarioActual _usuarioActual;

        public NotificacionesController(
            INotificacionService notificacionService,
            IUsuarioActual usuarioActual)
        {
            _notificacionService = notificacionService;
            _usuarioActual = usuarioActual;
        }

        [Authorize(Roles = "Administrador,Auditor")]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery, Range(1, 1_000_000)] int pagina = 1,
            [FromQuery, Range(1, 200)] int tamanoPagina = 100,
            CancellationToken cancellationToken = default) =>
            Ok(await _notificacionService.GetPageAsync(pagina, tamanoPagina, cancellationToken));

        [Authorize(Roles = "Administrador,Auditor")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) => Ok(await _notificacionService.GetByIdAsync(id));

        [HttpGet("mias")]
        public async Task<IActionResult> GetMias(
            [FromQuery, Range(1, 1_000_000)] int pagina = 1,
            [FromQuery, Range(1, 200)] int tamanoPagina = 50,
            CancellationToken cancellationToken = default) =>
            Ok(await _notificacionService.ObtenerPorUsuarioAsync(
                _usuarioActual.UsuarioId,
                pagina,
                tamanoPagina,
                cancellationToken));

        [HttpPut("{id:int}/leer")]
        public async Task<IActionResult> MarcarComoLeida(
            int id,
            CancellationToken cancellationToken)
        {
            await _notificacionService.MarcarComoLeidaAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPut("leer-todas")]
        public async Task<IActionResult> MarcarTodasComoLeidas(
            CancellationToken cancellationToken)
        {
            var actualizadas = await _notificacionService.MarcarTodasComoLeidasAsync(cancellationToken);
            return Ok(new { actualizadas });
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SaveNotificacionDto dto)
        {
            await _notificacionService.AddAsync(dto);
            return StatusCode(201);
        }

    }
}
