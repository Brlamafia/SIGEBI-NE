using System.Data;
using AutoMapper;
using Microsoft.Extensions.Logging;
using SIGEBI.Application.Common;
using SIGEBI.Application.Dtos.Multas;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Models.Prestamos;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Application.Services.Prestamos;

public sealed class MultaService(
    IMultaRepository multas,
    IResponsablePrestamoResolver responsables,
    IPrestamoEventosService eventos,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<MultaService> logger) : IMultaService
{
    public async Task<MultaDto> ObtenerPorIdAsync(
        int multaId,
        CancellationToken ct = default)
    {
        var multa = await multas.ObtenerPorIdAsync(multaId, ct)
            ?? throw new NotFoundException(nameof(Multa), multaId);
        return mapper.Map<MultaDto>(multa);
    }

    public async Task<IReadOnlyCollection<MultaDto>> ObtenerPorUsuarioAsync(
        int usuarioId,
        CancellationToken ct = default) =>
        mapper.Map<IReadOnlyCollection<MultaDto>>(
            await multas.ObtenerPorUsuarioAsync(usuarioId, ct));

    public async Task<IReadOnlyCollection<MultaDto>> ObtenerPorEstadoAsync(
        string estado,
        CancellationToken ct = default) =>
        mapper.Map<IReadOnlyCollection<MultaDto>>(
            await multas.ObtenerPorEstadoAsync(
                EnumParser.ParseDefined<EstadoMulta>(estado, "estado de la multa"),
                ct));

    public Task<bool> TienePendientesPorUsuarioAsync(
        int usuarioId,
        CancellationToken ct = default) =>
        multas.TienePendientesPorUsuarioAsync(usuarioId, ct);

    public Task<decimal> ObtenerMontoPendientePorUsuarioAsync(
        int usuarioId,
        CancellationToken ct = default) =>
        multas.ObtenerMontoPendientePorUsuarioAsync(usuarioId, ct);

    public async Task MarcarComoPagadaAsync(
        PagarMultaDto dto,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var usuarioResponsableId = responsables.ResolverUsuario(
            dto.UsuarioResponsableId);

        try
        {
            await unitOfWork.EjecutarEnTransaccionAsync(async transaccionCt =>
            {
                var multa = await multas.ObtenerPorIdAsync(dto.MultaId, transaccionCt)
                    ?? throw new NotFoundException(nameof(Multa), dto.MultaId);
                multa.MarcarComoPagada();
                multas.Actualizar(multa);
                await eventos.AgregarRangoAsync(
                    [new PrestamoEventoAplicacion(
                        usuarioResponsableId,
                        ModuloAuditoria.Multas,
                        AccionAuditoria.Pagar,
                        $"Pago de la multa {multa.Id}.",
                        multa.UsuarioId,
                        $"Se registró el pago de la multa #{multa.Id}.",
                        TipoNotificacion.Multa)],
                    transaccionCt);
            }, IsolationLevel.ReadCommitted, ct);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al registrar el pago de la multa {MultaId}",
                dto.MultaId);
            throw;
        }
    }

    public async Task ResolverAsync(
        ResolverMultaDto dto,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var empleado = await responsables.ResolverEmpleadoAsync(
            dto.EmpleadoResolucionId,
            ct);

        try
        {
            await unitOfWork.EjecutarEnTransaccionAsync(async transaccionCt =>
            {
                var multa = await multas.ObtenerPorIdAsync(dto.MultaId, transaccionCt)
                    ?? throw new NotFoundException(nameof(Multa), dto.MultaId);
                multa.Resolver(
                    empleado.Id,
                    DateTimeNormalizer.ToUtc(dto.FechaResolucion),
                    dto.Observacion);
                multas.Actualizar(multa);
                await eventos.AgregarRangoAsync(
                    [new PrestamoEventoAplicacion(
                        empleado.UsuarioId,
                        ModuloAuditoria.Multas,
                        AccionAuditoria.Resolver,
                        $"Resolución de la multa {multa.Id}.",
                        multa.UsuarioId,
                        $"La multa #{multa.Id} fue resuelta. El usuario queda habilitado si no mantiene otras multas pendientes.",
                        TipoNotificacion.Multa)],
                    transaccionCt);
            }, IsolationLevel.ReadCommitted, ct);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al resolver la multa {MultaId}",
                dto.MultaId);
            throw;
        }
    }
}
