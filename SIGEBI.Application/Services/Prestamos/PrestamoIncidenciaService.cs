using System.Data;
using AutoMapper;
using Microsoft.Extensions.Logging;
using SIGEBI.Application.Common;
using SIGEBI.Application.Dtos.Multas;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Models.Prestamos;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Policies;
using SIGEBI.Domain.Services;

namespace SIGEBI.Application.Services.Prestamos;

public sealed class PrestamoIncidenciaService(
    IPrestamoOperacionContextoResolver contextoResolver,
    IResponsablePrestamoResolver responsables,
    IPrestamoPersistenciaOperaciones persistencia,
    PrestamoDomainService prestamoDominio,
    MultaDomainService multaDominio,
    PoliticaPrestamos politica,
    IUnitOfWork unitOfWork,
    IPrestamoEventosService eventos,
    IMapper mapper,
    ILogger<PrestamoIncidenciaService> logger) : IPrestamoIncidenciaService
{
    public async Task<MultaDto?> RegistrarDevolucionAsync(
        RegistrarDevolucionDto dto,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var empleado = await responsables.ResolverEmpleadoAsync(
            dto.EmpleadoDevolucionId,
            ct);
        Multa? multa = null;

        try
        {
            await unitOfWork.EjecutarEnTransaccionAsync(async transaccionCt =>
            {
                var contexto = await contextoResolver.ResolverAsync(
                    dto.PrestamoId,
                    empleado,
                    transaccionCt);
                if (prestamoDominio.RegistrarDevolucion(
                        contexto.Prestamo,
                        contexto.Inventario,
                        contexto.Ejemplar,
                        empleado.Id,
                        DateTimeNormalizer.ToUtc(dto.FechaRealDevolucion)))
                {
                    multa = multaDominio.GenerarMultaPorRetraso(
                        contexto.Prestamo,
                        politica.MontoMultaPorDia,
                        await persistencia.ObtenerMultasPorUsuarioAsync(
                            contexto.Prestamo.UsuarioId,
                            transaccionCt));
                }

                AplicarCambios(contexto.Prestamo, contexto.Inventario, contexto.Ejemplar);
                if (multa is not null)
                    await persistencia.AgregarMultaAsync(multa, transaccionCt);
                await eventos.AgregarRangoAsync(
                    [CrearEventoDevolucion(contexto.Prestamo, contexto.Ejemplar.Codigo, empleado.UsuarioId, multa)],
                    transaccionCt);
            }, IsolationLevel.ReadCommitted, ct);

            return multa is null ? null : mapper.Map<MultaDto>(multa);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al registrar la devolución del préstamo {PrestamoId}",
                dto.PrestamoId);
            throw;
        }
    }

    public Task<MultaDto> RegistrarPerdidaAsync(
        RegistrarPerdidaDto dto,
        CancellationToken ct = default) =>
        RegistrarIncidenciaAsync(
            dto.PrestamoId,
            dto.EmpleadoResponsableId,
            dto.FechaReporte,
            politica.MontoMultaPorPerdida,
            dto.Motivo,
            true,
            ct);

    public Task<MultaDto> RegistrarDevolucionConDanioAsync(
        RegistrarDanioDto dto,
        CancellationToken ct = default) =>
        RegistrarIncidenciaAsync(
            dto.PrestamoId,
            dto.EmpleadoResponsableId,
            dto.FechaDevolucion,
            politica.MontoMultaPorDanio,
            dto.Motivo,
            false,
            ct);

    private async Task<MultaDto> RegistrarIncidenciaAsync(
        int prestamoId,
        int empleadoInformadoId,
        DateTime fecha,
        decimal monto,
        string motivo,
        bool esPerdida,
        CancellationToken ct)
    {
        var empleado = await responsables.ResolverEmpleadoAsync(empleadoInformadoId, ct);
        Multa? multa = null;

        try
        {
            await unitOfWork.EjecutarEnTransaccionAsync(async transaccionCt =>
            {
                var contexto = await contextoResolver.ResolverAsync(
                    prestamoId,
                    empleado,
                    transaccionCt);
                var multasUsuario = await persistencia.ObtenerMultasPorUsuarioAsync(
                    contexto.Prestamo.UsuarioId,
                    transaccionCt);
                var fechaUtc = DateTimeNormalizer.ToUtc(fecha);

                if (esPerdida)
                {
                    prestamoDominio.RegistrarPerdida(
                        contexto.Prestamo,
                        contexto.Inventario,
                        contexto.Ejemplar,
                        empleado.Id,
                        fechaUtc);
                    multa = multaDominio.GenerarMultaPorPerdida(
                        contexto.Prestamo,
                        monto,
                        motivo,
                        multasUsuario);
                }
                else
                {
                    prestamoDominio.RegistrarDevolucionConDanio(
                        contexto.Prestamo,
                        contexto.Inventario,
                        contexto.Ejemplar,
                        empleado.Id,
                        fechaUtc);
                    multa = multaDominio.GenerarMultaPorDanio(
                        contexto.Prestamo,
                        monto,
                        motivo,
                        multasUsuario);
                }

                AplicarCambios(contexto.Prestamo, contexto.Inventario, contexto.Ejemplar);
                await persistencia.AgregarMultaAsync(multa, transaccionCt);
                await eventos.AgregarRangoAsync(
                    [CrearEventoIncidencia(
                        contexto.Prestamo,
                        contexto.Ejemplar.Codigo,
                        empleado.UsuarioId,
                        multa,
                        motivo,
                        esPerdida)],
                    transaccionCt);
            }, IsolationLevel.ReadCommitted, ct);

            return mapper.Map<MultaDto>(multa!);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al registrar una incidencia del préstamo {PrestamoId}",
                prestamoId);
            throw;
        }
    }

    private void AplicarCambios(
        Prestamo prestamo,
        SIGEBI.Domain.Entities.Catalogo.Inventario inventario,
        SIGEBI.Domain.Entities.Catalogo.Ejemplar ejemplar)
    {
        persistencia.Actualizar(prestamo, inventario, ejemplar);
    }

    private static PrestamoEventoAplicacion CrearEventoDevolucion(
        Prestamo prestamo,
        string codigoEjemplar,
        int usuarioResponsableId,
        Multa? multa) =>
        new(
            usuarioResponsableId,
            ModuloAuditoria.Prestamos,
            AccionAuditoria.Devolver,
            $"Devolución del préstamo {prestamo.Id} registrada; ejemplar {codigoEjemplar}; penalización: {(multa is null ? "no" : "sí")}.",
            prestamo.UsuarioId,
            multa is null
                ? $"Se confirmó la devolución del préstamo #{prestamo.Id}."
                : $"Se confirmó la devolución del préstamo #{prestamo.Id} y se generó una multa de {multa.Monto:C}.",
            multa is null ? TipoNotificacion.Informacion : TipoNotificacion.Multa);

    private static PrestamoEventoAplicacion CrearEventoIncidencia(
        Prestamo prestamo,
        string codigoEjemplar,
        int usuarioResponsableId,
        Multa multa,
        string motivo,
        bool esPerdida) =>
        new(
            usuarioResponsableId,
            ModuloAuditoria.Prestamos,
            esPerdida ? AccionAuditoria.RegistrarPerdida : AccionAuditoria.RegistrarDanio,
            $"{(esPerdida ? "Pérdida" : "Daño")} del préstamo {prestamo.Id}; ejemplar {codigoEjemplar}. Motivo: {motivo}",
            prestamo.UsuarioId,
            $"Se registró {(esPerdida ? "la pérdida" : "un daño")} del préstamo #{prestamo.Id}. Multa generada: {multa.Monto:C}.",
            TipoNotificacion.Multa);
}
