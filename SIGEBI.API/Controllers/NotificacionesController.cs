using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Application.Interfaces.Notificaciones;
using System.Threading.Tasks;

namespace SIGEBI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificacionesController : ControllerBase
    {
        private readonly INotificacionService _notificacionService;

        public NotificacionesController(INotificacionService notificacionService)
        {
            _notificacionService = notificacionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _notificacionService.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) => Ok(await _notificacionService.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SaveNotificacionDto dto)
        {
            await _notificacionService.AddAsync(dto);
            return StatusCode(201);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _notificacionService.DeleteAsync(id);
            return NoContent();
        }
    }
}