using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Web.Models;
using SIGEBI.Web.Services;

namespace SIGEBI.Web.Controllers;

[Authorize]
public sealed class NotificacionesController(ISigebiApiClient api) : Controller
{
    private const int PageSize = 20;

    [HttpGet]
    public async Task<IActionResult> Index(
        int pagina = 1,
        CancellationToken cancellationToken = default)
    {
        pagina = Math.Max(1, pagina);
        var notifications = await api.GetMyNotificationsAsync(
            pagina,
            PageSize,
            cancellationToken);
        var hasNextPage = notifications.Count == PageSize &&
            (await api.GetMyNotificationsAsync(
                pagina + 1,
                PageSize,
                cancellationToken)).Count > 0;
        return View(new NotificacionesViewModel
        {
            Notificaciones = notifications,
            Pagina = pagina,
            HayPaginaSiguiente = hasNextPage
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarLeida(
        MarcarNotificacionViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "No se pudo identificar la notificación.";
            return RedirectToAction(nameof(Index));
        }

        await api.MarkNotificationReadAsync(model.Id, cancellationToken);
        TempData["Success"] = "La notificación fue marcada como leída.";
        return RedirectToAction(nameof(Index), new { pagina = model.Pagina });
    }
}
