using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Interfaces.Prestamos;

namespace SIGEBI.API.Controllers;

[Authorize(Roles = "Administrador,Bibliotecario")]
[ApiController]
[Route("api/[controller]")]
public class PrestamosController : ControllerBase
{
    private readonly IPrestamoService _prestamos;

    public PrestamosController(IPrestamoService prestamos)
    {
        _prestamos = prestamos;
    }

    [HttpGet("{prestamoId:int}")]
    public async Task<ActionResult<PrestamoDto>> ObtenerPorId(
        int prestamoId,
        CancellationToken cancellationToken)
        => Ok(await _prestamos.ObtenerPorIdAsync(prestamoId, cancellationToken));

    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<ActionResult<IReadOnlyCollection<PrestamoDto>>> ObtenerPorUsuario(
        int usuarioId,
        CancellationToken cancellationToken)
        => Ok(await _prestamos.ObtenerPorUsuarioAsync(usuarioId, cancellationToken));

    [HttpGet("estado/{estado}")]
    public async Task<ActionResult<IReadOnlyCollection<PrestamoDto>>> ObtenerPorEstado(
        string estado,
        CancellationToken cancellationToken)
        => Ok(await _prestamos.ObtenerPorEstadoAsync(estado, cancellationToken));

    [HttpGet("activos")]
    public async Task<ActionResult<IReadOnlyCollection<PrestamoDto>>> ObtenerActivos(
        CancellationToken cancellationToken)
        => Ok(await _prestamos.ObtenerActivosAsync(cancellationToken));

    [HttpGet("vencidos")]
    public async Task<ActionResult<IReadOnlyCollection<PrestamoDto>>> ObtenerVencidos(
        CancellationToken cancellationToken)
        => Ok(await _prestamos.ObtenerVencidosAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<PrestamoDto>> Registrar(
        [FromBody] RegistrarPrestamoDto dto,
        CancellationToken cancellationToken)
    {
        var prestamo = await _prestamos.RegistrarPrestamoAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(ObtenerPorId), new { prestamoId = prestamo.Id }, prestamo);
    }

    [HttpPost("cancelar")]
    public async Task<IActionResult> Cancelar(
        [FromBody] CancelarPrestamoDto dto,
        CancellationToken cancellationToken)
    {
        await _prestamos.CancelarPrestamoAsync(dto, cancellationToken);
        return NoContent();
    }

    [HttpPost("solicitudes/rechazar")]
    public async Task<IActionResult> RechazarSolicitud(
        [FromBody] RechazarSolicitudPrestamoDto dto,
        CancellationToken cancellationToken)
    {
        await _prestamos.RechazarSolicitudAsync(dto, cancellationToken);
        return NoContent();
    }

    [HttpPost("perdidas")]
    public async Task<IActionResult> RegistrarPerdida(
        [FromBody] RegistrarPerdidaDto dto,
        CancellationToken cancellationToken)
        => Ok(await _prestamos.RegistrarPerdidaAsync(dto, cancellationToken));

    [HttpPut("vencidos/actualizar")]
    public async Task<IActionResult> ActualizarVencidos(
        [FromBody] ActualizarPrestamosVencidosDto dto,
        CancellationToken cancellationToken)
    {
        var cantidad = await _prestamos.ActualizarPrestamosVencidosAsync(dto, cancellationToken);
        return Ok(new { cantidadActualizada = cantidad });
    }

}
