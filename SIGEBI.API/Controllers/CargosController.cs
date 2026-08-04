using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Cargos; // Asegúrate de tener este using
using SIGEBI.Application.Interfaces.Cargos;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace SIGEBI.API.Controllers
{
    [Authorize(Roles = "Administrador")]
    [Route("api/[controller]")]
    [ApiController]
    public class CargosController : ControllerBase
    {
        private readonly ICargoService _cargoService;

        public CargosController(ICargoService cargoService)
        {
            _cargoService = cargoService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery, Range(1, 1_000_000)] int pagina = 1,
            [FromQuery, Range(1, 200)] int tamanoPagina = 100,
            CancellationToken cancellationToken = default) =>
            Ok(await _cargoService.GetPageAsync(pagina, tamanoPagina, cancellationToken));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) => Ok(await _cargoService.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SaveCargoDto dto)
        {
            await _cargoService.AddAsync(dto);
            return StatusCode(201);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UpdateCargoDto dto)
        {
            await _cargoService.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _cargoService.DeleteAsync(id);
            return NoContent();
        }
    }
}
