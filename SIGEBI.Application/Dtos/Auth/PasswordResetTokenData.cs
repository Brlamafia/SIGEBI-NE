namespace SIGEBI.Application.Dtos.Auth;

public sealed record PasswordResetTokenData(
    int UsuarioId,
    string PasswordFingerprint);
