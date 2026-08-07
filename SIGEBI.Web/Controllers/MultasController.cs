using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Web.Models;
using SIGEBI.Web.Services;

namespace SIGEBI.Web.Controllers;

[Authorize]
public sealed class MultasController(ISigebiApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var summaryTask = api.GetMySummaryAsync(cancellationToken);
        var catalogTask = api.GetBooksAsync(cancellationToken: cancellationToken);
        await Task.WhenAll(summaryTask, catalogTask);

        var summary = await summaryTask;
        var catalog = await catalogTask;
        return View(new MultasViewModel
        {
            Multas = summary.Multas,
            Prestamos = summary.Prestamos.DistinctBy(item => item.Id).ToDictionary(item => item.Id),
            TitulosLibros = catalog.DistinctBy(item => item.Id).ToDictionary(item => item.Id, item => item.Titulo)
        });
    }
}
