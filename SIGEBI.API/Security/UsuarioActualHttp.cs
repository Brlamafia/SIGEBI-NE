using System.Security.Claims;
using SIGEBI.Application.Interfaces.Seguridad;

namespace SIGEBI.API.Security;

public sealed class UsuarioActualHttp(IHttpContextAccessor httpContextAccessor) : IUsuarioActual
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool EstaAutenticado => Principal?.Identity?.IsAuthenticated == true;

    public int UsuarioId =>
        int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : 0;

    public string? Rol => Principal?.FindFirstValue(ClaimTypes.Role);

    public bool TieneRol(string rol) =>
        Principal?.IsInRole(rol) == true;
}
