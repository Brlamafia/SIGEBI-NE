using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Exceptions;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Application.Services.Prestamos;

public sealed class ResponsablePrestamoResolver(
    IEmpleadoRepository empleados,
    IUsuarioActual usuarioActual) : IResponsablePrestamoResolver
{
    public async Task<Empleado> ResolverEmpleadoAsync(
        int empleadoInformadoId,
        CancellationToken cancellationToken = default)
    {
        if (usuarioActual.EstaAutenticado)
        {
            return await empleados.ObtenerPorUsuarioIdAsync(
                    usuarioActual.UsuarioId,
                    cancellationToken)
                ?? throw new BusinessRuleException(
                    "El usuario autenticado no está registrado como empleado.");
        }

        return await empleados.ObtenerPorIdAsync(
                empleadoInformadoId,
                cancellationToken)
            ?? throw new NotFoundException("Empleado", empleadoInformadoId);
    }

    public int ResolverUsuario(int usuarioInformadoId)
    {
        var usuarioId = usuarioActual.EstaAutenticado
            ? usuarioActual.UsuarioId
            : usuarioInformadoId;
        return usuarioId > 0
            ? usuarioId
            : throw new BusinessRuleException(
                "No se pudo determinar el usuario responsable.");
    }
}
