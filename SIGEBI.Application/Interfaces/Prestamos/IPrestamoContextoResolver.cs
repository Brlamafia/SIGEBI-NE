using SIGEBI.Application.Models.Prestamos;
using SIGEBI.Domain.Entities.Usuarios;

namespace SIGEBI.Application.Interfaces.Prestamos;

public interface IPrestamoRegistroContextoResolver
{
    Task<PrestamoRegistroContexto> ResolverAsync(
        int solicitudPrestamoId,
        Empleado empleado,
        CancellationToken cancellationToken = default);
}

public interface IPrestamoOperacionContextoResolver
{
    Task<PrestamoOperacionContexto> ResolverAsync(
        int prestamoId,
        Empleado empleado,
        CancellationToken cancellationToken = default);
}
