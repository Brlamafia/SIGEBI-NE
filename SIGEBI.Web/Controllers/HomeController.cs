using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Web.Models;
using SIGEBI.Web.Services;

namespace SIGEBI.Web.Controllers;

[Authorize]
public sealed class HomeController(ISigebiApiClient api) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var summaryTask = api.GetMySummaryAsync(cancellationToken);
        var requestsTask = api.GetMyRequestsAsync(cancellationToken);
        var catalogTask = api.GetBooksAsync(cancellationToken: cancellationToken);
        await Task.WhenAll(summaryTask, requestsTask, catalogTask);

        var summary = await summaryTask;
        var requests = await requestsTask;
        var catalog = await catalogTask;
        return View(new DashboardViewModel
        {
            Usuario = summary.Usuario,
            Prestamos = summary.Prestamos,
            MontoMultasPendientes = summary.Multas
                .Where(item => item.Estado == "Pendiente")
                .Sum(item => item.Monto),
            NotificacionesSinLeer = summary.Notificaciones.Count(item => !item.Leida),
            Solicitudes = requests,
            TitulosLibros = catalog.ToDictionary(item => item.Id, item => item.Titulo)
        });
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
}
