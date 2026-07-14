using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces.Repositories;
using AuditoriaEntidad = SIGEBI.Domain.Entities.Auditoria.Auditoria;

namespace SIGEBI.Application.Services.Auditoria;

public sealed class AuditoriaWriter(IAuditoriaRepository auditorias) : IAuditoriaWriter
{
    public Task RegistrarAsync(
        int usuarioResponsableId,
        ModuloAuditoria modulo,
        AccionAuditoria accion,
        string descripcion,
        ResultadoAuditoria resultado = ResultadoAuditoria.Exitoso,
        CancellationToken cancellationToken = default)
        => auditorias.AgregarAsync(
            new AuditoriaEntidad(usuarioResponsableId, modulo, accion, descripcion, resultado),
            cancellationToken);
}
