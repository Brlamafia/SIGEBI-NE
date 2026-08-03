using SIGEBI.Domain.Enums;

namespace SIGEBI.Application.Models.Prestamos;

public sealed record PrestamoEventoAplicacion(
    int UsuarioResponsableId,
    ModuloAuditoria Modulo,
    AccionAuditoria Accion,
    string Descripcion,
    int UsuarioDestinatarioId,
    string Mensaje,
    TipoNotificacion TipoNotificacion);

public sealed record PrestamoRecordatorio(
    int PrestamoId,
    int UsuarioId,
    string Mensaje);
