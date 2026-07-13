using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SIGEBI.Application.Interfaces.Usuarios;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SIGEBI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IUsuarioService _usuarioService;

        public AuthController(IConfiguration config, IUsuarioService usuarioService)
        {
            _config = config;
            _usuarioService = usuarioService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Nota académica: En producción usarías hashing de contraseñas (ej. BCrypt)
            // Aquí validamos usando el servicio de usuarios
            var usuarios = await _usuarioService.GetAllAsync();
            var usuarioValido = usuarios.FirstOrDefault(u => u.Email == request.Email);

            if (usuarioValido == null || request.Password != "Admin123") // Clave de acceso simulada
            {
                return Unauthorized("Credenciales inválidas.");
            }

            // Generación de Token JWT Real
            var jwtKey = _config["Jwt:Key"] ?? "EstaEsUnaClaveSuperSecretaDeMasDe32CaracteresParaElITLA";
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuarioValido.Id.ToString()),
                    new Claim(ClaimTypes.Email, usuarioValido.Email),
                    new Claim(ClaimTypes.Role, "Administrador") // Rol por defecto
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new { Token = tokenString, Usuario = usuarioValido });
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}