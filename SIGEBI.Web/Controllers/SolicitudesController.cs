using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Web.Models;
using SIGEBI.Web.Services;

namespace SIGEBI.Web.Controllers;

[Authorize]
public sealed class SolicitudesController(
    ISigebiApiClient api,
    ILogger<SolicitudesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var catalogTask = api.GetBooksAsync(cancellationToken: cancellationToken);
        var requestsTask = api.GetMyRequestsAsync(cancellationToken);
        await Task.WhenAll(catalogTask, requestsTask);

        var catalog = await catalogTask;
        return View(new SolicitudesViewModel
        {
            Solicitudes = await requestsTask,
            TitulosLibros = catalog.ToDictionary(item => item.Id, item => item.Titulo)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(
        CancelarSolicitudViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "No se pudo identificar la solicitud.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await api.CancelRequestAsync(model.Id, cancellationToken);
            TempData["Success"] = "La solicitud fue cancelada.";
        }
        catch (SigebiApiException exception)
        {
            logger.LogWarning(
                exception,
                "La API rechazó la cancelación de la solicitud {SolicitudId}.",
                model.Id);
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
