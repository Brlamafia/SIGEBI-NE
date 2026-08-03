using System.Data;
using Microsoft.Extensions.Logging;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Models.Prestamos;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Services;

namespace SIGEBI.Application.Services.Prestamos;

public sealed class PrestamoCancelacionService(
    IPrestamoOperacionContextoResolver contextoResolver,
    IResponsablePrestamoResolver responsables,
    IPrestamoPersistenciaOperaciones persistencia,
    PrestamoDomainService dominio,
    IUnitOfWork unitOfWork,
    IPrestamoEventosService eventos,
    ILogger<PrestamoCancelacionService> logger) : IPrestamoCancelacionService
{
    public async Task CancelarPrestamoAsync(
        CancelarPrestamoDto dto,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var empleado = await responsables.ResolverEmpleadoAsync(
            dto.EmpleadoResponsableId,
            ct);

        try
        {
            await unitOfWork.EjecutarEnTransaccionAsync(async transaccionCt =>
            {
                var contexto = await contextoResolver.ResolverAsync(
                    dto.PrestamoId,
                    empleado,
                    transaccionCt);
                dominio.CancelarPrestamo(
                    contexto.Prestamo,
                    contexto.Inventario,
                    contexto.Ejemplar);
                persistencia.Actualizar(
                    contexto.Prestamo,
                    contexto.Inventario,
                    contexto.Ejemplar);
                await eventos.AgregarRangoAsync(
                    [new PrestamoEventoAplicacion(
                        empleado.UsuarioId,
                        ModuloAuditoria.Prestamos,
                        AccionAuditoria.Cancelar,
                        $"Préstamo {contexto.Prestamo.Id} cancelado. Motivo: {dto.Motivo}",
                        contexto.Prestamo.UsuarioId,
                        $"El préstamo #{contexto.Prestamo.Id} fue cancelado. Motivo: {dto.Motivo}",
                        TipoNotificacion.Alerta)],
                    transaccionCt);
            }, IsolationLevel.ReadCommitted, ct);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al cancelar el préstamo {PrestamoId}",
                dto.PrestamoId);
            throw;
        }
    }
}
