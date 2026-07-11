using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Administradores;
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
        public async Task<IActionResult> GetAll() => Ok(await _administradorService.ObtenerTodosAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) => Ok(await _administradorService.ObtenerPorIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SaveAdministradorDto dto)
        {
            var administrador = await _administradorService.CrearAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = administrador.Id }, administrador);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UpdateAdministradorDto dto)
        {
            return Ok(await _administradorService.ActualizarAsync(id, dto));
        }
    }
}
