using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SIGEBI.Application.Dtos.Catalogo;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Dtos.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Web.Controllers;
using SIGEBI.Web.Filters;
using SIGEBI.Web.Models;
using SIGEBI.Web.Services;

namespace SIGEBI.Tests.Web;

public sealed class WebApiPresentationTests
{
    [Theory]
    [InlineData(TipoUsuario.Estudiante)]
    [InlineData(TipoUsuario.Docente)]
    public void Registro_PermiteLosDosPerfilesDeLectores(TipoUsuario tipo)
    {
        var model = RegistroValido(tipo);
        var results = new List<ValidationResult>();
        Assert.True(Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            results,
            validateAllProperties: true));
        Assert.Empty(results);
    }

    [Fact]
    public async Task Registro_EnviaLosDatosALaApi()
    {
        var api = new Mock<ISigebiApiClient>();
        var controller = CreateAuthController(api.Object);
        var model = RegistroValido(TipoUsuario.Docente);

        var result = await controller.Register(model);

        Assert.IsType<RedirectToActionResult>(result);
        api.Verify(client => client.RegisterAsync(
            It.Is<SaveUsuarioDto>(dto =>
                dto.Email == model.Email &&
                dto.TipoUsuario == TipoUsuario.Docente),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Catalogo_ConsultaApiYMarcaSolicitudesPendientes()
    {
        var api = new Mock<ISigebiApiClient>();
        api.Setup(client => client.SearchBooksAsync(
                "clean", null, null, true, 1, 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new LibroDto
                {
                    Id = 10,
                    Titulo = "Clean Architecture",
                    Genero = "Tecnología",
                    Editorial = "Prentice Hall"
                }
            ]);
        api.Setup(client => client.GetBooksAsync(
                1, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new LibroDto
                {
                    Id = 10,
                    Titulo = "Clean Architecture",
                    Genero = "Tecnología",
                    Editorial = "Prentice Hall"
                }
            ]);
        api.Setup(client => client.GetMyRequestsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new SolicitudPrestamoDto
                {
                    UsuarioId = 7,
                    LibroId = 10,
                    Estado = "Pendiente"
                }
            ]);
        api.Setup(client => client.GetMySummaryAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MySummary());
        var controller = new CatalogoController(
            api.Object,
            NullLogger<CatalogoController>.Instance);

        var result = Assert.IsType<ViewResult>(await controller.Index(
            "clean", null, null, true));
        var model = Assert.IsType<CatalogoViewModel>(result.Model);

        Assert.Single(model.Libros);
        Assert.Contains(10, model.LibrosConSolicitudPendiente);
        Assert.Contains("Tecnología", model.GenerosDisponibles);
    }

    [Fact]
    public async Task Inicio_CuentaTodasLasNotificacionesSinLeer()
    {
        var api = new Mock<ISigebiApiClient>();
        api.Setup(client => client.GetMySummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MySummary
            {
                Usuario = new UsuarioDto { Nombre = "Ada" },
                Notificaciones = Enumerable.Range(1, 8)
                    .Select(id => new NotificacionDto
                    {
                        Id = id,
                        UsuarioId = 7,
                        Mensaje = $"Aviso {id}",
                        FechaEnvio = DateTime.UtcNow,
                        Leida = id > 6
                    })
                    .ToArray()
            });
        api.Setup(client => client.GetMyRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        api.Setup(client => client.GetBooksAsync(
                1, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var controller = new HomeController(api.Object);

        var result = Assert.IsType<ViewResult>(await controller.Index(
            CancellationToken.None));
        var model = Assert.IsType<DashboardViewModel>(result.Model);

        Assert.Equal(6, model.NotificacionesSinLeer);
    }

    [Fact]
    public async Task Catalogo_ConsultaLaPaginaSiguienteSinOmitirRegistros()
    {
        var api = new Mock<ISigebiApiClient>();
        api.Setup(client => client.SearchBooksAsync(
                null, null, null, null, 1, 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Range(1, 12)
                .Select(id => new LibroDto { Id = id, Titulo = $"Libro {id}" })
                .ToArray());
        api.Setup(client => client.SearchBooksAsync(
                null, null, null, null, 2, 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new LibroDto { Id = 13, Titulo = "Libro 13" }]);
        api.Setup(client => client.GetMyRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        api.Setup(client => client.GetMySummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MySummary());
        api.Setup(client => client.GetBooksAsync(
                1, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var controller = new CatalogoController(
            api.Object,
            NullLogger<CatalogoController>.Instance);

        var result = Assert.IsType<ViewResult>(await controller.Index(
            null, null, null, null));
        var model = Assert.IsType<CatalogoViewModel>(result.Model);

        Assert.Equal(12, model.Libros.Count);
        Assert.True(model.HayPaginaSiguiente);
        Assert.Equal(12, model.Libros.Max(item => item.Id));
    }

    [Fact]
    public async Task SolicitudValida_CompletaElFlujoVistaApiVista()
    {
        var api = new Mock<ISigebiApiClient>();
        var controller = new CatalogoController(
            api.Object,
            NullLogger<CatalogoController>.Instance);
        ConfigurePresentationContext(controller);

        var result = await controller.Solicitar(
            new SolicitarLibroViewModel { LibroId = 10 },
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CatalogoController.Index), redirect.ActionName);
        Assert.Equal("La solicitud de préstamo fue registrada.",
            controller.TempData["Success"]);
        api.Verify(client => client.CreateRequestAsync(
            10,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SolicitudInvalida_RegresaElMensajeDeLaApiALaVista()
    {
        var api = new Mock<ISigebiApiClient>();
        api.Setup(client => client.CreateRequestAsync(
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SigebiApiException(
                "El usuario tiene una multa pendiente.",
                StatusCodes.Status409Conflict));
        var controller = new CatalogoController(
            api.Object,
            NullLogger<CatalogoController>.Instance);
        ConfigurePresentationContext(controller);

        var result = await controller.Solicitar(
            new SolicitarLibroViewModel { LibroId = 10 },
            CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("El usuario tiene una multa pendiente.",
            controller.TempData["Error"]);
    }

    [Fact]
    public async Task DetalleLibro_ConsultaApiYConstruyeViewModel()
    {
        var api = new Mock<ISigebiApiClient>();
        api.Setup(client => client.GetBookByIdAsync(
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LibroDto
            {
                Id = 10,
                Titulo = "Clean Architecture",
                Descripcion = "Presenta principios para diseñar sistemas mantenibles.",
                CantidadTotal = 3,
                CantidadDisponible = 2
            });
        api.Setup(client => client.GetMyRequestsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new SolicitudPrestamoDto
                {
                    Id = 4,
                    LibroId = 10,
                    Estado = "Pendiente"
                }
            ]);
        api.Setup(client => client.GetMySummaryAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MySummary());
        var controller = new CatalogoController(
            api.Object,
            NullLogger<CatalogoController>.Instance);

        var result = Assert.IsType<ViewResult>(
            await controller.Details(10));
        var model = Assert.IsType<CatalogoDetalleViewModel>(result.Model);

        Assert.Equal("Clean Architecture", model.Libro.Titulo);
        Assert.Equal(3, model.Libro.CantidadTotal);
        Assert.NotEmpty(model.Libro.Descripcion);
        Assert.True(model.SolicitudPendiente);
        Assert.Null(model.RestriccionSolicitud);
    }

    [Fact]
    public async Task DetalleLibro_IdentificadorInvalidoDevuelveNoEncontrado()
    {
        var api = new Mock<ISigebiApiClient>();
        var controller = new CatalogoController(
            api.Object,
            NullLogger<CatalogoController>.Instance);

        var result = await controller.Details(0);

        Assert.IsType<NotFoundResult>(result);
        api.Verify(client => client.GetBookByIdAsync(
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SolicitudDesdeDetalle_RegresaAlLibroSeleccionado()
    {
        var api = new Mock<ISigebiApiClient>();
        var controller = new CatalogoController(
            api.Object,
            NullLogger<CatalogoController>.Instance);
        ConfigurePresentationContext(controller);

        var result = await controller.Solicitar(
            new SolicitarLibroViewModel
            {
                LibroId = 10,
                VolverAlDetalle = true
            },
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CatalogoController.Details), redirect.ActionName);
        Assert.Equal(10, redirect.RouteValues?["id"]);
    }

    [Fact]
    public async Task Cancelacion_UsaViewModelYDelegaEnLaApi()
    {
        var api = new Mock<ISigebiApiClient>();
        var controller = new SolicitudesController(
            api.Object,
            NullLogger<SolicitudesController>.Instance);
        ConfigurePresentationContext(controller);

        var result = await controller.Cancelar(
            new CancelarSolicitudViewModel { Id = 8 });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("La solicitud fue cancelada.", controller.TempData["Success"]);
        api.Verify(client => client.CancelRequestAsync(
            8,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(typeof(SolicitarLibroViewModel))]
    [InlineData(typeof(CancelarSolicitudViewModel))]
    [InlineData(typeof(MarcarNotificacionViewModel))]
    public void AccionesConIdentificadorInvalido_NoSuperanValidacion(
        Type modelType)
    {
        var model = Activator.CreateInstance(modelType)!;
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            results,
            validateAllProperties: true);

        Assert.False(valid);
        Assert.NotEmpty(results);
    }

    [Theory]
    [InlineData("solo-minusculas1")]
    [InlineData("SOLO-MAYUSCULAS1")]
    [InlineData("SinNumeros")]
    public void Registro_RechazaPasswordSinLaComplejidadRequerida(string password)
    {
        var model = RegistroValido(TipoUsuario.Estudiante);
        model.Password = password;
        model.ConfirmPassword = password;
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            results,
            validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(RegisterViewModel.Password)));
    }

    [Fact]
    public async Task NotificacionLeida_RegresaALaPaginaDeOrigen()
    {
        var api = new Mock<ISigebiApiClient>();
        var controller = new NotificacionesController(api.Object);
        ConfigurePresentationContext(controller);

        var result = await controller.MarcarLeida(
            new MarcarNotificacionViewModel { Id = 4, Pagina = 3 });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(3, redirect.RouteValues?["pagina"]);
        api.Verify(client => client.MarkNotificationReadAsync(
            4,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Notificaciones_CompruebaLaPaginaSiguienteSinAlterarElTamano()
    {
        var api = new Mock<ISigebiApiClient>();
        api.Setup(client => client.GetMyNotificationsAsync(
                1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Range(1, 20)
                .Select(id => new NotificacionDto
                {
                    Id = id,
                    UsuarioId = 7,
                    Mensaje = $"Aviso {id}",
                    FechaEnvio = DateTime.UtcNow,
                    Leida = false
                })
                .ToArray());
        api.Setup(client => client.GetMyNotificationsAsync(
                2, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new NotificacionDto
                {
                    Id = 21,
                    UsuarioId = 7,
                    Mensaje = "Aviso 21",
                    FechaEnvio = DateTime.UtcNow,
                    Leida = false
                }
            ]);
        var controller = new NotificacionesController(api.Object);

        var result = Assert.IsType<ViewResult>(await controller.Index());
        var model = Assert.IsType<NotificacionesViewModel>(result.Model);

        Assert.Equal(20, model.Notificaciones.Count);
        Assert.True(model.HayPaginaSiguiente);
    }

    [Fact]
    public async Task Google_SoloCompletaRegistroCuandoLaCuentaNoExiste()
    {
        var api = new Mock<ISigebiApiClient>();
        api.Setup(client => client.ExternalLoginAsync(
                "ada@sigebi.test",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SigebiApiException(
                "La sesión no es válida.",
                StatusCodes.Status401Unauthorized,
                responseDetail:
                    "No existe una cuenta activa de SIGEBI asociada a este correo."));
        var controller = CreateAuthController(api.Object);
        ConfigureExternalIdentity(controller, "ada@sigebi.test");

        var result = await controller.GoogleCallback();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AuthController.CompleteGoogleRegistration),
            redirect.ActionName);
    }

    [Fact]
    public async Task Google_ErrorDeTransporte_RegresaAlLogin()
    {
        var api = new Mock<ISigebiApiClient>();
        api.Setup(client => client.ExternalLoginAsync(
                "ada@sigebi.test",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SigebiApiException(
                "No fue posible comunicarse con la API de SIGEBI.",
                StatusCodes.Status503ServiceUnavailable));
        var controller = CreateAuthController(api.Object);
        var authentication = ConfigureExternalIdentity(
            controller,
            "ada@sigebi.test");

        var result = await controller.GoogleCallback();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AuthController.Login), redirect.ActionName);
        authentication.Verify(service => service.SignOutAsync(
            It.IsAny<HttpContext>(),
            "External",
            It.IsAny<AuthenticationProperties>()), Times.Once);
    }

    [Fact]
    public void ApiNoDisponible_SeRenderizaComoErrorControlado()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());
        var exceptionContext = new ExceptionContext(actionContext, [])
        {
            Exception = new SigebiApiException(
                "No fue posible comunicarse con la API de SIGEBI.",
                StatusCodes.Status503ServiceUnavailable)
        };
        var filter = new ApiExceptionFilter(
            NullLogger<ApiExceptionFilter>.Instance);

        filter.OnException(exceptionContext);

        var result = Assert.IsType<ViewResult>(exceptionContext.Result);
        var model = Assert.IsType<ApiErrorViewModel>(result.Model);
        Assert.True(exceptionContext.ExceptionHandled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable,
            httpContext.Response.StatusCode);
        Assert.Equal("Servicio temporalmente no disponible", model.Title);
    }

    [Fact]
    public void SesionApiInvalida_EliminaLaCookieYRegresaAlLogin()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());
        var exceptionContext = new ExceptionContext(actionContext, [])
        {
            Exception = new SigebiApiException(
                "La sesión dejó de ser válida.",
                StatusCodes.Status401Unauthorized)
        };
        var filter = new ApiExceptionFilter(
            NullLogger<ApiExceptionFilter>.Instance);

        filter.OnException(exceptionContext);

        var redirect = Assert.IsType<RedirectToActionResult>(
            exceptionContext.Result);
        Assert.True(exceptionContext.ExceptionHandled);
        Assert.Equal("Auth", redirect.ControllerName);
        Assert.Equal(nameof(AuthController.Login), redirect.ActionName);
        Assert.Equal(true, redirect.RouteValues!["sessionExpired"]);
        Assert.Contains(
            "SIGEBI.Web.Session=",
            httpContext.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public void PermisoApiDenegado_MuestraLaVistaDeAccesoDenegado()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());
        var exceptionContext = new ExceptionContext(actionContext, [])
        {
            Exception = new SigebiApiException(
                "Acceso denegado.",
                StatusCodes.Status403Forbidden)
        };
        var filter = new ApiExceptionFilter(
            NullLogger<ApiExceptionFilter>.Instance);

        filter.OnException(exceptionContext);

        var redirect = Assert.IsType<RedirectToActionResult>(
            exceptionContext.Result);
        Assert.True(exceptionContext.ExceptionHandled);
        Assert.Equal("Auth", redirect.ControllerName);
        Assert.Equal(nameof(AuthController.AccesoDenegado), redirect.ActionName);
    }

    [Fact]
    public async Task Inicio_IniciaLasConsultasIndependientesEnParalelo()
    {
        var summarySource = new TaskCompletionSource<MySummary>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var requestsSource = new TaskCompletionSource<
            IReadOnlyCollection<SolicitudPrestamoDto>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var booksSource = new TaskCompletionSource<
            IReadOnlyCollection<LibroDto>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var api = new Mock<ISigebiApiClient>();
        api.Setup(client => client.GetMySummaryAsync(
                It.IsAny<CancellationToken>()))
            .Returns(summarySource.Task);
        api.Setup(client => client.GetMyRequestsAsync(
                It.IsAny<CancellationToken>()))
            .Returns(requestsSource.Task);
        api.Setup(client => client.GetBooksAsync(
                1,
                200,
                It.IsAny<CancellationToken>()))
            .Returns(booksSource.Task);
        var controller = new HomeController(api.Object);

        var actionTask = controller.Index(CancellationToken.None);

        api.Verify(client => client.GetMySummaryAsync(
            It.IsAny<CancellationToken>()), Times.Once);
        api.Verify(client => client.GetMyRequestsAsync(
            It.IsAny<CancellationToken>()), Times.Once);
        api.Verify(client => client.GetBooksAsync(
            1,
            200,
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(actionTask.IsCompleted);

        summarySource.SetResult(new MySummary
        {
            Usuario = new UsuarioDto { Nombre = "Ada" }
        });
        requestsSource.SetResult([]);
        booksSource.SetResult([]);

        Assert.IsType<ViewResult>(await actionTask);
    }

    [Theory]
    [InlineData(typeof(HomeController))]
    [InlineData(typeof(CatalogoController))]
    [InlineData(typeof(SolicitudesController))]
    [InlineData(typeof(PrestamosController))]
    [InlineData(typeof(MultasController))]
    [InlineData(typeof(NotificacionesController))]
    [InlineData(typeof(CuentaController))]
    public void ModulosPrivados_RequierenAutenticacion(Type controllerType) =>
        Assert.NotNull(controllerType
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .SingleOrDefault());

    private static AuthController CreateAuthController(ISigebiApiClient api)
    {
        var controller = new AuthController(
            api,
            new ConfigurationBuilder().Build(),
            NullLogger<AuthController>.Instance);
        var context = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        controller.Url = Mock.Of<IUrlHelper>();
        controller.TempData = new TempDataDictionary(
            context,
            Mock.Of<ITempDataProvider>());
        return controller;
    }

    private static void ConfigurePresentationContext(Controller controller)
    {
        var context = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };
        controller.TempData = new TempDataDictionary(
            context,
            Mock.Of<ITempDataProvider>());
    }

    private static Mock<IAuthenticationService> ConfigureExternalIdentity(
        Controller controller,
        string email)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, email)],
            "External"));
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(service => service.AuthenticateAsync(
                It.IsAny<HttpContext>(),
                "External"))
            .ReturnsAsync(AuthenticateResult.Success(
                new AuthenticationTicket(principal, "External")));
        authentication.Setup(service => service.SignOutAsync(
                It.IsAny<HttpContext>(),
                "External",
                It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);
        var services = new ServiceCollection()
            .AddSingleton(authentication.Object)
            .BuildServiceProvider();
        controller.HttpContext.RequestServices = services;
        return authentication;
    }

    private static RegisterViewModel RegistroValido(TipoUsuario tipo) =>
        new()
        {
            TipoUsuario = tipo,
            Nombre = "Ada",
            Apellido = "Lovelace",
            Cedula = "00112345678",
            Telefono = "8095550101",
            Email = "ada@sigebi.test",
            Password = "Clave123",
            ConfirmPassword = "Clave123"
        };
}
