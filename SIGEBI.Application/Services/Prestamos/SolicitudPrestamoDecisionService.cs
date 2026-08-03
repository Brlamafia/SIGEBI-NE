using System.Data;
using Microsoft.Extensions.Logging;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Models.Prestamos;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Application.Services.Prestamos;

public sealed class SolicitudPrestamoDecisionService(
    ISolicitudPrestamoRepository solicitudes,
    IResponsablePrestamoResolver responsables,
    IUnitOfWork unitOfWork,
    IPrestamoEventosService eventos,
    ILogger<SolicitudPrestamoDecisionService> logger) : ISolicitudPrestamoDecisionService
{
    public async Task RechazarSolicitudAsync(
        RechazarSolicitudPrestamoDto dto,
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
                var solicitud = await solicitudes.ObtenerPorIdAsync(
                        dto.SolicitudPrestamoId,
                        transaccionCt)
                    ?? throw new NotFoundException(
                        "Solicitud",
                        dto.SolicitudPrestamoId);
                solicitud.Rechazar(dto.Motivo);
                solicitudes.Actualizar(solicitud);
                await eventos.AgregarRangoAsync(
                    [new PrestamoEventoAplicacion(
                        empleado.UsuarioId,
                        ModuloAuditoria.Solicitudes,
                        AccionAuditoria.Rechazar,
                        $"Solicitud {solicitud.Id} rechazada. Motivo: {dto.Motivo}",
                        solicitud.UsuarioId,
                        $"Su solicitud #{solicitud.Id} fue rechazada. Motivo: {dto.Motivo}",
                        TipoNotificacion.Alerta)],
                    transaccionCt);
            }, IsolationLevel.ReadCommitted, ct);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al rechazar la solicitud {SolicitudId}",
                dto.SolicitudPrestamoId);
            throw;
        }
    }
}
