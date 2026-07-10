using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Interfaces.Administradores;
using System.Threading.Tasks;

namespace SIGEBI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdministradoresController : ControllerBase
    {
        private readonly IAdministradorService _administradorService;

        public AdministradoresController(IAdministradorService administradorService)
        {
            _administradorService = administradorService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _administradorService.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            return Ok(await _administradorService.GetByIdAsync(id));
        }
    }
}