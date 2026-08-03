using System.Data;
using Microsoft.Extensions.Logging;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Models.Prestamos;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Policies;

namespace SIGEBI.Application.Services.Prestamos;

public sealed class PrestamoMantenimientoService(
    IPrestamoRepository prestamos,
    IResponsablePrestamoResolver responsables,
    IPrestamoEventosService eventos,
    IUnitOfWork unitOfWork,
    PoliticaPrestamos politica,
    ILogger<PrestamoMantenimientoService> logger) : IPrestamoMantenimientoService
{
    public async Task<int> ActualizarPrestamosVencidosAsync(
        ActualizarPrestamosVencidosDto dto,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var usuarioResponsableId = responsables.ResolverUsuario(
            dto.UsuarioResponsableId);
        var cantidad = 0;

        try
        {
            await unitOfWork.EjecutarEnTransaccionAsync(async transaccionCt =>
            {
                var vencidos = await prestamos.ObtenerActivosVencidosAsync(
                    dto.FechaReferencia,
                    transaccionCt);
                var loteEventos = new List<PrestamoEventoAplicacion>(vencidos.Count);

                foreach (var prestamo in vencidos)
                {
                    prestamo.MarcarComoVencido(dto.FechaReferencia);
                    prestamos.Actualizar(prestamo);
                    loteEventos.Add(new PrestamoEventoAplicacion(
                        usuarioResponsableId,
                        ModuloAuditoria.Prestamos,
                        AccionAuditoria.ActualizarEstado,
                        $"Préstamo {prestamo.Id} marcado como vencido.",
                        prestamo.UsuarioId,
                        $"El préstamo #{prestamo.Id} está vencido desde {prestamo.FechaEsperadaDevolucion:dd/MM/yyyy}.",
                        TipoNotificacion.Vencimiento));
                }

                await eventos.AgregarRangoAsync(loteEventos, transaccionCt);
                cantidad = vencidos.Count;
            }, IsolationLevel.ReadCommitted, ct);

            return cantidad;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error al actualizar los préstamos vencidos");
            throw;
        }
    }

    public async Task<int> GenerarRecordatoriosVencimientoAsync(
        DateTime fechaReferencia,
        CancellationToken ct = default)
    {
        var desde = fechaReferencia.Date;
        var hasta = desde
            .AddDays(politica.DiasAnticipacionRecordatorio + 1)
            .AddTicks(-1);
        var proximos = await prestamos.ObtenerActivosProximosAVencerAsync(
            desde,
            hasta,
            ct);
        var recordatorios = proximos
            .Select(prestamo => new PrestamoRecordatorio(
                prestamo.Id,
                prestamo.UsuarioId,
                $"Recordatorio: el préstamo #{prestamo.Id} vence el {prestamo.FechaEsperadaDevolucion:dd/MM/yyyy}."))
            .ToArray();
        var enviados = 0;

        await unitOfWork.EjecutarEnTransaccionAsync(async transaccionCt =>
        {
            enviados = await eventos.AgregarRecordatoriosSiNoExistenAsync(
                recordatorios,
                desde,
                transaccionCt);
        }, IsolationLevel.ReadCommitted, ct);

        return enviados;
    }
}
