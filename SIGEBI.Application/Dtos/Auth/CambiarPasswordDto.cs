namespace SIGEBI.Application.Dtos.Auth;

public sealed class CambiarPasswordDto
{
    public string PasswordActual { get; set; } = string.Empty;
    public string PasswordNueva { get; set; } = string.Empty;
}
