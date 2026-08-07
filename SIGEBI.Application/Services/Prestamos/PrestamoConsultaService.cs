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
    IUsuarioRepository usuarios,
    ILibroRepository libros,
    IEmpleadoRepository empleados,
    IMapper mapper) : IPrestamoConsultaService
{
    public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerTodosAsync(CancellationToken ct = default) =>
        await EnriquecerAsync(await prestamos.ObtenerTodosAsync(ct), ct);

    public async Task<PrestamoDto> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        var prestamo = await prestamos.ObtenerPorIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Prestamo), id);
        return (await EnriquecerAsync([prestamo], ct)).Single();
    }

    public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorUsuarioAsync(int usuarioId, CancellationToken ct = default) =>
        await EnriquecerAsync(await prestamos.ObtenerPorUsuarioAsync(usuarioId, ct), ct);

    public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorLibroAsync(int libroId, CancellationToken ct = default) =>
        await EnriquecerAsync(await prestamos.ObtenerPorLibroAsync(libroId, ct), ct);

    public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorEjemplarAsync(int ejemplarId, CancellationToken ct = default) =>
        await EnriquecerAsync(await prestamos.ObtenerPorEjemplarAsync(ejemplarId, ct), ct);

    public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerDevolucionesPorUsuarioAsync(int usuarioId, CancellationToken ct = default) =>
        await EnriquecerAsync(await prestamos.ObtenerDevolucionesPorUsuarioAsync(usuarioId, ct), ct);

    public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerDevolucionesPorLibroAsync(int libroId, CancellationToken ct = default) =>
        await EnriquecerAsync(await prestamos.ObtenerDevolucionesPorLibroAsync(libroId, ct), ct);

    public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorEstadoAsync(string estado, CancellationToken ct = default) =>
        await EnriquecerAsync(
            await prestamos.ObtenerPorEstadoAsync(
                EnumParser.ParseDefined<EstadoPrestamo>(estado, "estado"), ct), ct);

    public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorRangoAsync(
        DateTime desde,
        DateTime hasta,
        CancellationToken ct = default) =>
        await EnriquecerAsync(
            await prestamos.ObtenerPorRangoAsync(
                DateTimeNormalizer.ToUtc(desde), DateTimeNormalizer.ToUtc(hasta), ct), ct);

    public Task<IReadOnlyCollection<PrestamoDto>> ObtenerActivosAsync(CancellationToken ct = default) =>
        ObtenerPorEstadoAsync(nameof(EstadoPrestamo.Activo), ct);

    public Task<IReadOnlyCollection<PrestamoDto>> ObtenerVencidosAsync(CancellationToken ct = default) =>
        ObtenerPorEstadoAsync(nameof(EstadoPrestamo.Vencido), ct);

    private async Task<IReadOnlyCollection<PrestamoDto>> EnriquecerAsync(
        IReadOnlyCollection<Prestamo> entidades,
        CancellationToken ct)
    {
        if (entidades.Count == 0)
            return Array.Empty<PrestamoDto>();

        // Los repositorios comparten el mismo DbContext por solicitud. Por eso las
        // consultas se ejecutan en secuencia: iniciar varias a la vez causa el error
        // de concurrencia de EF Core y termina la petición con HTTP 500.
        var usuariosPorId = (await usuarios.ObtenerPorIdsAsync(
                entidades.Select(item => item.UsuarioId).Distinct().ToArray(), ct))
            .ToDictionary(item => item.Id);
        var librosPorId = (await libros.ObtenerPorIdsAsync(
                entidades.Select(item => item.LibroId).Distinct().ToArray(), ct))
            .ToDictionary(item => item.Id);
        var empleadosPorId = (await empleados.ObtenerTodosConDetallesAsync(ct))
            .ToDictionary(item => item.Id);

        return entidades.Select(entidad =>
        {
            var dto = mapper.Map<PrestamoDto>(entidad);
            if (usuariosPorId.TryGetValue(entidad.UsuarioId, out var usuario))
                dto.UsuarioNombre = $"{usuario.Nombre} {usuario.Apellido}".Trim();
            if (librosPorId.TryGetValue(entidad.LibroId, out var libro))
                dto.LibroTitulo = libro.Titulo;
            if (empleadosPorId.TryGetValue(entidad.EmpleadoPrestamoId, out var empleado))
                dto.EmpleadoPrestamoNombre = $"{empleado.Usuario?.Nombre} {empleado.Usuario?.Apellido}".Trim();
            if (entidad.EmpleadoDevolucionId is int devolucionId &&
                empleadosPorId.TryGetValue(devolucionId, out var empleadoDevolucion))
                dto.EmpleadoDevolucionNombre = $"{empleadoDevolucion.Usuario?.Nombre} {empleadoDevolucion.Usuario?.Apellido}".Trim();
            return dto;
        }).ToArray();
    }
}
