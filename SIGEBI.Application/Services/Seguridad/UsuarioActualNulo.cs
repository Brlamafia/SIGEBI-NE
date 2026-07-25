using SIGEBI.Application.Interfaces.Seguridad;

namespace SIGEBI.Application.Services.Seguridad;

public sealed class UsuarioActualNulo : IUsuarioActual
{
    public bool EstaAutenticado => false;
    public int UsuarioId => 0;
    public string? Rol => null;
}
