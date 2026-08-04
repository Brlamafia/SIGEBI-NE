using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Web.Models;
using SIGEBI.Web.Services;

namespace SIGEBI.Web.Controllers;

[Authorize]
public sealed class CatalogoController(
    ISigebiApiClient api,
    ILogger<CatalogoController> logger) : Controller
{
    private const int PageSize = 12;

    [HttpGet]
    public async Task<IActionResult> Index(
        string? termino,
        string? genero,
        string? editorial,
        bool? disponible,
        int pagina = 1,
        CancellationToken cancellationToken = default)
    {
        pagina = Math.Max(1, pagina);
        var booksTask = api.SearchBooksAsync(
            termino,
            genero,
            editorial,
            disponible,
            pagina,
            PageSize,
            cancellationToken);
        var requestsTask = api.GetMyRequestsAsync(cancellationToken);
        var summaryTask = api.GetMySummaryAsync(cancellationToken);
        var catalogTask = api.GetBooksAsync(cancellationToken: cancellationToken);
        await Task.WhenAll(
            booksTask,
            requestsTask,
            summaryTask,
            catalogTask);

        var books = await booksTask;
        var hasNextPage = books.Count > PageSize;
        var requests = await requestsTask;
        var summary = await summaryTask;
        var restriction = GetRequestRestriction(summary);
        var catalog = await catalogTask;

        return View(new CatalogoViewModel
        {
            Libros = books.Take(PageSize).ToArray(),
            Termino = termino,
            Genero = genero,
            Editorial = editorial,
            Disponible = disponible,
            Pagina = pagina,
            HayPaginaSiguiente = hasNextPage,
            LibrosConSolicitudPendiente = requests
                .Where(item => item.Estado == "Pendiente")
                .Select(item => item.LibroId)
                .ToHashSet(),
            GenerosDisponibles = catalog.Select(item => item.Genero)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item)
                .ToArray()!,
            EditorialesDisponibles = catalog.Select(item => item.Editorial)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item)
                .ToArray()!,
            RestriccionSolicitud = restriction
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return NotFound();

        var bookTask = api.GetBookByIdAsync(id, cancellationToken);
        var requestsTask = api.GetMyRequestsAsync(cancellationToken);
        var summaryTask = api.GetMySummaryAsync(cancellationToken);
        await Task.WhenAll(bookTask, requestsTask, summaryTask);

        var book = await bookTask;
        var requests = await requestsTask;
        var summary = await summaryTask;

        return View(new CatalogoDetalleViewModel
        {
            Libro = book,
            SolicitudPendiente = requests.Any(item =>
                item.LibroId == id && item.Estado == "Pendiente"),
            RestriccionSolicitud = GetRequestRestriction(summary)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Solicitar(
        SolicitarLibroViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "No se pudo identificar el libro solicitado.";
            return RedirectToCatalog(model);
        }

        try
        {
            await api.CreateRequestAsync(model.LibroId, cancellationToken);
            TempData["Success"] = "La solicitud de préstamo fue registrada.";
        }
        catch (SigebiApiException exception)
        {
            logger.LogWarning(
                exception,
                "La API rechazó la solicitud del libro {LibroId}.",
                model.LibroId);
            TempData["Error"] = exception.Message;
        }

        return RedirectToCatalog(model);
    }

    private RedirectToActionResult RedirectToCatalog(SolicitarLibroViewModel model) =>
        model.VolverAlDetalle && model.LibroId > 0
            ? RedirectToAction(nameof(Details), new { id = model.LibroId })
            : RedirectToAction(nameof(Index));

    private static string? GetRequestRestriction(MySummary summary)
    {
        var overdue = summary.Prestamos.Any(item =>
            item.Estado == "Vencido" ||
            item.Estado == "Activo" &&
            item.FechaEsperadaDevolucion < DateTime.UtcNow);
        var pendingAmount = summary.Multas
            .Where(item => item.Estado == "Pendiente")
            .Sum(item => item.Monto);

        return pendingAmount > 0
            ? $"Tienes {pendingAmount:C} en multas pendientes. Regulariza tu cuenta para solicitar otro préstamo."
            : overdue
                ? "Tienes un préstamo vencido. Debes devolverlo antes de realizar otra solicitud."
                : null;
    }
}
