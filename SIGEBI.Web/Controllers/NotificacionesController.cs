using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Web.Services;

namespace SIGEBI.Web.Controllers;

[Authorize]
public sealed class NotificacionesController(ISigebiApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int pagina = 1,
        CancellationToken cancellationToken = default)
    {
        pagina = Math.Max(1, pagina);
        ViewData["Pagina"] = pagina;
        return View(await api.GetMyNotificationsAsync(
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
        await api.MarkNotificationReadAsync(id, cancellationToken);
        TempData["Success"] = "La notificación fue marcada como leída.";
        return RedirectToAction(nameof(Index));
    }
}
