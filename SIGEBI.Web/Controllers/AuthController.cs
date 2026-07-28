using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Usuarios;
using SIGEBI.Web.Models;
using SIGEBI.Web.Services;

namespace SIGEBI.Web.Controllers;

public sealed class AuthController(
    ISigebiApiClient api,
    IConfiguration configuration,
    ILogger<AuthController> logger) : Controller
{
    private bool GoogleEnabled =>
        !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"]) &&
        !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]) &&
        !string.IsNullOrWhiteSpace(configuration["Authentication:WebClientSecret"]);

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl,
            GoogleEnabled = GoogleEnabled
        });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        model.GoogleEnabled = GoogleEnabled;
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var session = await api.LoginAsync(
                model.Email,
                model.Password,
                cancellationToken);
            await SignInAsync(session, model.Recordarme);
            return RedirectAfterLogin(returnUrl);
        }
        catch (SigebiApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register(string? email = null) =>
        View(new RegisterViewModel { Email = email ?? string.Empty });

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await api.RegisterAsync(ToSaveUser(model), cancellationToken);
            TempData["Success"] = "Tu cuenta fue creada. Ya puedes iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }
        catch (SigebiApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return View(model);

        var resetUrlBase = Url.Action(
            nameof(ResetPassword),
            "Auth",
            values: null,
            protocol: Request.Scheme)
            ?? throw new InvalidOperationException("No se pudo crear la URL de recuperación.");
        try
        {
            ViewData["DevelopmentResetUrl"] =
                await api.RequestPasswordResetAsync(
                    model.Email,
                    resetUrlBase,
                    cancellationToken);
        }
        catch (SigebiApiException exception)
        {
            logger.LogError(exception, "La API no pudo procesar la recuperación.");
        }

        return View("ForgotPasswordConfirmation");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResetPassword(string token) =>
        View(new ResetPasswordViewModel { Token = token });

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await api.ResetPasswordAsync(model.Token, model.Password, cancellationToken);
            TempData["Success"] = "La contraseña fue restablecida.";
            return RedirectToAction(nameof(Login));
        }
        catch (SigebiApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        if (!GoogleEnabled)
        {
            TempData["Error"] =
                "El acceso con Google requiere configurar sus credenciales y la clave privada entre Web y API.";
            return RedirectToAction(nameof(Login));
        }

        var redirectUrl = Url.Action(nameof(GoogleCallback), new { returnUrl });
        return Challenge(
            new AuthenticationProperties { RedirectUri = redirectUrl },
            GoogleDefaults.AuthenticationScheme);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GoogleCallback(
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        var external = await HttpContext.AuthenticateAsync("External");
        var email = external.Principal?.FindFirstValue(ClaimTypes.Email);
        if (!external.Succeeded || string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Google no pudo validar tu correo electrónico.";
            return RedirectToAction(nameof(Login));
        }

        try
        {
            var session = await api.ExternalLoginAsync(email, cancellationToken);
            await SignInAsync(session, true);
            await HttpContext.SignOutAsync("External");
            return RedirectAfterLogin(returnUrl);
        }
        catch (SigebiApiException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(CompleteGoogleRegistration));
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> CompleteGoogleRegistration()
    {
        var external = await HttpContext.AuthenticateAsync("External");
        var email = external.Principal?.FindFirstValue(ClaimTypes.Email);
        if (!external.Succeeded || string.IsNullOrWhiteSpace(email))
            return RedirectToAction(nameof(Login));

        return View(new GoogleRegistrationViewModel
        {
            Email = email,
            Nombre = external.Principal?.FindFirstValue(ClaimTypes.GivenName)
                ?? external.Principal?.Identity?.Name
                ?? string.Empty,
            Apellido = external.Principal?.FindFirstValue(ClaimTypes.Surname)
                ?? string.Empty
        });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteGoogleRegistration(
        GoogleRegistrationViewModel model,
        CancellationToken cancellationToken = default)
    {
        var external = await HttpContext.AuthenticateAsync("External");
        var email = external.Principal?.FindFirstValue(ClaimTypes.Email);
        if (!external.Succeeded || string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "La sesión temporal de Google expiró.";
            return RedirectToAction(nameof(Login));
        }

        model.Email = email;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var session = await api.ExternalRegisterAsync(new SaveUsuarioDto
            {
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Cedula = model.Cedula,
                Telefono = model.Telefono,
                Email = email,
                Password = $"Google1a{Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))}",
                TipoUsuario = model.TipoUsuario!.Value
            }, cancellationToken);
            await SignInAsync(session, true);
            await HttpContext.SignOutAsync("External");
            TempData["Success"] = "Tu cuenta fue vinculada con Google.";
            return RedirectToAction("Index", "Home");
        }
        catch (SigebiApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccesoDenegado() => View();

    private async Task SignInAsync(ApiSession session, bool persistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.Usuario.Id.ToString()),
            new(ClaimTypes.Name, $"{session.Usuario.Nombre} {session.Usuario.Apellido}"),
            new(ClaimTypes.Email, session.Usuario.Email)
        };
        claims.AddRange(session.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(session.Permisos.Select(permission => new Claim("permission", permission)));

        var properties = new AuthenticationProperties
        {
            IsPersistent = persistent,
            AllowRefresh = true
        };
        properties.StoreTokens([
            new AuthenticationToken
            {
                Name = "access_token",
                Value = session.Token
            }
        ]);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme)),
            properties);
    }

    private static SaveUsuarioDto ToSaveUser(RegisterViewModel model) =>
        new()
        {
            Nombre = model.Nombre,
            Apellido = model.Apellido,
            Cedula = model.Cedula,
            Telefono = model.Telefono,
            Email = model.Email,
            Password = model.Password,
            TipoUsuario = model.TipoUsuario!.Value
        };

    private IActionResult RedirectAfterLogin(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Home");
}
