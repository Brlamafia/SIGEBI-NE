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
        public async Task<IActionResult> GetAll() => Ok(await _administradorService.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) => Ok(await _administradorService.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SaveAdministradorDto dto)
        {
            await _administradorService.AddAsync(dto);
            return StatusCode(201);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UpdateAdministradorDto dto)
        {
            await _administradorService.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _administradorService.DeleteAsync(id);
            return NoContent();
        }
    }
}