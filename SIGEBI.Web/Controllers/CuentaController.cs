using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Interfaces.Usuarios;
using SIGEBI.Web.Models;

namespace SIGEBI.Web.Controllers;

[Authorize]
public sealed class CuentaController(
    IUsuarioService usuarios,
    IUsuarioActual usuarioActual) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index() =>
        View(new CuentaViewModel
        {
            Usuario = await usuarios.GetByIdAsync(usuarioActual.UsuarioId)
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarPassword(
        CuentaViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            model = new CuentaViewModel
            {
                Usuario = await usuarios.GetByIdAsync(usuarioActual.UsuarioId),
                Password = model.Password
            };
            return View("Index", model);
        }

        try
        {
            await usuarios.CambiarPasswordAsync(
                usuarioActual.UsuarioId,
                model.Password.PasswordActual,
                model.Password.PasswordNueva,
                cancellationToken);
            TempData["Success"] = "La contraseña fue actualizada.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model = new CuentaViewModel
            {
                Usuario = await usuarios.GetByIdAsync(usuarioActual.UsuarioId),
                Password = model.Password
            };
            return View("Index", model);
        }
    }
}
