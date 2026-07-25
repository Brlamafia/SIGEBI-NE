namespace SIGEBI.Application.Interfaces.Seguridad;

public interface IUsuarioActual
{
    bool EstaAutenticado { get; }
    int UsuarioId { get; }
    string? Rol { get; }
}
