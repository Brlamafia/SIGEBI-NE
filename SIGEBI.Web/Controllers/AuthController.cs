using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Dtos.Auth;
using SIGEBI.Application.Dtos.Usuarios;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Interfaces.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Web.Models;
using ApplicationAuthenticationException = SIGEBI.Application.Exceptions.AuthenticationException;
using ApplicationAuthenticationService = SIGEBI.Application.Interfaces.Seguridad.IAuthenticationService;

namespace SIGEBI.Web.Controllers;

public sealed class AuthController(
    ApplicationAuthenticationService authenticationService,
    IUsuarioService users,
    IPasswordRecoveryService passwordRecovery,
    IPasswordResetEmailSender passwordResetEmails,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger<AuthController> logger) : Controller
{
    private bool GoogleEnabled =>
        !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"]) &&
        !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]);

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
            var authenticated = await authenticationService.AuthenticateAsync(
                model.Email,
                model.Password,
                cancellationToken);
            await SignInAsync(authenticated, model.Recordarme);
            return RedirectAfterLogin(returnUrl);
        }
        catch (ApplicationAuthenticationException exception)
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
            await users.CrearAsync(new SaveUsuarioDto
            {
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Cedula = model.Cedula,
                Telefono = model.Telefono,
                Email = model.Email,
                Password = model.Password,
                TipoUsuario = model.TipoUsuario!.Value
            }, cancellationToken);
            TempData["Success"] = "Tu cuenta fue creada. Ya puedes iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }
        catch (Exception exception) when (
            exception is BusinessRuleException or ArgumentException)
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

        var token = await passwordRecovery.CreateTokenAsync(
            model.Email,
            cancellationToken);
        if (token is not null)
        {
            var resetUrl = Url.Action(
                nameof(ResetPassword),
                "Auth",
                new { token },
                Request.Scheme);

            if (passwordResetEmails.IsConfigured &&
                !string.IsNullOrWhiteSpace(resetUrl))
            {
                try
                {
                    await passwordResetEmails.SendAsync(
                        model.Email,
                        resetUrl,
                        cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "No fue posible enviar el correo de recuperación mediante SMTP.");
                    if (environment.IsDevelopment())
                        ViewData["DevelopmentResetUrl"] = resetUrl;
                }
            }
            else if (environment.IsDevelopment())
            {
                ViewData["DevelopmentResetUrl"] = resetUrl;
            }
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
            await passwordRecovery.ResetPasswordAsync(
                model.Token,
                model.Password,
                cancellationToken);
            TempData["Success"] = "La contraseña fue restablecida.";
            return RedirectToAction(nameof(Login));
        }
        catch (BusinessRuleException exception)
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
                "El acceso con Google requiere configurar el Client ID y el Client Secret.";
            return RedirectToAction(nameof(Login));
        }

        var redirectUrl = Url.Action(
            nameof(GoogleCallback),
            new { returnUrl });
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
            var authenticated = await authenticationService.AuthenticateExternalAsync(
                email,
                cancellationToken);
            await SignInAsync(authenticated, true);
            await HttpContext.SignOutAsync("External");
            return RedirectAfterLogin(returnUrl);
        }
        catch (ApplicationAuthenticationException exception)
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
            await users.CrearAsync(new SaveUsuarioDto
            {
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Cedula = model.Cedula,
                Telefono = model.Telefono,
                Email = email,
                Password = $"Google1a{Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))}",
                TipoUsuario = model.TipoUsuario!.Value
            }, cancellationToken);

            var authenticated = await authenticationService.AuthenticateExternalAsync(
                email,
                cancellationToken);
            await SignInAsync(authenticated, true);
            await HttpContext.SignOutAsync("External");
            TempData["Success"] = "Tu cuenta fue vinculada con Google.";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception exception) when (
            exception is BusinessRuleException or ArgumentException)
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

    private async Task SignInAsync(
        AuthenticatedUserDto authenticated,
        bool persistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, authenticated.Usuario.Id.ToString()),
            new(
                ClaimTypes.Name,
                $"{authenticated.Usuario.Nombre} {authenticated.Usuario.Apellido}"),
            new(ClaimTypes.Email, authenticated.Usuario.Email)
        };
        claims.AddRange(authenticated.Roles.Select(
            role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(authenticated.Permisos.Select(
            permission => new Claim("permission", permission)));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties
            {
                IsPersistent = persistent,
                AllowRefresh = true
            });
    }

    private IActionResult RedirectAfterLogin(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Home");
}
