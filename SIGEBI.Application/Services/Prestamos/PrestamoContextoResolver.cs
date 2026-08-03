using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Models.Prestamos;
using SIGEBI.Domain.Exceptions;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Application.Services.Prestamos;

public sealed class PrestamoRegistroContextoResolver(
    ISolicitudPrestamoRepository solicitudes,
    IUsuarioRepository usuarios,
    IInventarioRepository inventarios,
    IEjemplarRepository ejemplares,
    IMultaRepository multas,
    IPrestamoRepository prestamos) : IPrestamoRegistroContextoResolver
{
    public async Task<PrestamoRegistroContexto> ResolverAsync(
        int solicitudPrestamoId,
        Empleado empleado,
        CancellationToken cancellationToken = default)
    {
        var solicitud = await solicitudes.ObtenerPorIdAsync(
                solicitudPrestamoId,
                cancellationToken)
            ?? throw new NotFoundException("SolicitudPrestamo", solicitudPrestamoId);
        var usuario = await usuarios.ObtenerPorIdAsync(
                solicitud.UsuarioId,
                cancellationToken)
            ?? throw new NotFoundException("Usuario", solicitud.UsuarioId);
        var inventario = await inventarios.ObtenerPorLibroIdAsync(
                solicitud.LibroId,
                cancellationToken)
            ?? throw new BusinessRuleException(
                "El libro no posee un inventario registrado.");
        var ejemplar = await ejemplares.ObtenerDisponibleParaPrestamoAsync(
                solicitud.LibroId,
                cancellationToken)
            ?? throw new BusinessRuleException("No hay ejemplares disponibles.");

        return new PrestamoRegistroContexto(
            solicitud,
            usuario,
            empleado,
            inventario,
            ejemplar,
            await multas.TienePendientesPorUsuarioAsync(usuario.Id, cancellationToken),
            await prestamos.TieneVencidosPorUsuarioAsync(usuario.Id, cancellationToken),
            await prestamos.ContarActivosPorUsuarioAsync(usuario.Id, cancellationToken));
    }
}

public sealed class PrestamoOperacionContextoResolver(
    IPrestamoRepository prestamos,
    IInventarioRepository inventarios,
    IEjemplarRepository ejemplares) : IPrestamoOperacionContextoResolver
{
    public async Task<PrestamoOperacionContexto> ResolverAsync(
        int prestamoId,
        Empleado empleado,
        CancellationToken cancellationToken = default)
    {
        var prestamo = await prestamos.ObtenerPorIdAsync(prestamoId, cancellationToken)
            ?? throw new NotFoundException("Prestamo", prestamoId);
        var inventario = await inventarios.ObtenerPorLibroIdAsync(
                prestamo.LibroId,
                cancellationToken)
            ?? throw new BusinessRuleException("Inventario no encontrado.");
        var ejemplar = await ejemplares.ObtenerPorIdAsync(
                prestamo.EjemplarId,
                cancellationToken)
            ?? throw new NotFoundException("Ejemplar", prestamo.EjemplarId);

        return new PrestamoOperacionContexto(
            prestamo,
            empleado,
            inventario,
            ejemplar);
    }
}
