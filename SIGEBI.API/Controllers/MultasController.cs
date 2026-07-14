using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Multas;
using SIGEBI.Application.Interfaces.Prestamos;

namespace SIGEBI.API.Controllers;

[Authorize(Roles = "Administrador,Bibliotecario")]
[ApiController]
[Route("api/[controller]")]
public class MultasController(IMultaService multas) : ControllerBase
{
    [HttpGet("{multaId:int}")]
    public async Task<ActionResult<MultaDto>> ObtenerPorId(
        int multaId,
        CancellationToken cancellationToken)
        => Ok(await multas.ObtenerPorIdAsync(multaId, cancellationToken));

    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<ActionResult<IReadOnlyCollection<MultaDto>>> ObtenerPorUsuario(
        int usuarioId,
        CancellationToken cancellationToken)
        => Ok(await multas.ObtenerPorUsuarioAsync(usuarioId, cancellationToken));

    [HttpGet("estado/{estado}")]
    public async Task<ActionResult<IReadOnlyCollection<MultaDto>>> ObtenerPorEstado(
        string estado,
        CancellationToken cancellationToken)
        => Ok(await multas.ObtenerPorEstadoAsync(estado, cancellationToken));

    [HttpGet("usuario/{usuarioId:int}/pendientes")]
    public async Task<IActionResult> TienePendientes(
        int usuarioId,
        CancellationToken cancellationToken)
        => Ok(new
        {
            usuarioId,
            tienePendientes = await multas.TienePendientesPorUsuarioAsync(usuarioId, cancellationToken)
        });

    [HttpPut("pagar")]
    public async Task<IActionResult> MarcarComoPagada(
        [FromBody] PagarMultaDto dto,
        CancellationToken cancellationToken)
    {
        await multas.MarcarComoPagadaAsync(dto, cancellationToken);
        return NoContent();
    }

    [HttpPut("resolver")]
    public async Task<IActionResult> Resolver(
        [FromBody] ResolverMultaDto dto,
        CancellationToken cancellationToken)
    {
        await multas.ResolverAsync(dto, cancellationToken);
        return NoContent();
    }
}
