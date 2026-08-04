using Microsoft.AspNetCore.Mvc;
using Moq;
using SIGEBI.API.Controllers;
using SIGEBI.Application.Dtos.Catalogo;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Dtos.Reportes;
using SIGEBI.Application.Interfaces.Catalogo;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.Usuarios;

namespace SIGEBI.Tests.API;

public sealed class ReportesControllerTests
{
    [Fact]
    public async Task Catalogo_CalculaDemandaConTodosLosRecursosAntesDelTopDiez()
    {
        var desde = DateTime.UtcNow.AddDays(-30);
        var hasta = DateTime.UtcNow;
        var libros = Enumerable.Range(1, 11)
            .Select(id => new LibroDto
            {
                Id = id,
                Titulo = $"Libro {id}",
                Autor = "Autor",
                Genero = "Tecnología",
                CantidadTotal = 2,
                CantidadDisponible = 1
            })
            .ToArray();
        var prestamos = Enumerable.Range(1, 11)
            .Select(id => new PrestamoDto
            {
                Id = id,
                LibroId = id,
                FechaPrestamo = desde.AddDays(1),
                FechaEsperadaDevolucion = hasta.AddDays(1),
                Estado = "Activo"
            })
            .ToArray();
        var catalogo = new Mock<ILibroService>();
        catalogo.Setup(x => x.BuscarLibrosAsync(
                null,
                null,
                null,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(libros);
        var servicioPrestamos = new Mock<IPrestamoService>();
        servicioPrestamos.Setup(x => x.ObtenerPorRangoAsync(
                desde, hasta, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prestamos);
        var controller = new ReportesController(
            servicioPrestamos.Object,
            catalogo.Object,
            Mock.Of<IMultaService>(),
            Mock.Of<IUsuarioService>());

        var action = await controller.GetReporteCatalogo(desde, hasta, CancellationToken.None);
        var reporte = Assert.IsType<ReporteCatalogoDto>(
            Assert.IsType<OkObjectResult>(action).Value);

        Assert.Equal(10, reporte.RecursosMasSolicitados.Count);
        Assert.Equal(11, Assert.Single(reporte.DemandaPorCategoria).Prestamos);
        Assert.Equal(50m, reporte.DisponibilidadPromedioPorcentaje);
    }
}
