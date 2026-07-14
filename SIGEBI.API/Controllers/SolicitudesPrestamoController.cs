using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.SolicitudesPrestamo;
using System.Threading.Tasks;

namespace SIGEBI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SolicitudesPrestamoController : ControllerBase
    {
        private readonly ISolicitudPrestamoService _solicitudService;

        public SolicitudesPrestamoController(ISolicitudPrestamoService solicitudService)
        {
            _solicitudService = solicitudService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _solicitudService.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) => Ok(await _solicitudService.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SaveSolicitudPrestamoDto dto)
        {
            await _solicitudService.RegistrarSolicitudAsync(dto);
            return StatusCode(StatusCodes.Status201Created);
        }

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
            await _solicitudService.DeleteAsync(id);
            return NoContent();
        }
    }
}
