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
        var books = await api.SearchBooksAsync(
            termino, genero, editorial, disponible, pagina, 12, cancellationToken);
        var requests = await api.GetMyRequestsAsync(cancellationToken);
        var summary = await api.GetMySummaryAsync(cancellationToken);
        var overdue = summary.Prestamos.Any(item =>
            item.Estado == "Vencido" ||
            item.Estado == "Activo" &&
            item.FechaEsperadaDevolucion < DateTime.UtcNow);
        var pendingAmount = summary.Multas
            .Where(item => item.Estado == "Pendiente")
            .Sum(item => item.Monto);
        var restriction = pendingAmount > 0
            ? $"Tienes {pendingAmount:C} en multas pendientes. Regulariza tu cuenta para solicitar otro préstamo."
            : overdue
                ? "Tienes un préstamo vencido. Debes devolverlo antes de realizar otra solicitud."
                : null;
        var catalog = await api.GetBooksAsync(cancellationToken: cancellationToken);

        return View(new CatalogoViewModel
        {
            Libros = books,
            Termino = termino,
            Genero = genero,
            Editorial = editorial,
            Disponible = disponible,
            Pagina = pagina,
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Solicitar(
        int libroId,
        CancellationToken cancellationToken)
    {
        try
        {
            await api.CreateRequestAsync(libroId, cancellationToken);
            TempData["Success"] = "La solicitud de préstamo fue registrada.";
        }
        catch (SigebiApiException exception)
        {
            logger.LogWarning(
                exception,
                "La API rechazó la solicitud del libro {LibroId}.",
                libroId);
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
