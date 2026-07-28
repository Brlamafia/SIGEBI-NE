using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Catalogo;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Interfaces.SolicitudesPrestamo;
using SIGEBI.Web.Models;

namespace SIGEBI.Web.Controllers;

[Authorize]
public sealed class CatalogoController(
    ILibroService libros,
    ISolicitudPrestamoService solicitudes,
    IMultaService multas,
    IPrestamoService prestamos,
    IUsuarioActual usuarioActual,
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
        var resultado = await libros.BuscarLibrosAsync(
            termino,
            genero,
            editorial,
            disponible,
            (pagina - 1) * 12,
            12,
            cancellationToken);
        var solicitudesUsuario =
            await solicitudes.ObtenerPorUsuarioAsync(usuarioActual.UsuarioId)
            ?? [];
        var montoPendiente = await multas.ObtenerMontoPendientePorUsuarioAsync(
            usuarioActual.UsuarioId,
            cancellationToken);
        var prestamosUsuario =
            await prestamos.ObtenerPorUsuarioAsync(
                usuarioActual.UsuarioId,
                cancellationToken)
            ?? [];
        var prestamoVencido = prestamosUsuario.Any(item =>
            item.Estado == "Vencido" ||
            item.Estado == "Activo" &&
            item.FechaEsperadaDevolucion < DateTime.UtcNow);
        var restriccionSolicitud = montoPendiente > 0
            ? $"Tienes {montoPendiente:C} en multas pendientes. Regulariza tu cuenta para solicitar otro préstamo."
            : prestamoVencido
                ? "Tienes un préstamo vencido. Debes devolverlo antes de realizar otra solicitud."
                : null;
        var catalogoCompleto = (await libros.GetAllAsync()).ToArray();
        var librosPendientes = solicitudesUsuario
            .Where(item => item.Estado == "Pendiente")
            .Select(item => item.LibroId)
            .ToHashSet();

        return View(new CatalogoViewModel
        {
            Libros = resultado.ToArray(),
            Termino = termino,
            Genero = genero,
            Editorial = editorial,
            Disponible = disponible,
            Pagina = pagina,
            LibrosConSolicitudPendiente = librosPendientes,
            GenerosDisponibles = catalogoCompleto
                .Select(item => item.Genero)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item)
                .ToArray(),
            EditorialesDisponibles = catalogoCompleto
                .Select(item => item.Editorial)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item)
                .ToArray(),
            RestriccionSolicitud = restriccionSolicitud
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Solicitar(int libroId)
    {
        try
        {
            await solicitudes.RegistrarSolicitudAsync(new SaveSolicitudPrestamoDto
            {
                UsuarioId = usuarioActual.UsuarioId,
                LibroId = libroId
            });
            TempData["Success"] = "La solicitud de préstamo fue registrada.";
        }
        catch (BusinessRuleException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "No se pudo registrar la solicitud del libro {LibroId}.",
                libroId);
            TempData["Error"] =
                "No pudimos registrar la solicitud en este momento. Inténtalo nuevamente.";
        }

        return RedirectToAction(nameof(Index));
    }
}
