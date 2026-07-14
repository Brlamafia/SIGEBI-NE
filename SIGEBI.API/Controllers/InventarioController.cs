using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Inventario;
using SIGEBI.Application.Interfaces.Inventario;

namespace SIGEBI.API.Controllers;

[Authorize(Roles = "Administrador,Bibliotecario")]
[ApiController]
[Route("api/[controller]")]
public class InventarioController(IInventarioService inventario) : ControllerBase
{
    [HttpGet("{inventarioId:int}")]
    public async Task<ActionResult<InventarioDto>> ObtenerPorId(
        int inventarioId,
        CancellationToken cancellationToken)
        => Ok(await inventario.ObtenerPorIdAsync(inventarioId, cancellationToken));

    [HttpGet("libro/{libroId:int}")]
    public async Task<ActionResult<InventarioDto>> ObtenerPorLibro(
        int libroId,
        CancellationToken cancellationToken)
        => Ok(await inventario.ObtenerPorLibroIdAsync(libroId, cancellationToken));

    [HttpGet("libro/{libroId:int}/ejemplares")]
    public async Task<ActionResult<IReadOnlyCollection<EjemplarDto>>> ObtenerEjemplares(
        int libroId,
        CancellationToken cancellationToken)
        => Ok(await inventario.ObtenerEjemplaresPorLibroAsync(libroId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<InventarioDto>> Crear(
        [FromBody] CrearInventarioDto dto,
        CancellationToken cancellationToken)
    {
        var creado = await inventario.CrearAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(ObtenerPorId), new { inventarioId = creado.Id }, creado);
    }

    [HttpPut("ajustar")]
    public async Task<ActionResult<InventarioDto>> Ajustar(
        [FromBody] AjustarInventarioDto dto,
        CancellationToken cancellationToken)
        => Ok(await inventario.AjustarCantidadTotalAsync(dto, cancellationToken));

    [HttpPut("ejemplares/estado")]
    public async Task<ActionResult<EjemplarDto>> CambiarEstadoEjemplar(
        [FromBody] CambiarEstadoEjemplarDto dto,
        CancellationToken cancellationToken)
        => Ok(await inventario.CambiarEstadoEjemplarAsync(dto, cancellationToken));
}
