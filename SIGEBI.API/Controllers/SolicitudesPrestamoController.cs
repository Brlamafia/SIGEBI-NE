using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _solicitudService.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            return Ok(await _solicitudService.GetByIdAsync(id));
        }
    }
}