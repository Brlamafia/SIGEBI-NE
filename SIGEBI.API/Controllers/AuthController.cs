using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Seguridad;

namespace SIGEBI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    IConfiguration configuration,
    IAuthenticationService authentication) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        SIGEBI.Application.Dtos.Auth.AuthenticatedUserDto authenticated;
        try
        {
            authenticated = await authentication.AuthenticateAsync(
                request.Email,
                request.Password,
                HttpContext.RequestAborted);
        }
        catch (AuthenticationException exception)
        {
            return Unauthorized(exception.Message);
        }

        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Debe configurar Jwt:Key.");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, authenticated.Usuario.Id.ToString()),
            new(ClaimTypes.Email, authenticated.Usuario.Email)
        };
        claims.AddRange(authenticated.Roles.Select(
            role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(authenticated.Permisos.Select(
            permission => new Claim("permission", permission)));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

        return Ok(new
        {
            Token = token,
            authenticated.Usuario,
            authenticated.Roles
        });
    }
}

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "admin@sigebi.local";

    [Required, MinLength(8)]
    public string Password { get; set; } = "Admin123";
}
