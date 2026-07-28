using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SIGEBI.API.Controllers;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Interfaces.SolicitudesPrestamo;
using SIGEBI.Application.Validations;

namespace SIGEBI.Tests.API;

public sealed class SolicitudesPrestamoControllerTests
{
    [Fact]
    public void SolicitudWeb_NoExigeUsuarioIdEnElCuerpo()
    {
        var validator = new SaveSolicitudPrestamoValidator();

        var result = validator.Validate(new SaveSolicitudPrestamoDto
        {
            LibroId = 15
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Post_AsignaElUsuarioDelTokenAntesDeRegistrar()
    {
        SaveSolicitudPrestamoDto? received = null;
        var service = new Mock<ISolicitudPrestamoService>();
        service.Setup(value => value.RegistrarSolicitudAsync(
                It.IsAny<SaveSolicitudPrestamoDto>()))
            .Callback<SaveSolicitudPrestamoDto>(dto => received = dto)
            .ReturnsAsync(true);
        var currentUser = new Mock<IUsuarioActual>();
        currentUser.SetupGet(value => value.UsuarioId).Returns(42);
        var controller = new SolicitudesPrestamoController(
            service.Object,
            currentUser.Object);

        var result = await controller.Post(new SaveSolicitudPrestamoDto
        {
            LibroId = 15
        });

        Assert.Equal(StatusCodes.Status201Created,
            Assert.IsType<StatusCodeResult>(result).StatusCode);
        Assert.NotNull(received);
        Assert.Equal(42, received.UsuarioId);
        Assert.Equal(15, received.LibroId);
    }
}
