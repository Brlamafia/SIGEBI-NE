using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Application.Interfaces.Notificaciones;
using SIGEBI.Application.Interfaces.Seguridad;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<IActionResult> GetAll() => Ok(await _notificacionService.GetAllAsync());

        [Authorize(Roles = "Administrador,Auditor")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) => Ok(await _notificacionService.GetByIdAsync(id));

        [HttpGet("mias")]
        public async Task<IActionResult> GetMias(CancellationToken cancellationToken) =>
            Ok(await _notificacionService.ObtenerPorUsuarioAsync(
                _usuarioActual.UsuarioId,
                cancellationToken));

        [HttpPut("{id:int}/leer")]
        public async Task<IActionResult> MarcarComoLeida(
            int id,
            CancellationToken cancellationToken)
        {
            var propias = await _notificacionService.ObtenerPorUsuarioAsync(
                _usuarioActual.UsuarioId,
                cancellationToken);
            if (!propias.Any(notificacion => notificacion.Id == id) &&
                _usuarioActual.Rol is not ("Administrador" or "Auditor"))
                return Forbid();

            await _notificacionService.MarcarComoLeidaAsync(id, cancellationToken);
            return NoContent();
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SaveNotificacionDto dto)
        {
            await _notificacionService.AddAsync(dto);
            return StatusCode(201);
        }

        [Authorize(Roles = "Administrador")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _notificacionService.DeleteAsync(id);
            return NoContent();
        }
    }
}
