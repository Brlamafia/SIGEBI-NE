using SIGEBI.Application.Dtos.Auth;

namespace SIGEBI.Application.Interfaces.Seguridad;

public interface IAuthenticationService
{
    Task<AuthenticatedUserDto> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<AuthenticatedUserDto> AuthenticateExternalAsync(
        string verifiedEmail,
        CancellationToken cancellationToken = default);
}
