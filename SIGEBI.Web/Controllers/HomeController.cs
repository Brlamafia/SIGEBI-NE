using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Interfaces.Notificaciones;
using SIGEBI.Application.Interfaces.Catalogo;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Interfaces.SolicitudesPrestamo;
using SIGEBI.Application.Interfaces.Usuarios;
using SIGEBI.Web.Models;

namespace SIGEBI.Web.Controllers;

[Authorize]
public sealed class HomeController(
    IUsuarioActual usuarioActual,
    IUsuarioService usuarios,
    IPrestamoService prestamos,
    IMultaService multas,
    INotificacionService notificaciones,
    ISolicitudPrestamoService solicitudes,
    ILibroService libros) : Controller
{
    public async Task<IActionResult> Index()
    {
        var usuarioId = usuarioActual.UsuarioId;
        var catalogo = (await libros.GetAllAsync()).ToArray();
        var modelo = new DashboardViewModel
        {
            Usuario = await usuarios.GetByIdAsync(usuarioId),
            Prestamos = await prestamos.ObtenerPorUsuarioAsync(usuarioId),
            MontoMultasPendientes =
                await multas.ObtenerMontoPendientePorUsuarioAsync(usuarioId),
            Notificaciones = (await notificaciones.ObtenerPorUsuarioAsync(
                usuarioId,
                1,
                5)).ToArray(),
            Solicitudes = (await solicitudes.ObtenerPorUsuarioAsync(usuarioId)).ToArray(),
            TitulosLibros = catalogo.ToDictionary(item => item.Id, item => item.Titulo)
        };

        return View(modelo);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
}
