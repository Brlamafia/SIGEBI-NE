using System.Security.Cryptography;
using System.Text;
using SIGEBI.Application.Dtos.Auth;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Security;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Application.Services.Seguridad;

public sealed class PasswordRecoveryService(
    IUsuarioRepository users,
    IPasswordResetTokenProtector tokenProtector,
    IUnitOfWork unitOfWork) : IPasswordRecoveryService
{
    public async Task<string?> CreateTokenAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var user = await users.ObtenerPorEmailAsync(email.Trim(), cancellationToken);
        if (user is null)
            return null;

        return tokenProtector.Protect(
            new PasswordResetTokenData(
                user.Id,
                Fingerprint(user.ContrasenaHash)),
            TimeSpan.FromMinutes(30));
    }

    public async Task ResetPasswordAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        PasswordResetTokenData data;
        try
        {
            data = tokenProtector.Unprotect(token);
        }
        catch
        {
            throw new BusinessRuleException(
                "El enlace de recuperación no es válido o expiró.");
        }

        var user = await users.ObtenerPorIdAsync(data.UsuarioId, cancellationToken)
            ?? throw new BusinessRuleException(
                "El enlace de recuperación no es válido o expiró.");
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(Fingerprint(user.ContrasenaHash)),
                Encoding.UTF8.GetBytes(data.PasswordFingerprint)))
            throw new BusinessRuleException(
                "El enlace de recuperación ya fue utilizado.");

        user.EstablecerContrasenaHash(PasswordHasher.Hash(newPassword));
        users.Actualizar(user);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);
    }

    private static string Fingerprint(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
