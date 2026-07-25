using Microsoft.AspNetCore.Mvc;
using Moq;
using SIGEBI.API.Controllers;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Interfaces.Prestamos;

namespace SIGEBI.Tests.API;

public class PrestamosControllerTests
{
    [Fact]
    public async Task ObtenerPorLibro_DevuelveElHistorialDelRecurso()
    {
        var expected = new[] { new PrestamoDto { Id = 12, LibroId = 7 } };
        var service = new Mock<IPrestamoService>();
        service.Setup(value => value.ObtenerPorLibroAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new PrestamosController(service.Object);

        var response = await controller.ObtenerPorLibro(7, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Same(expected, ok.Value);
    }
}
