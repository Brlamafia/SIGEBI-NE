using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SIGEBI.Application.Dtos.Catalogo;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Dtos.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Web.Controllers;
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
        controller.TempData = new TempDataDictionary(
            context,
            Mock.Of<ITempDataProvider>());
        return controller;
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
