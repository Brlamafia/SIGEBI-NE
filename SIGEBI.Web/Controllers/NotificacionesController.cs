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
            PageSize + 1,
            cancellationToken);
        var hasNextPage = notifications.Count > PageSize;
        return View(new NotificacionesViewModel
        {
            Notificaciones = notifications.Take(PageSize).ToList(),
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

        try
        {
            await api.MarkNotificationReadAsync(model.Id, cancellationToken);
            TempData["Success"] = "La notificación fue marcada como leída.";
        }
        catch (SigebiApiException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index), new { pagina = model.Pagina });
    }
}
