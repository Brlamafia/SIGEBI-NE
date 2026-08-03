using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Models.Prestamos;
using SIGEBI.Domain.Entities.Notificaciones;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces.Repositories;
using AuditoriaEntidad = SIGEBI.Domain.Entities.Auditoria.Auditoria;

namespace SIGEBI.Application.Services.Prestamos;

public sealed class PrestamoEventosService(
    IAuditoriaRepository auditorias,
    INotificacionRepository notificaciones) : IPrestamoEventosService
{
    public async Task AgregarRangoAsync(
        IReadOnlyCollection<PrestamoEventoAplicacion> eventos,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventos);
        if (eventos.Count == 0)
            return;

        await auditorias.AgregarRangoAsync(
            eventos.Select(evento => new AuditoriaEntidad(
                evento.UsuarioResponsableId,
                evento.Modulo,
                evento.Accion,
                evento.Descripcion,
                ResultadoAuditoria.Exitoso)),
            cancellationToken);
        await notificaciones.AgregarRangoAsync(
            eventos.Select(evento => new Notificacion(
                evento.UsuarioDestinatarioId,
                evento.Mensaje,
                evento.TipoNotificacion)),
            cancellationToken);
    }

    public async Task<int> AgregarRecordatoriosSiNoExistenAsync(
        IReadOnlyCollection<PrestamoRecordatorio> recordatorios,
        DateTime desde,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recordatorios);
        if (recordatorios.Count == 0)
            return 0;

        var existentes = await notificaciones.ObtenerPorUsuariosDesdeAsync(
            recordatorios.Select(item => item.UsuarioId).Distinct().ToArray(),
            desde,
            cancellationToken);
        var pendientes = recordatorios
            .Where(recordatorio => !existentes.Any(notificacion =>
                notificacion.UsuarioId == recordatorio.UsuarioId &&
                notificacion.Mensaje.Contains(
                    $"préstamo #{recordatorio.PrestamoId}",
                    StringComparison.OrdinalIgnoreCase)))
            .Select(recordatorio => new Notificacion(
                recordatorio.UsuarioId,
                recordatorio.Mensaje,
                TipoNotificacion.Vencimiento))
            .ToArray();

        await notificaciones.AgregarRangoAsync(pendientes, cancellationToken);
        return pendientes.Length;
    }
}
