using System.Data;
using AutoMapper;
using Microsoft.Extensions.Logging;
using SIGEBI.Application.Common;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Models.Prestamos;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Policies;
using SIGEBI.Domain.Services;

namespace SIGEBI.Application.Services.Prestamos;

public sealed class PrestamoRegistroService(
    IPrestamoRegistroContextoResolver contextoResolver,
    IResponsablePrestamoResolver responsables,
    IPrestamoPersistenciaOperaciones persistencia,
    PrestamoDomainService dominio,
    PoliticaPrestamos politica,
    IUnitOfWork unitOfWork,
    IPrestamoEventosService eventos,
    IMapper mapper,
    ILogger<PrestamoRegistroService> logger) : IPrestamoRegistroService
{
    public async Task<PrestamoDto> RegistrarPrestamoAsync(
        RegistrarPrestamoDto dto,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var empleado = await responsables.ResolverEmpleadoAsync(dto.EmpleadoPrestamoId, ct);
        Prestamo? prestamoRegistrado = null;

        try
        {
            await unitOfWork.EjecutarEnTransaccionAsync(async transaccionCt =>
            {
                var contexto = await contextoResolver.ResolverAsync(
                    dto.SolicitudPrestamoId,
                    empleado,
                    transaccionCt);
                AprobarSolicitud(contexto.Solicitud);

                var fechaPrestamo = DateTimeNormalizer.ToUtc(dto.FechaPrestamo);
                prestamoRegistrado = dominio.RegistrarPrestamo(
                    contexto.Usuario.Id,
                    contexto.Usuario.Estado == EstadoUsuario.Activo,
                    contexto.TieneMultasPendientes,
                    contexto.TienePrestamosVencidos,
                    contexto.PrestamosActivos,
                    politica.ObtenerCondiciones(contexto.Usuario.TipoUsuario).LimitePrestamos,
                    contexto.Solicitud,
                    contexto.Empleado.Id,
                    fechaPrestamo,
                    politica.CalcularFechaLimite(contexto.Usuario.TipoUsuario, fechaPrestamo),
                    contexto.Inventario,
                    contexto.Ejemplar);

                await persistencia.AgregarPrestamoAsync(prestamoRegistrado, transaccionCt);
                await eventos.AgregarRangoAsync(
                    [new PrestamoEventoAplicacion(
                        contexto.Empleado.UsuarioId,
                        ModuloAuditoria.Prestamos,
                        AccionAuditoria.Aprobar,
                        $"Préstamo {prestamoRegistrado.Id} formalizado desde solicitud {contexto.Solicitud.Id}; ejemplar {contexto.Ejemplar.Codigo}.",
                        contexto.Usuario.Id,
                        $"Su préstamo #{prestamoRegistrado.Id} fue formalizado. Fecha límite: {prestamoRegistrado.FechaEsperadaDevolucion:dd/MM/yyyy}.",
                        TipoNotificacion.Informacion)],
                    transaccionCt);
            }, IsolationLevel.ReadCommitted, ct);

            return mapper.Map<PrestamoDto>(prestamoRegistrado!);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al registrar el préstamo de la solicitud {SolicitudId}",
                dto.SolicitudPrestamoId);
            throw;
        }
    }

    private static void AprobarSolicitud(SolicitudPrestamo solicitud)
    {
        if (solicitud.Estado == EstadoSolicitud.Pendiente)
            solicitud.Aprobar();
        else if (solicitud.Estado != EstadoSolicitud.Aprobada)
            throw new BusinessRuleException(
                "Solo una solicitud pendiente o aprobada puede convertirse en préstamo.");
    }
}
