using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SIGEBI.Application.Interfaces.Usuarios;
using SIGEBI.Application.Security;
using SIGEBI.Domain.Interfaces.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace SIGEBI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IUsuarioService _usuarioService;
        private readonly IUsuarioRepository _usuarios;
        private readonly IAdministradorRepository _administradores;
        private readonly IEmpleadoRepository _empleados;

        public AuthController(
            IConfiguration config,
            IUsuarioService usuarioService,
            IUsuarioRepository usuarios,
            IAdministradorRepository administradores,
            IEmpleadoRepository empleados)
        {
            _config = config;
            _usuarioService = usuarioService;
            _usuarios = usuarios;
            _administradores = administradores;
            _empleados = empleados;
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var usuarioValido = await _usuarios.ObtenerPorEmailAsync(
                request.Email.Trim(),
                HttpContext.RequestAborted);
            if (usuarioValido == null ||
                usuarioValido.Estado != SIGEBI.Domain.Enums.EstadoUsuario.Activo ||
                !PasswordHasher.Verify(request.Password, usuarioValido.ContrasenaHash))
            {
                return Unauthorized("Credenciales inválidas.");
            }

            // Generación de Token JWT Real
            var jwtKey = _config["Jwt:Key"] ?? "EstaEsUnaClaveSuperSecretaDeMasDe32CaracteresParaElITLA";
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtKey);
            var role = await DeterminarRolAsync(usuarioValido, HttpContext.RequestAborted);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuarioValido.Id.ToString()),
                    new Claim(ClaimTypes.Email, usuarioValido.Email),
                    new Claim(ClaimTypes.Role, role)
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new
            {
                Token = tokenString,
                Usuario = await _usuarioService.GetByIdAsync(usuarioValido.Id),
                Rol = role
            });
        }

        private async Task<string> DeterminarRolAsync(
            SIGEBI.Domain.Entities.Usuarios.Usuario usuario,
            CancellationToken cancellationToken)
        {
            if (usuario.Roles.Any(r => r.Nombre == "Administrador") ||
                await _administradores.ObtenerPorUsuarioIdAsync(usuario.Id, cancellationToken) is not null)
                return "Administrador";
            if (usuario.Roles.Any(r => r.Nombre is "Bibliotecario" or "Empleado") ||
                await _empleados.ObtenerPorUsuarioIdAsync(usuario.Id, cancellationToken) is not null)
                return "Bibliotecario";
            if (usuario.Roles.Any(r => r.Nombre == "Auditor"))
                return "Auditor";
            return "Usuario";
        }
    }

    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "admin@sigebi.local";
        [Required, MinLength(8)]
        public string Password { get; set; } = "Admin123";
    }
}
