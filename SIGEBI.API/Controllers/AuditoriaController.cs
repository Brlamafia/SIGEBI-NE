using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Auditoria;
using SIGEBI.Application.Interfaces.Auditoria;

namespace SIGEBI.API.Controllers;

[Authorize(Roles = "Administrador,Auditor")]
[ApiController]
[Route("api/[controller]")]
public class AuditoriaController(IAuditoriaService auditoria) : ControllerBase
{
    [HttpGet("{auditoriaId:int}")]
    public async Task<ActionResult<AuditoriaDto>> ObtenerPorId(
        int auditoriaId,
        CancellationToken cancellationToken)
        => Ok(await auditoria.ObtenerPorIdAsync(auditoriaId, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AuditoriaDto>>> Filtrar(
        [FromQuery] FiltroAuditoriaDto filtro,
        CancellationToken cancellationToken)
        => Ok(await auditoria.FiltrarAsync(filtro, cancellationToken));

    [HttpGet("usuario/{usuarioResponsableId:int}")]
    public async Task<ActionResult<IReadOnlyCollection<AuditoriaDto>>> ObtenerPorUsuario(
        int usuarioResponsableId,
        CancellationToken cancellationToken)
        => Ok(await auditoria.ObtenerPorUsuarioAsync(usuarioResponsableId, cancellationToken));

    [HttpGet("modulo/{modulo}")]
    public async Task<ActionResult<IReadOnlyCollection<AuditoriaDto>>> ObtenerPorModulo(
        string modulo,
        CancellationToken cancellationToken)
        => Ok(await auditoria.ObtenerPorModuloAsync(modulo, cancellationToken));

    [HttpGet("rango")]
    public async Task<ActionResult<IReadOnlyCollection<AuditoriaDto>>> ObtenerPorRango(
        [FromQuery] DateTime fechaDesde,
        [FromQuery] DateTime fechaHasta,
        CancellationToken cancellationToken)
        => Ok(await auditoria.ObtenerPorRangoAsync(fechaDesde, fechaHasta, cancellationToken));
}
