using Microsoft.AspNetCore.DataProtection;
using SIGEBI.Application.Dtos.Auth;
using SIGEBI.Application.Interfaces.Seguridad;

namespace SIGEBI.API.Security;

public sealed class DataProtectionPasswordResetTokenProtector(
    IDataProtectionProvider provider) : IPasswordResetTokenProtector
{
    private readonly ITimeLimitedDataProtector _protector = provider
        .CreateProtector("SIGEBI.PasswordReset.v1")
        .ToTimeLimitedDataProtector();

    public string Protect(PasswordResetTokenData data, TimeSpan lifetime) =>
        _protector.Protect($"{data.UsuarioId}|{data.PasswordFingerprint}", lifetime);

    public PasswordResetTokenData Unprotect(string token)
    {
        var value = _protector.Unprotect(token);
        var parts = value.Split('|', 2);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var userId))
            throw new InvalidOperationException("Invalid password reset token.");
        return new PasswordResetTokenData(userId, parts[1]);
    }
}
