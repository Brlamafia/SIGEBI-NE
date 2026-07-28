using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Interfaces.Catalogo;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.Seguridad;

namespace SIGEBI.Web.Controllers;

[Authorize]
public sealed class MultasController(
    IMultaService multas,
    IUsuarioActual usuarioActual,
    IPrestamoService prestamos,
    ILibroService libros) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var prestamosUsuario = await prestamos.ObtenerPorUsuarioAsync(
            usuarioActual.UsuarioId,
            cancellationToken);
        var catalogo = (await libros.GetAllAsync()).ToArray();
        return View(new SIGEBI.Web.Models.MultasViewModel
        {
            Multas = await multas.ObtenerPorUsuarioAsync(
                usuarioActual.UsuarioId,
                cancellationToken),
            Prestamos = prestamosUsuario.ToDictionary(item => item.Id),
            TitulosLibros = catalogo.ToDictionary(item => item.Id, item => item.Titulo)
        });
    }
}
