using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Web.Models;
using SIGEBI.Web.Services;

namespace SIGEBI.Web.Controllers;

[Authorize]
public sealed class CuentaController(ISigebiApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(new CuentaViewModel
        {
            Usuario = await api.GetMeAsync(cancellationToken)
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarPassword(
        CuentaViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            model.Usuario = await api.GetMeAsync(cancellationToken);
            return View("Index", model);
        }

        try
        {
            await api.ChangeMyPasswordAsync(
                model.Password.PasswordActual,
                model.Password.PasswordNueva,
                cancellationToken);
            TempData["Success"] = "La contraseña fue actualizada.";
            return RedirectToAction(nameof(Index));
        }
        catch (SigebiApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            model.Usuario = await api.GetMeAsync(cancellationToken);
            return View("Index", model);
        }
    }
}
