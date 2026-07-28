using Microsoft.AspNetCore.Mvc;
using Moq;
using SIGEBI.API.Controllers;
using SIGEBI.Application.Dtos.Usuarios;
using SIGEBI.Application.Interfaces.Usuarios;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.Notificaciones;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Domain.Enums;

namespace SIGEBI.Tests.API;

public class UsuariosControllerTests
{
    [Fact]
    public async Task PostYPut_DevuelvenElUsuarioPersistido()
    {
        var service = new Mock<IUsuarioService>();
        var created = new UsuarioDto { Id = 42, Nombre = "Nuevo" };
        var updated = new UsuarioDto { Id = 42, Nombre = "Actualizado" };
        service.Setup(value => value.CrearAsync(
                It.IsAny<SaveUsuarioDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);
        service.Setup(value => value.ActualizarAsync(
                42,
                It.IsAny<UpdateUsuarioDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);
        var controller = new UsuariosController(
            service.Object,
            Mock.Of<IPrestamoService>(),
            Mock.Of<IMultaService>(),
            Mock.Of<INotificacionService>(),
            Mock.Of<IUsuarioActual>());

        var post = await controller.Post(
            new SaveUsuarioDto { TipoUsuario = TipoUsuario.Estudiante },
            CancellationToken.None);
        var put = await controller.Put(
            42,
            new UpdateUsuarioDto
            {
                TipoUsuario = TipoUsuario.Docente,
                Estado = EstadoUsuario.Activo
            },
            CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(post);
        Assert.Same(created, createdResult.Value);
        var ok = Assert.IsType<OkObjectResult>(put);
        Assert.Same(updated, ok.Value);
    }

    [Fact]
    public async Task GetAll_AplicaPaginaYTamanoSolicitados()
    {
        var service = new Mock<IUsuarioService>();
        service.Setup(value => value.ObtenerPaginaAsync(
                2,
                25,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var controller = new UsuariosController(
            service.Object,
            Mock.Of<IPrestamoService>(),
            Mock.Of<IMultaService>(),
            Mock.Of<INotificacionService>(),
            Mock.Of<IUsuarioActual>());

        var result = await controller.GetAll(2, 25, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(value => value.ObtenerPaginaAsync(
            2,
            25,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
