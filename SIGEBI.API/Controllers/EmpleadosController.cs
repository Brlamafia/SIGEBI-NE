using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Empleados;
using SIGEBI.Application.Interfaces.Empleados;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace SIGEBI.API.Controllers
{
    [Authorize(Roles = "Administrador")]
    [Route("api/[controller]")]
    [ApiController]
    public class EmpleadosController : ControllerBase
    {
        private readonly IEmpleadoService _empleadoService;

        public EmpleadosController(IEmpleadoService empleadoService)
        {
            _empleadoService = empleadoService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _empleadoService.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) => Ok(await _empleadoService.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SaveEmpleadoDto dto)
        {
            return StatusCode(201, await _empleadoService.CrearAsync(dto, HttpContext.RequestAborted));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UpdateEmpleadoDto dto)
        {
            return Ok(await _empleadoService.ActualizarAsync(id, dto, HttpContext.RequestAborted));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _empleadoService.DeleteAsync(id);
            return NoContent();
        }
    }
}
