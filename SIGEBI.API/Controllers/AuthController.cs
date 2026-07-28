using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SIGEBI.Application.Interfaces.Usuarios;
using SIGEBI.Application.Security;
using SIGEBI.Domain.Interfaces.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;

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
        private readonly IAuditoriaWriter _auditoria;
        private readonly IUnitOfWork _unitOfWork;

        public AuthController(
            IConfiguration config,
            IUsuarioService usuarioService,
            IUsuarioRepository usuarios,
            IAdministradorRepository administradores,
            IEmpleadoRepository empleados,
            IAuditoriaWriter auditoria,
            IUnitOfWork unitOfWork)
        {
            _config = config;
            _usuarioService = usuarioService;
            _usuarios = usuarios;
            _administradores = administradores;
            _empleados = empleados;
            _auditoria = auditoria;
            _unitOfWork = unitOfWork;
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
                usuarioValido.Estado != EstadoUsuario.Activo)
            {
                return Unauthorized("Credenciales inválidas.");
            }

            if (usuarioValido.EstaBloqueado(DateTime.UtcNow))
                return Unauthorized("La cuenta está temporalmente bloqueada.");

            if (!PasswordHasher.Verify(request.Password, usuarioValido.ContrasenaHash))
            {
                usuarioValido.RegistrarIntentoFallido(
                    _config.GetValue("Authentication:MaxFailedAttempts", 5),
                    TimeSpan.FromMinutes(_config.GetValue("Authentication:LockoutMinutes", 15)));
                _usuarios.Actualizar(usuarioValido);
                await _auditoria.RegistrarAsync(
                    usuarioValido.Id,
                    ModuloAuditoria.Usuarios,
                    AccionAuditoria.ActualizarEstado,
                    "Intento de acceso fallido.",
                    ResultadoAuditoria.Fallido,
                    HttpContext.RequestAborted);
                await _unitOfWork.GuardarCambiosAsync(HttpContext.RequestAborted);
                return Unauthorized("Credenciales inválidas.");
            }

            usuarioValido.RegistrarAccesoExitoso();
            _usuarios.Actualizar(usuarioValido);
            await _auditoria.RegistrarAsync(
                usuarioValido.Id,
                ModuloAuditoria.Usuarios,
                AccionAuditoria.Registrar,
                "Inicio de sesión exitoso.",
                cancellationToken: HttpContext.RequestAborted);

            var jwtKey = _config["Jwt:Key"]
                ?? throw new InvalidOperationException("Debe configurar Jwt:Key.");
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtKey);
            var roles = await DeterminarRolesAsync(usuarioValido, HttpContext.RequestAborted);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuarioValido.Id.ToString()),
                new(ClaimTypes.Email, usuarioValido.Email)
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            claims.AddRange(usuarioValido.Roles
                .SelectMany(role => role.Permisos)
                .Select(permiso => permiso.Codigo)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(permiso => new Claim("permission", permiso)));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            await _unitOfWork.GuardarCambiosAsync(HttpContext.RequestAborted);

            return Ok(new
            {
                Token = tokenString,
                Usuario = await _usuarioService.GetByIdAsync(usuarioValido.Id),
                Roles = roles
            });
        }

        private async Task<IReadOnlyCollection<string>> DeterminarRolesAsync(
            SIGEBI.Domain.Entities.Usuarios.Usuario usuario,
            CancellationToken cancellationToken)
        {
            var roles = usuario.Roles.Select(r => r.Nombre)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (await _administradores.ObtenerPorUsuarioIdAsync(usuario.Id, cancellationToken) is not null)
                roles.Add("Administrador");
            if (await _empleados.ObtenerPorUsuarioIdAsync(usuario.Id, cancellationToken) is not null)
                roles.Add("Bibliotecario");
            if (roles.Count == 0)
                roles.Add("Usuario");
            return roles.OrderBy(role => role).ToArray();
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
