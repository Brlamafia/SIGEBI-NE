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
    IPrestamoRepository prestamos,
    IUsuarioRepository usuarios,
    ILibroRepository libros,
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
        return (await EnriquecerAsync([multa], ct)).Single();
    }

    public async Task<IReadOnlyCollection<MultaDto>> ObtenerPorUsuarioAsync(
        int usuarioId,
        CancellationToken ct = default) =>
        await EnriquecerAsync(await multas.ObtenerPorUsuarioAsync(usuarioId, ct), ct);

    public async Task<IReadOnlyCollection<MultaDto>> ObtenerPorEstadoAsync(
        string estado,
        CancellationToken ct = default) =>
        await EnriquecerAsync(
            await multas.ObtenerPorEstadoAsync(
                EnumParser.ParseDefined<EstadoMulta>(estado, "estado de la multa"),
                ct),
            ct);

    public async Task<IReadOnlyCollection<MultaDto>> ObtenerPorRangoAsync(
        DateTime desde,
        DateTime hasta,
        CancellationToken ct = default) =>
        await EnriquecerAsync(
            await multas.ObtenerPorRangoAsync(
                DateTimeNormalizer.ToUtc(desde),
                DateTimeNormalizer.ToUtc(hasta),
                ct),
            ct);

    private async Task<IReadOnlyCollection<MultaDto>> EnriquecerAsync(
        IReadOnlyCollection<Multa> entidades,
        CancellationToken ct)
    {
        if (entidades.Count == 0)
            return Array.Empty<MultaDto>();

        var usuariosPorId = (await usuarios.ObtenerPorIdsAsync(
                entidades.Select(item => item.UsuarioId).Distinct().ToArray(), ct))
            .ToDictionary(item => item.Id);
        var prestamosPorId = (await prestamos.ObtenerPorIdsAsync(
                entidades.Where(item => item.PrestamoId.HasValue)
                    .Select(item => item.PrestamoId!.Value)
                    .Distinct()
                    .ToArray(), ct))
            .ToDictionary(item => item.Id);
        var librosPorId = (await libros.ObtenerPorIdsAsync(
                prestamosPorId.Values.Select(item => item.LibroId).Distinct().ToArray(), ct))
            .ToDictionary(item => item.Id);

        return entidades.Select(entidad =>
        {
            var dto = mapper.Map<MultaDto>(entidad);
            if (usuariosPorId.TryGetValue(entidad.UsuarioId, out var usuario))
                dto.UsuarioNombre = $"{usuario.Nombre} {usuario.Apellido}".Trim();
            if (entidad.PrestamoId is int prestamoId &&
                prestamosPorId.TryGetValue(prestamoId, out var prestamo) &&
                librosPorId.TryGetValue(prestamo.LibroId, out var libro))
                dto.LibroTitulo = libro.Titulo;
            return dto;
        }).ToArray();
    }

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
