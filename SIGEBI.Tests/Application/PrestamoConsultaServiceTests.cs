using AutoMapper;
using Moq;
using SIGEBI.Application.Common;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Services.Prestamos;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Tests.Application;

public sealed class PrestamoConsultaServiceTests
{
    [Fact]
    public async Task ObtenerPorRango_NormalizaFechasSinZonaAntesDeConsultarPostgres()
    {
        var repositorio = new Mock<IPrestamoRepository>();
        repositorio.Setup(value => value.ObtenerPorRangoAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var mapper = new Mock<IMapper>();
        mapper.Setup(value => value.Map<IReadOnlyCollection<PrestamoDto>>(
                It.IsAny<object>()))
            .Returns([]);
        var servicio = new PrestamoConsultaService(
            repositorio.Object,
            mapper.Object);
        var desde = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var hasta = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Unspecified);

        await servicio.ObtenerPorRangoAsync(desde, hasta);

        repositorio.Verify(value => value.ObtenerPorRangoAsync(
            DateTimeNormalizer.ToUtc(desde),
            DateTimeNormalizer.ToUtc(hasta),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
