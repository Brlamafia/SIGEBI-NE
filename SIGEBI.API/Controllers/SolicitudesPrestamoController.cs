using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.SolicitudesPrestamo;
using SIGEBI.Application.Interfaces.Seguridad;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace SIGEBI.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SolicitudesPrestamoController : ControllerBase
    {
        private readonly ISolicitudPrestamoService _solicitudService;
        private readonly IUsuarioActual _usuarioActual;

        public SolicitudesPrestamoController(
            ISolicitudPrestamoService solicitudService,
            IUsuarioActual usuarioActual)
        {
            _solicitudService = solicitudService;
            _usuarioActual = usuarioActual;
        }

        [Authorize(Roles = "Administrador,Bibliotecario")]
        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _solicitudService.GetAllAsync());

        [Authorize(Roles = "Administrador,Bibliotecario")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) => Ok(await _solicitudService.GetByIdAsync(id));

        [HttpGet("mias")]
        public async Task<IActionResult> GetMias() =>
            Ok(await _solicitudService.ObtenerPorUsuarioAsync(_usuarioActual.UsuarioId));

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SaveSolicitudPrestamoDto dto)
        {
            dto.UsuarioId = _usuarioActual.UsuarioId;
            await _solicitudService.RegistrarSolicitudAsync(dto);
            return StatusCode(StatusCodes.Status201Created);
        }

        [Authorize(Roles = "Administrador,Bibliotecario")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UpdateSolicitudPrestamoDto dto)
        {
            dto.Id = id;
            await _solicitudService.EvaluarSolicitudAsync(dto);
            return NoContent();
        }

        [Authorize(Roles = "Administrador,Bibliotecario")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _solicitudService.DeleteAsync(id);
            return NoContent();
        }
    }
}
