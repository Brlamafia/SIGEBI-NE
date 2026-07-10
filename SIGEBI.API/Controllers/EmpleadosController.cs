using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Empleados;
using SIGEBI.Application.Interfaces.Empleados;
using System.Threading.Tasks;

namespace SIGEBI.API.Controllers
{
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
            await _empleadoService.AddAsync(dto);
            return StatusCode(201);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UpdateEmpleadoDto dto)
        {
            await _empleadoService.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _empleadoService.DeleteAsync(id);
            return NoContent();
        }
    }
}