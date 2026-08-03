using SIGEBI.Domain.Entities.Usuarios;

namespace SIGEBI.Application.Interfaces.Prestamos;

public interface IResponsablePrestamoResolver
{
    Task<Empleado> ResolverEmpleadoAsync(
        int empleadoInformadoId,
        CancellationToken cancellationToken = default);
    int ResolverUsuario(int usuarioInformadoId);
}
