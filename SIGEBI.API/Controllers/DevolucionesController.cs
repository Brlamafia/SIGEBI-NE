using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Multas;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Interfaces.Prestamos;

namespace SIGEBI.API.Controllers;

[Authorize(Roles = "Administrador,Bibliotecario")]
[ApiController]
[Route("api/[controller]")]
public class DevolucionesController(IPrestamoService prestamos) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MultaDto?>> Registrar(
        [FromBody] RegistrarDevolucionDto dto,
        CancellationToken cancellationToken)
    {
        var multa = await prestamos.RegistrarDevolucionAsync(dto, cancellationToken);
        return Ok(new
        {
            mensaje = multa is null
                ? "Devolución registrada sin penalización."
                : "Devolución registrada con penalización.",
            multa
        });
    }

    [HttpPost("con-danio")]
    public async Task<ActionResult<MultaDto>> RegistrarConDanio(
        [FromBody] RegistrarDanioDto dto,
        CancellationToken cancellationToken)
        => Ok(await prestamos.RegistrarDevolucionConDanioAsync(dto, cancellationToken));
}
