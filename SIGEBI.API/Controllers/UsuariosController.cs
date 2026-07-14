using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Usuarios;
using SIGEBI.Application.Interfaces.Usuarios;
using System.Threading.Tasks;

namespace SIGEBI.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public UsuariosController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var usuarios = await _usuarioService.GetAllAsync();
            return Ok(usuarios);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var usuario = await _usuarioService.GetByIdAsync(id);
            return Ok(usuario);
        }

        [HttpGet("{id}/detalles")]
        public async Task<IActionResult> GetDetallesUsuario(int id)
        {
            return Ok(await _usuarioService.ConsultarHistorialCompletoAsync(id));
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SaveUsuarioDto dto)
        {
            var result = await _usuarioService.AddAsync(dto);
            return StatusCode(201, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UpdateUsuarioDto dto)
        {
            await _usuarioService.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _usuarioService.DeleteAsync(id);
            return NoContent();
        }
    }
}
