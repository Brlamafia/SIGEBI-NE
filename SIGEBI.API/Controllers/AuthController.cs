using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using SIGEBI.Application.Dtos.Auth;
using SIGEBI.Application.Dtos.Usuarios;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Interfaces.Usuarios;
using SIGEBI.Domain.Enums;
using ApplicationAuthenticationException = SIGEBI.Application.Exceptions.AuthenticationException;

namespace SIGEBI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public sealed class AuthController(
    IConfiguration configuration,
    IAuthenticationService authentication,
    IUsuarioService users,
    IPasswordRecoveryService passwordRecovery,
    IPasswordResetEmailSender passwordResetEmails,
    IWebHostEnvironment environment,
    ILogger<AuthController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(CreateSession(await authentication.AuthenticateAsync(
                request.Email, request.Password, cancellationToken)));
        }
        catch (ApplicationAuthenticationException exception)
        {
            return Unauthorized(exception.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] SaveUsuarioDto request,
        CancellationToken cancellationToken)
    {
        ValidateReaderType(request.TipoUsuario);
        var user = await users.CrearAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        ValidateResetUrl(request.ResetUrlBase);
        var token = await passwordRecovery.CreateTokenAsync(request.Email, cancellationToken);
        string? resetUrl = null;
        if (token is not null)
        {
            resetUrl = $"{request.ResetUrlBase}?token={Uri.EscapeDataString(token)}";
            if (passwordResetEmails.IsConfigured)
            {
                try
                {
                    await passwordResetEmails.SendAsync(
                        request.Email, resetUrl, cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "No fue posible enviar el correo de recuperación.");
                }
            }
        }

        return Ok(new
        {
            DevelopmentResetUrl = environment.IsDevelopment() ? resetUrl : null
        });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await passwordRecovery.ResetPasswordAsync(
            request.Token, request.Password, cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("external-login")]
    public async Task<IActionResult> ExternalLogin(
        [FromBody] ExternalLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsWebClientAuthorized())
            return Unauthorized("Cliente web no autorizado.");
        try
        {
            return Ok(CreateSession(await authentication.AuthenticateExternalAsync(
                request.Email, cancellationToken)));
        }
        catch (ApplicationAuthenticationException exception)
        {
            return Unauthorized(exception.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("external-register")]
    public async Task<IActionResult> ExternalRegister(
        [FromBody] SaveUsuarioDto request,
        CancellationToken cancellationToken)
    {
        if (!IsWebClientAuthorized())
            return Unauthorized("Cliente web no autorizado.");
        ValidateReaderType(request.TipoUsuario);
        await users.CrearAsync(request, cancellationToken);
        return Ok(CreateSession(await authentication.AuthenticateExternalAsync(
            request.Email, cancellationToken)));
    }

    private object CreateSession(AuthenticatedUserDto authenticated)
    {
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
        var handler = new JwtSecurityTokenHandler();
        var token = handler.WriteToken(handler.CreateToken(tokenDescriptor));
        return new
        {
            Token = token,
            authenticated.Usuario,
            authenticated.Roles,
            authenticated.Permisos
        };
    }

    private bool IsWebClientAuthorized()
    {
        var configured = configuration["Authentication:WebClientSecret"];
        var supplied = Request.Headers["X-SIGEBI-Web-Key"].ToString();
        return !string.IsNullOrWhiteSpace(configured) &&
            configured.Length == supplied.Length &&
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(configured),
                Encoding.UTF8.GetBytes(supplied));
    }

    private static void ValidateReaderType(TipoUsuario type)
    {
        if (type is not (TipoUsuario.Estudiante or TipoUsuario.Docente))
            throw new BusinessRuleException(
                "El registro público solo permite Estudiante o Docente.");
    }

    private void ValidateResetUrl(string resetUrl)
    {
        if (!Uri.TryCreate(resetUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
            throw new BusinessRuleException("La URL de recuperación no es válida.");

        var allowedOrigins = configuration
            .GetSection("Authentication:PasswordResetAllowedOrigins")
            .Get<string[]>() ?? [];
        var origin = uri.GetLeftPart(UriPartial.Authority);
        if (!allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
            throw new BusinessRuleException(
                "El origen de la URL de recuperación no está autorizado.");
    }
}

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public sealed class ForgotPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string ResetUrlBase { get; set; } = string.Empty;
}

public sealed class ResetPasswordRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public sealed class ExternalLoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
