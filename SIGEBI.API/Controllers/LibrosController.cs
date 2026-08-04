using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Catalogo;
using SIGEBI.Application.Interfaces.Catalogo;
using System.ComponentModel.DataAnnotations;
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
        public async Task<IActionResult> GetAll(
            [FromQuery, Range(1, 1_000_000)] int pagina = 1,
            [FromQuery, Range(1, 200)] int tamanoPagina = 50,
            CancellationToken cancellationToken = default)
        {
            var libros = await _libroService.BuscarLibrosAsync(
                skip: (pagina - 1) * tamanoPagina,
                take: tamanoPagina,
                cancellationToken: cancellationToken);
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
            [FromQuery, Range(1, 1_000_000)] int pagina = 1,
            [FromQuery, Range(1, 200)] int tamanoPagina = 50,
            [FromQuery, Range(0, 200_000_000)] int? skip = null,
            [FromQuery, Range(1, 200)] int? take = null,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _libroService.BuscarLibrosAsync(
                termino,
                genero,
                editorial,
                disponible,
                skip ?? (pagina - 1) * tamanoPagina,
                take ?? tamanoPagina,
                cancellationToken));
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
