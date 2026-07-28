using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Interfaces.Catalogo;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.Seguridad;

namespace SIGEBI.Web.Controllers;

[Authorize]
public sealed class PrestamosController(
    IPrestamoService prestamos,
    IUsuarioActual usuarioActual,
    ILibroService libros) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var catalogo = (await libros.GetAllAsync()).ToArray();
        return View(new SIGEBI.Web.Models.PrestamosViewModel
        {
            Prestamos = await prestamos.ObtenerPorUsuarioAsync(
                usuarioActual.UsuarioId,
                cancellationToken),
            TitulosLibros = catalogo.ToDictionary(item => item.Id, item => item.Titulo)
        });
    }
}
