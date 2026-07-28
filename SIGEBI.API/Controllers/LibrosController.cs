using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Catalogo;
using SIGEBI.Application.Interfaces.Catalogo;
using System.Threading.Tasks;

namespace SIGEBI.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LibrosController : ControllerBase
    {
        private readonly ILibroService _libroService;

        public LibrosController(ILibroService libroService)
        {
            _libroService = libroService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var libros = await _libroService.BuscarLibrosAsync(cancellationToken: cancellationToken);
            return Ok(libros);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var libro = await _libroService.GetByIdAsync(id);
            return Ok(libro);
        }

        [HttpGet("buscar")]
        public async Task<IActionResult> BuscarLibros(
            [FromQuery] string? termino,
            [FromQuery] string? genero,
            [FromQuery] string? editorial,
            [FromQuery] bool? disponible,
            CancellationToken cancellationToken)
        {
            return Ok(await _libroService.BuscarLibrosAsync(
                termino, genero, editorial, disponible, cancellationToken));
        }

        [Authorize(Roles = "Administrador,Bibliotecario")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SaveLibroDto dto)
        {
            var result = await _libroService.AddAsync(dto);
            return StatusCode(201, result);
        }

        [Authorize(Roles = "Administrador,Bibliotecario")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UpdateLibroDto dto)
        {
            await _libroService.UpdateAsync(id, dto);
            return NoContent();
        }

        [Authorize(Roles = "Administrador,Bibliotecario")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _libroService.DeleteAsync(id);
            return NoContent();
        }
    }
}
