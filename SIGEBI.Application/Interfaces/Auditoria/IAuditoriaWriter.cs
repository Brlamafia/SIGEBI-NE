using SIGEBI.Domain.Enums;

namespace SIGEBI.Application.Interfaces.Auditoria;

public interface IAuditoriaWriter
{
    Task RegistrarAsync(
        int usuarioResponsableId,
        ModuloAuditoria modulo,
        AccionAuditoria accion,
        string descripcion,
        ResultadoAuditoria resultado = ResultadoAuditoria.Exitoso,
        CancellationToken cancellationToken = default);
}
