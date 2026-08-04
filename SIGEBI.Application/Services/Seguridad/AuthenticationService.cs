using SIGEBI.Application.Dtos.Auth;
using SIGEBI.Application.Dtos.Usuarios;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Options;
using SIGEBI.Application.Security;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Application.Services.Seguridad;

public sealed class AuthenticationService(
    IUsuarioRepository users,
    IAuditoriaWriter audit,
    IUnitOfWork unitOfWork,
    AuthenticationOptions options) : IAuthenticationService
{
    public async Task<AuthenticatedUserDto> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new AuthenticationException("Credenciales inválidas.");

        var user = await users.ObtenerPorEmailAsync(email.Trim(), cancellationToken);
        if (user is null || user.Estado != EstadoUsuario.Activo)
            throw new AuthenticationException("Credenciales inválidas.");
        if (user.EstaBloqueado(DateTime.UtcNow))
            throw new AuthenticationException("La cuenta está temporalmente bloqueada.");

        if (!PasswordHasher.Verify(password, user.ContrasenaHash))
        {
            user.RegistrarIntentoFallido(
                options.MaxFailedAttempts,
                TimeSpan.FromMinutes(options.LockoutMinutes));
            users.Actualizar(user);
            await audit.RegistrarAsync(
                user.Id,
                ModuloAuditoria.Usuarios,
                AccionAuditoria.ActualizarEstado,
                "Intento de acceso fallido.",
                ResultadoAuditoria.Fallido,
                cancellationToken);
            await unitOfWork.GuardarCambiosAsync(cancellationToken);
            throw new AuthenticationException("Credenciales inválidas.");
        }

        user.RegistrarAccesoExitoso();
        users.Actualizar(user);
        await audit.RegistrarAsync(
            user.Id,
            ModuloAuditoria.Usuarios,
            AccionAuditoria.Registrar,
            "Inicio de sesión exitoso.",
            cancellationToken: cancellationToken);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return BuildResult(user);
    }

    public async Task<AuthenticatedUserDto> AuthenticateExternalAsync(
        string verifiedEmail,
        CancellationToken cancellationToken = default)
    {
        var user = await users.ObtenerPorEmailAsync(
            verifiedEmail.Trim(),
            cancellationToken);
        if (user is null || user.Estado != EstadoUsuario.Activo)
            throw new AuthenticationException(
                "No existe una cuenta activa de SIGEBI asociada a este correo.");

        user.RegistrarAccesoExitoso();
        users.Actualizar(user);
        await audit.RegistrarAsync(
            user.Id,
            ModuloAuditoria.Usuarios,
            AccionAuditoria.Registrar,
            "Inicio de sesión con proveedor externo.",
            cancellationToken: cancellationToken);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return BuildResult(user);
    }

    private static AuthenticatedUserDto BuildResult(
        SIGEBI.Domain.Entities.Usuarios.Usuario user)
    {
        var roles = user.Roles
            .Select(role => role.Nombre)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (roles.Count == 0)
            roles.Add("Usuario");

        var permissions = user.Roles
            .SelectMany(role => role.Permisos)
            .Select(permission => permission.Codigo)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(permission => permission)
            .ToArray();
        return new AuthenticatedUserDto
        {
            // El repositorio ya cargó el usuario, sus roles y permisos para validar
            // el acceso. Reutilizar esa entidad evita una segunda consulta remota.
            Usuario = new UsuarioDto
            {
                Id = user.Id,
                Nombre = user.Nombre,
                Apellido = user.Apellido,
                Email = user.Email,
                Cedula = user.Cedula,
                Telefono = user.Telefono,
                TipoUsuario = user.TipoUsuario.ToString(),
                Estado = user.Estado.ToString(),
                TieneMultasPendientes = false
            },
            Roles = roles.OrderBy(role => role).ToArray(),
            Permisos = permissions
        };
    }
}
