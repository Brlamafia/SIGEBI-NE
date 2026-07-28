using SIGEBI.Application.Dtos.Auth;

namespace SIGEBI.Application.Interfaces.Seguridad;

public interface IPasswordResetTokenProtector
{
    string Protect(PasswordResetTokenData data, TimeSpan lifetime);
    PasswordResetTokenData Unprotect(string token);
}
