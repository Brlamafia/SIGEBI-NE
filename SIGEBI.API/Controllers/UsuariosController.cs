using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Usuarios;
using SIGEBI.Application.Interfaces.Usuarios;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.Notificaciones;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Dtos.Auth;
using System.Threading.Tasks;

namespace SIGEBI.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IPrestamoService _prestamos;
        private readonly IMultaService _multas;
        private readonly INotificacionService _notificaciones;
        private readonly IUsuarioActual _usuarioActual;

        public UsuariosController(
            IUsuarioService usuarioService,
            IPrestamoService prestamos,
            IMultaService multas,
            INotificacionService notificaciones,
            IUsuarioActual usuarioActual)
        {
            _usuarioService = usuarioService;
            _prestamos = prestamos;
            _multas = multas;
            _notificaciones = notificaciones;
            _usuarioActual = usuarioActual;
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var usuarios = await _usuarioService.GetAllAsync();
            return Ok(usuarios);
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var usuario = await _usuarioService.GetByIdAsync(id);
            return Ok(usuario);
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet("{id}/detalles")]
        public async Task<IActionResult> GetDetallesUsuario(int id)
        {
            return Ok(await _usuarioService.ConsultarHistorialCompletoAsync(id));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        public async Task<IActionResult> Post(
            [FromBody] SaveUsuarioDto dto,
            CancellationToken cancellationToken)
        {
            var usuario = await _usuarioService.CrearAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, usuario);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(
            int id,
            [FromBody] UpdateUsuarioDto dto,
            CancellationToken cancellationToken)
        {
            return Ok(await _usuarioService.ActualizarAsync(id, dto, cancellationToken));
        }

        [Authorize(Roles = "Administrador")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            await _usuarioService.EliminarAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpGet("me")]
        public Task<UsuarioDto> GetMe() =>
            _usuarioService.GetByIdAsync(_usuarioActual.UsuarioId);

        [HttpGet("me/resumen")]
        public async Task<IActionResult> GetMiResumen(CancellationToken cancellationToken)
        {
            var usuarioId = _usuarioActual.UsuarioId;
            return Ok(new
            {
                Usuario = await _usuarioService.GetByIdAsync(usuarioId),
                Prestamos = await _prestamos.ObtenerPorUsuarioAsync(usuarioId, cancellationToken),
                Multas = await _multas.ObtenerPorUsuarioAsync(usuarioId, cancellationToken),
                Notificaciones = await _notificaciones.ObtenerPorUsuarioAsync(usuarioId, cancellationToken)
            });
        }

        [HttpPut("me/password")]
        public async Task<IActionResult> CambiarMiPassword(
            [FromBody] CambiarPasswordDto dto,
            CancellationToken cancellationToken)
        {
            await _usuarioService.CambiarPasswordAsync(
                _usuarioActual.UsuarioId,
                dto.PasswordActual,
                dto.PasswordNueva,
                cancellationToken);
            return NoContent();
        }
    }
}
