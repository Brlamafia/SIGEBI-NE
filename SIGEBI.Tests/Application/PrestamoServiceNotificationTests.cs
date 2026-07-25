using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Notificaciones;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Services.Prestamos;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Policies;
using SIGEBI.Domain.Services;

namespace SIGEBI.Tests.Application;

public class PrestamoServiceNotificationTests
{
    [Fact]
    public async Task GenerarRecordatorios_NoDuplicaYUsaPoliticaConfigurada()
    {
        var hoy = new DateTime(2026, 7, 24, 8, 0, 0, DateTimeKind.Utc);
        var prestamo = new Prestamo(4, 2, 3, 5, 7, hoy.AddDays(-2), hoy.AddDays(2));
        var prestamos = new Mock<IPrestamoRepository>();
        prestamos.Setup(repository => repository.ObtenerActivosProximosAVencerAsync(
                hoy.Date,
                hoy.Date.AddDays(3).AddTicks(-1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([prestamo]);

        var notificaciones = new Mock<INotificacionService>();
        notificaciones.Setup(service => service.EnviarSiNoExisteAsync(
                It.IsAny<SaveNotificacionDto>(),
                It.IsAny<string>(),
                hoy.Date,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CrearServicio(
            prestamos.Object,
            notificaciones.Object,
            new PoliticaPrestamos(new PoliticaPrestamosOptions
            {
                DiasAnticipacionRecordatorio = 2
            }));

        var enviados = await service.GenerarRecordatoriosVencimientoAsync(hoy);

        Assert.Equal(1, enviados);
        notificaciones.Verify(service => service.EnviarSiNoExisteAsync(
            It.Is<SaveNotificacionDto>(dto =>
                dto.UsuarioId == 4 &&
                dto.TipoEvento == "Vencimiento" &&
                dto.Mensaje.Contains("vence el")),
            It.Is<string>(value => value.Contains("préstamo #")),
            hoy.Date,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static PrestamoService CrearServicio(
        IPrestamoRepository prestamos,
        INotificacionService notificaciones,
        PoliticaPrestamos politica) =>
        new(
            Mock.Of<ISolicitudPrestamoRepository>(),
            Mock.Of<IUsuarioRepository>(),
            Mock.Of<IEmpleadoRepository>(),
            prestamos,
            Mock.Of<IMultaRepository>(),
            Mock.Of<IInventarioRepository>(),
            Mock.Of<IEjemplarRepository>(),
            Mock.Of<IAuditoriaWriter>(),
            notificaciones,
            Mock.Of<IUsuarioActual>(),
            Mock.Of<IUnitOfWork>(),
            new PrestamoDomainService(),
            new MultaDomainService(),
            Mock.Of<IMapper>(),
            politica,
            NullLogger<PrestamoService>.Instance);
}
