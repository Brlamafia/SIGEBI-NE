using SIGEBI.Application.Dtos.Usuarios;

namespace SIGEBI.Application.Dtos.Auth;

public sealed class AuthenticatedUserDto
{
    public required UsuarioDto Usuario { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; }
    public required IReadOnlyCollection<string> Permisos { get; init; }
}
