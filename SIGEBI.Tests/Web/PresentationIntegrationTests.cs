using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SIGEBI.Application.Dtos.Catalogo;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Dtos.Usuarios;
using SIGEBI.Application.Interfaces.Catalogo;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Interfaces.SolicitudesPrestamo;
using SIGEBI.Application.Interfaces.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Web.Controllers;
using SIGEBI.Web.Models;

namespace SIGEBI.Tests.Web;

public sealed class PresentationIntegrationTests
{
    [Theory]
    [InlineData(TipoUsuario.Estudiante)]
    [InlineData(TipoUsuario.Docente)]
    public void Registro_PermiteLosDosPerfilesDeLectores(TipoUsuario tipo)
    {
        var model = RegistroValido(tipo);
        var validationResults = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            validationResults,
            validateAllProperties: true);

        Assert.True(valid);
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(TipoUsuario.Administrativo)]
    [InlineData(TipoUsuario.Externo)]
    public void Registro_RechazaPerfilesInternosONoDocumentados(TipoUsuario tipo)
    {
        var model = RegistroValido(tipo);
        var validationResults = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            validationResults,
            validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(
            validationResults,
            result => result.MemberNames.Contains(nameof(model.TipoUsuario)));
    }

    [Fact]
    public async Task Registro_EntregaElPerfilDocenteALaCapaDeAplicacion()
    {
        var users = new Mock<IUsuarioService>();
        users.Setup(service => service.CrearAsync(
                It.IsAny<SaveUsuarioDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioDto());
        var controller = new AuthController(
            Mock.Of<IAuthenticationService>(),
            users.Object,
            Mock.Of<IPasswordRecoveryService>(),
            Mock.Of<IPasswordResetEmailSender>(),
            new ConfigurationBuilder().Build(),
            Mock.Of<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(),
            NullLogger<AuthController>.Instance);
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext =
            new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(
            httpContext,
            Mock.Of<ITempDataProvider>());
        var model = RegistroValido(TipoUsuario.Docente);

        var result = await controller.Register(model);

        Assert.IsType<RedirectToActionResult>(result);
        users.Verify(service => service.CrearAsync(
            It.Is<SaveUsuarioDto>(
                dto => dto.TipoUsuario == TipoUsuario.Docente),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Recuperacion_EnviaElEnlaceGeneradoMedianteSmtp()
    {
        var recovery = new Mock<IPasswordRecoveryService>();
        recovery.Setup(service => service.CreateTokenAsync(
                "ada@sigebi.test",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("token-protegido");
        var emailSender = new Mock<IPasswordResetEmailSender>();
        emailSender.SetupGet(service => service.IsConfigured).Returns(true);
        var controller = new AuthController(
            Mock.Of<IAuthenticationService>(),
            Mock.Of<IUsuarioService>(),
            recovery.Object,
            emailSender.Object,
            new ConfigurationBuilder().Build(),
            Mock.Of<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(),
            NullLogger<AuthController>.Instance);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        controller.ControllerContext =
            new ControllerContext { HttpContext = httpContext };
        var url = new Mock<IUrlHelper>();
        url.Setup(helper => helper.Action(It.IsAny<UrlActionContext>()))
            .Returns("https://localhost:7030/Auth/ResetPassword?token=token-protegido");
        controller.Url = url.Object;

        var result = await controller.ForgotPassword(
            new ForgotPasswordViewModel { Email = "ada@sigebi.test" });

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("ForgotPasswordConfirmation", view.ViewName);
        emailSender.Verify(service => service.SendAsync(
            "ada@sigebi.test",
            "https://localhost:7030/Auth/ResetPassword?token=token-protegido",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Catalogo_ObtieneDatosDesdeServicioDeAplicacion()
    {
        var libros = new Mock<ILibroService>();
        var solicitudes = new Mock<ISolicitudPrestamoService>();
        libros.Setup(service => service.BuscarLibrosAsync(
                "clean",
                null,
                null,
                true,
                0,
                12,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new LibroDto
                {
                    Id = 10,
                    Titulo = "Clean Architecture",
                    Autor = "Robert C. Martin",
                    CantidadDisponible = 2
                }
            ]);
        libros.Setup(service => service.GetAllAsync())
            .ReturnsAsync([
                new LibroDto
                {
                    Id = 10,
                    Titulo = "Clean Architecture",
                    Genero = "Tecnología",
                    Editorial = "Prentice Hall"
                }
            ]);
        solicitudes
            .Setup(service => service.ObtenerPorUsuarioAsync(
                It.IsAny<int>()))
            .ReturnsAsync([]);
        var controller = new CatalogoController(
            libros.Object,
            solicitudes.Object,
            Mock.Of<IMultaService>(),
            Mock.Of<IPrestamoService>(),
            Mock.Of<IUsuarioActual>(),
            NullLogger<CatalogoController>.Instance);

        var result = Assert.IsType<ViewResult>(await controller.Index(
            "clean",
            null,
            null,
            true));
        var model = Assert.IsType<CatalogoViewModel>(result.Model);

        Assert.Single(model.Libros);
        Assert.Equal("Clean Architecture", model.Libros.Single().Titulo);
        Assert.Contains("Tecnología", model.GenerosDisponibles);
        Assert.Contains("Prentice Hall", model.EditorialesDisponibles);
    }

    [Fact]
    public async Task Catalogo_MuestraComoPendientesLosLibrosYaSolicitados()
    {
        var libros = new Mock<ILibroService>();
        libros.Setup(service => service.BuscarLibrosAsync(
                null,
                null,
                null,
                null,
                0,
                12,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new LibroDto { Id = 18, Titulo = "El principito" }]);
        libros.Setup(service => service.GetAllAsync())
            .ReturnsAsync([new LibroDto { Id = 18, Titulo = "El principito" }]);
        var solicitudes = new Mock<ISolicitudPrestamoService>();
        solicitudes.Setup(service => service.ObtenerPorUsuarioAsync(7))
            .ReturnsAsync([
                new SolicitudPrestamoDto
                {
                    UsuarioId = 7,
                    LibroId = 18,
                    Estado = "Pendiente"
                }
            ]);
        var usuarioActual = new Mock<IUsuarioActual>();
        usuarioActual.SetupGet(service => service.UsuarioId).Returns(7);
        var controller = new CatalogoController(
            libros.Object,
            solicitudes.Object,
            Mock.Of<IMultaService>(),
            Mock.Of<IPrestamoService>(),
            usuarioActual.Object,
            NullLogger<CatalogoController>.Instance);

        var result = Assert.IsType<ViewResult>(await controller.Index(
            null,
            null,
            null,
            null));
        var model = Assert.IsType<CatalogoViewModel>(result.Model);

        Assert.Contains(18, model.LibrosConSolicitudPendiente);
    }

    [Theory]
    [InlineData(typeof(HomeController))]
    [InlineData(typeof(CatalogoController))]
    [InlineData(typeof(SolicitudesController))]
    [InlineData(typeof(PrestamosController))]
    [InlineData(typeof(MultasController))]
    [InlineData(typeof(NotificacionesController))]
    [InlineData(typeof(CuentaController))]
    public void ModulosPrivados_RequierenAutenticacion(Type controllerType)
    {
        Assert.NotNull(controllerType
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .SingleOrDefault());
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
