namespace SIGEBI.Application.Interfaces.Seguridad;

public interface IPasswordRecoveryService
{
    Task<string?> CreateTokenAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);
}
