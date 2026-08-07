using SIGEBI.Application.Dtos.Multas;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Interfaces.Prestamos;

namespace SIGEBI.Application.Services.Prestamos;

// Fachada: mantiene un contrato único para los clientes sin concentrar la lógica.
public sealed class PrestamoService(
    IPrestamoConsultaService consultas,
    IPrestamoRegistroService registros,
    ISolicitudPrestamoDecisionService decisiones,
    IPrestamoCancelacionService cancelaciones,
    IPrestamoIncidenciaService incidencias,
    IPrestamoMantenimientoService mantenimiento) : IPrestamoService
{
    public Task<IReadOnlyCollection<PrestamoDto>> ObtenerTodosAsync(CancellationToken ct = default) =>
        consultas.ObtenerTodosAsync(ct);

    public Task<PrestamoDto> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        consultas.ObtenerPorIdAsync(id, ct);

    public Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorUsuarioAsync(int usuarioId, CancellationToken ct = default) =>
        consultas.ObtenerPorUsuarioAsync(usuarioId, ct);

    public Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorLibroAsync(int libroId, CancellationToken ct = default) =>
        consultas.ObtenerPorLibroAsync(libroId, ct);

    public Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorEjemplarAsync(int ejemplarId, CancellationToken ct = default) =>
        consultas.ObtenerPorEjemplarAsync(ejemplarId, ct);

    public Task<IReadOnlyCollection<PrestamoDto>> ObtenerDevolucionesPorUsuarioAsync(int usuarioId, CancellationToken ct = default) =>
        consultas.ObtenerDevolucionesPorUsuarioAsync(usuarioId, ct);

    public Task<IReadOnlyCollection<PrestamoDto>> ObtenerDevolucionesPorLibroAsync(int libroId, CancellationToken ct = default) =>
        consultas.ObtenerDevolucionesPorLibroAsync(libroId, ct);

    public Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorEstadoAsync(string estado, CancellationToken ct = default) =>
        consultas.ObtenerPorEstadoAsync(estado, ct);

    public Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorRangoAsync(DateTime desde, DateTime hasta, CancellationToken ct = default) =>
        consultas.ObtenerPorRangoAsync(desde, hasta, ct);

    public Task<IReadOnlyCollection<PrestamoDto>> ObtenerActivosAsync(CancellationToken ct = default) =>
        consultas.ObtenerActivosAsync(ct);

    public Task<IReadOnlyCollection<PrestamoDto>> ObtenerVencidosAsync(CancellationToken ct = default) =>
        consultas.ObtenerVencidosAsync(ct);

    public Task<PrestamoDto> RegistrarPrestamoAsync(RegistrarPrestamoDto dto, CancellationToken ct = default) =>
        registros.RegistrarPrestamoAsync(dto, ct);

    public Task RechazarSolicitudAsync(RechazarSolicitudPrestamoDto dto, CancellationToken ct = default) =>
        decisiones.RechazarSolicitudAsync(dto, ct);

    public Task CancelarPrestamoAsync(CancelarPrestamoDto dto, CancellationToken ct = default) =>
        cancelaciones.CancelarPrestamoAsync(dto, ct);

    public Task<MultaDto?> RegistrarDevolucionAsync(RegistrarDevolucionDto dto, CancellationToken ct = default) =>
        incidencias.RegistrarDevolucionAsync(dto, ct);

    public Task<MultaDto> RegistrarPerdidaAsync(RegistrarPerdidaDto dto, CancellationToken ct = default) =>
        incidencias.RegistrarPerdidaAsync(dto, ct);

    public Task<MultaDto> RegistrarDevolucionConDanioAsync(RegistrarDanioDto dto, CancellationToken ct = default) =>
        incidencias.RegistrarDevolucionConDanioAsync(dto, ct);

    public Task<int> ActualizarPrestamosVencidosAsync(ActualizarPrestamosVencidosDto dto, CancellationToken ct = default) =>
        mantenimiento.ActualizarPrestamosVencidosAsync(dto, ct);

    public Task<int> GenerarRecordatoriosVencimientoAsync(DateTime fechaReferencia, CancellationToken ct = default) =>
        mantenimiento.GenerarRecordatoriosVencimientoAsync(fechaReferencia, ct);
}
