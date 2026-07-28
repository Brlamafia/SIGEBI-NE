using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Interfaces.Notificaciones;
using SIGEBI.Application.Interfaces.Seguridad;

namespace SIGEBI.Web.Controllers;

[Authorize]
public sealed class NotificacionesController(
    INotificacionService notificaciones,
    IUsuarioActual usuarioActual) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int pagina = 1,
        CancellationToken cancellationToken = default)
    {
        pagina = Math.Max(1, pagina);
        ViewData["Pagina"] = pagina;
        return View(await notificaciones.ObtenerPorUsuarioAsync(
            usuarioActual.UsuarioId,
            pagina,
            20,
            cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarLeida(
        int id,
        CancellationToken cancellationToken = default)
    {
        var notificacion = await notificaciones.GetByIdAsync(id);
        if (notificacion.UsuarioId != usuarioActual.UsuarioId)
            return Forbid();

        await notificaciones.MarcarComoLeidaAsync(id, cancellationToken);
        TempData["Success"] = "La notificación fue marcada como leída.";
        return RedirectToAction(nameof(Index));
    }
}
