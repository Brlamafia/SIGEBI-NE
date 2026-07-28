using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Roles;
using SIGEBI.Application.Interfaces.Roles;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace SIGEBI.API.Controllers
{
    [Authorize(Policy = "AdministracionCompleta")]
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IRolService _rolService;

        public RolesController(IRolService rolService)
        {
            _rolService = rolService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _rolService.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) => Ok(await _rolService.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SaveRolDto dto)
        {
            await _rolService.AddAsync(dto);
            return StatusCode(201);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UpdateRolDto dto)
        {
            await _rolService.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _rolService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("asignar")]
        public async Task<IActionResult> AsignarRol([FromBody] AsignarRolDto dto)
        {
            await _rolService.AsignarAUsuarioAsync(dto, HttpContext.RequestAborted);
            return NoContent();
        }

        [HttpDelete("asignar")]
        public async Task<IActionResult> RemoverRol([FromBody] AsignarRolDto dto)
        {
            await _rolService.RemoverDeUsuarioAsync(dto, HttpContext.RequestAborted);
            return NoContent();
        }

        [HttpPost("permisos")]
        public async Task<IActionResult> CrearPermiso([FromBody] SavePermisoDto dto) =>
            StatusCode(StatusCodes.Status201Created,
                await _rolService.CrearPermisoAsync(dto, HttpContext.RequestAborted));

        [HttpPost("permisos/asignar")]
        public async Task<IActionResult> AsignarPermiso([FromBody] AsignarPermisoDto dto)
        {
            await _rolService.AsignarPermisoAsync(dto, HttpContext.RequestAborted);
            return NoContent();
        }

        [HttpDelete("permisos/asignar")]
        public async Task<IActionResult> RemoverPermiso([FromBody] AsignarPermisoDto dto)
        {
            await _rolService.RemoverPermisoAsync(dto, HttpContext.RequestAborted);
            return NoContent();
        }
    }
}
