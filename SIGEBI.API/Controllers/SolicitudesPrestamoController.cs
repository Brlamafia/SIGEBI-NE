using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.SolicitudesPrestamo;
using SIGEBI.Application.Interfaces.Seguridad;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

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
        public async Task<IActionResult> GetAll(
            [FromQuery, Range(1, 1_000_000)] int pagina = 1,
            [FromQuery, Range(1, 200)] int tamanoPagina = 100,
            CancellationToken cancellationToken = default) =>
            Ok(await _solicitudService.GetPageAsync(pagina, tamanoPagina, cancellationToken));

        [Authorize(Roles = "Administrador,Bibliotecario")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) => Ok(await _solicitudService.GetByIdAsync(id));

        [Authorize(Roles = "Administrador,Bibliotecario")]
        [HttpGet("estado/{estado}")]
        public async Task<IActionResult> GetByEstado(
            string estado,
            [FromQuery, Range(1, 1_000_000)] int pagina = 1,
            [FromQuery, Range(1, 200)] int tamanoPagina = 100)
        {
            var solicitudes = await _solicitudService.ObtenerPorEstadoAsync(estado);

            // Se mantienen los parámetros opcionales para no afectar a los consumidores
            // existentes y permitir que la pantalla pagine sin abandonar el filtro actual.
            if (pagina == 1 && tamanoPagina == 100)
                return Ok(solicitudes);

            return Ok(solicitudes
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina));
        }

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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _solicitudService.CancelarAsync(
                id,
                _usuarioActual.UsuarioId,
                HttpContext.RequestAborted);
            return NoContent();
        }
    }
}
