using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Auth;
using System.Threading.Tasks;

namespace SIGEBI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        // Nota: Aquí inyectarás tu IAuthService más adelante

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // Aquí irá la lógica de:
            // 1. Buscar usuario por Email
            // 2. Verificar Hash de contraseña
            // 3. Generar JWT

            // Retorno temporal simulado para que compile
            return Ok(new { Token = "jwt_token_generado_aqui", Mensaje = "Login exitoso" });
        }
    }
}