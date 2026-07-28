using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Catalogo;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Interfaces.SolicitudesPrestamo;

namespace SIGEBI.Web.Controllers;

[Authorize]
public sealed class SolicitudesController(
    ISolicitudPrestamoService solicitudes,
    IUsuarioActual usuarioActual,
    ILibroService libros,
    ILogger<SolicitudesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var catalogo = (await libros.GetAllAsync()).ToArray();
        return View(new SIGEBI.Web.Models.SolicitudesViewModel
        {
            Solicitudes =
                (await solicitudes.ObtenerPorUsuarioAsync(usuarioActual.UsuarioId))
                .ToArray(),
            TitulosLibros = catalogo.ToDictionary(item => item.Id, item => item.Titulo)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await solicitudes.CancelarAsync(id, usuarioActual.UsuarioId, cancellationToken);
            TempData["Success"] = "La solicitud fue cancelada.";
        }
        catch (BusinessRuleException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "No se pudo cancelar la solicitud {SolicitudId}.",
                id);
            TempData["Error"] =
                "No pudimos cancelar la solicitud en este momento. Inténtalo nuevamente.";
        }

        return RedirectToAction(nameof(Index));
    }
}
