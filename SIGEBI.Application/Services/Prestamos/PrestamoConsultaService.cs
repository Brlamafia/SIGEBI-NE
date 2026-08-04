using AutoMapper;
using SIGEBI.Application.Common;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Application.Services.Prestamos;

public sealed class PrestamoConsultaService(
    IPrestamoRepository prestamos,
    IMapper mapper) : IPrestamoConsultaService
{
    public async Task<PrestamoDto> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        mapper.Map<PrestamoDto>(
            await prestamos.ObtenerPorIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Prestamo), id));

    public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorUsuarioAsync(int usuarioId, CancellationToken ct = default) =>
        mapper.Map<IReadOnlyCollection<PrestamoDto>>(await prestamos.ObtenerPorUsuarioAsync(usuarioId, ct));

    public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorLibroAsync(int libroId, CancellationToken ct = default) =>
        mapper.Map<IReadOnlyCollection<PrestamoDto>>(await prestamos.ObtenerPorLibroAsync(libroId, ct));

    public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorEjemplarAsync(int ejemplarId, CancellationToken ct = default) =>
        mapper.Map<IReadOnlyCollection<PrestamoDto>>(await prestamos.ObtenerPorEjemplarAsync(ejemplarId, ct));

    public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerDevolucionesPorUsuarioAsync(int usuarioId, CancellationToken ct = default) =>
        mapper.Map<IReadOnlyCollection<PrestamoDto>>(await prestamos.ObtenerDevolucionesPorUsuarioAsync(usuarioId, ct));

    public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerDevolucionesPorLibroAsync(int libroId, CancellationToken ct = default) =>
        mapper.Map<IReadOnlyCollection<PrestamoDto>>(await prestamos.ObtenerDevolucionesPorLibroAsync(libroId, ct));

    public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorEstadoAsync(string estado, CancellationToken ct = default) =>
        mapper.Map<IReadOnlyCollection<PrestamoDto>>(
            await prestamos.ObtenerPorEstadoAsync(
                EnumParser.ParseDefined<EstadoPrestamo>(estado, "estado"),
                ct));

    public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorRangoAsync(
        DateTime desde,
        DateTime hasta,
        CancellationToken ct = default) =>
        mapper.Map<IReadOnlyCollection<PrestamoDto>>(
            await prestamos.ObtenerPorRangoAsync(
                DateTimeNormalizer.ToUtc(desde),
                DateTimeNormalizer.ToUtc(hasta),
                ct));

    public Task<IReadOnlyCollection<PrestamoDto>> ObtenerActivosAsync(CancellationToken ct = default) =>
        ObtenerPorEstadoAsync(nameof(EstadoPrestamo.Activo), ct);

    public Task<IReadOnlyCollection<PrestamoDto>> ObtenerVencidosAsync(CancellationToken ct = default) =>
        ObtenerPorEstadoAsync(nameof(EstadoPrestamo.Vencido), ct);
}
