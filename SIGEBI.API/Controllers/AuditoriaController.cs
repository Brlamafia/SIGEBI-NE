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
        CancellationToken cancellationToken = default)
        => Ok(await auditoria.ObtenerPorIdAsync(auditoriaId, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AuditoriaDto>>> Filtrar(
        [FromQuery] FiltroAuditoriaDto filtro,
        CancellationToken cancellationToken = default)
        => Ok(await auditoria.FiltrarAsync(filtro, cancellationToken));

    [HttpGet("usuario/{usuarioResponsableId:int}")]
    public async Task<ActionResult<IReadOnlyCollection<AuditoriaDto>>> ObtenerPorUsuario(
        int usuarioResponsableId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 100,
        CancellationToken cancellationToken = default)
        => Ok(await auditoria.FiltrarAsync(new FiltroAuditoriaDto
        {
            UsuarioResponsableId = usuarioResponsableId,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        }, cancellationToken));

    [HttpGet("modulo/{modulo}")]
    public async Task<ActionResult<IReadOnlyCollection<AuditoriaDto>>> ObtenerPorModulo(
        string modulo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 100,
        CancellationToken cancellationToken = default)
        => Ok(await auditoria.FiltrarAsync(new FiltroAuditoriaDto
        {
            Modulo = modulo,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        }, cancellationToken));

    [HttpGet("rango")]
    public async Task<ActionResult<IReadOnlyCollection<AuditoriaDto>>> ObtenerPorRango(
        [FromQuery] DateTime fechaDesde,
        [FromQuery] DateTime fechaHasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 100,
        CancellationToken cancellationToken = default)
        => Ok(await auditoria.FiltrarAsync(new FiltroAuditoriaDto
        {
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        }, cancellationToken));
}
