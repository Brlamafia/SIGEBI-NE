using System.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Models.Prestamos;
using SIGEBI.Application.Services.Prestamos;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Policies;

namespace SIGEBI.Tests.Application;

public sealed class PrestamoServiceNotificationTests
{
    [Fact]
    public async Task GenerarRecordatorios_ConsultaYRegistraElLoteUnaSolaVez()
    {
        var hoy = new DateTime(2026, 7, 24, 8, 0, 0, DateTimeKind.Utc);
        var prestamo = new Prestamo(4, 2, 3, 5, 7, hoy.AddDays(-2), hoy.AddDays(2));
        AsignarId(prestamo, 12);
        var prestamos = new Mock<IPrestamoRepository>();
        prestamos.Setup(repository => repository.ObtenerActivosProximosAVencerAsync(
                hoy.Date,
                hoy.Date.AddDays(3).AddTicks(-1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([prestamo]);
        var eventos = new Mock<IPrestamoEventosService>();
        eventos.Setup(service => service.AgregarRecordatoriosSiNoExistenAsync(
                It.IsAny<IReadOnlyCollection<PrestamoRecordatorio>>(),
                hoy.Date,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var unitOfWork = UnidadDeTrabajoEjecutable();
        var service = new PrestamoMantenimientoService(
            prestamos.Object,
            Mock.Of<IResponsablePrestamoResolver>(),
            eventos.Object,
            unitOfWork.Object,
            new PoliticaPrestamos(new PoliticaPrestamosOptions
            {
                DiasAnticipacionRecordatorio = 2
            }),
            NullLogger<PrestamoMantenimientoService>.Instance);

        var enviados = await service.GenerarRecordatoriosVencimientoAsync(hoy);

        Assert.Equal(1, enviados);
        eventos.Verify(service => service.AgregarRecordatoriosSiNoExistenAsync(
            It.Is<IReadOnlyCollection<PrestamoRecordatorio>>(lote =>
                lote.Count == 1 &&
                lote.Single().PrestamoId == 12 &&
                lote.Single().UsuarioId == 4 &&
                lote.Single().Mensaje.Contains("vence el")),
            hoy.Date,
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(service => service.EjecutarEnTransaccionAsync(
            It.IsAny<Func<CancellationToken, Task>>(),
            IsolationLevel.ReadCommitted,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IUnitOfWork> UnidadDeTrabajoEjecutable()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(service => service.EjecutarEnTransaccionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                Func<CancellationToken, Task> operacion,
                IsolationLevel _,
                CancellationToken cancellationToken) => operacion(cancellationToken));
        return unitOfWork;
    }

    private static void AsignarId(
        SIGEBI.Domain.Base.EntidadBase entidad,
        int id) =>
        typeof(SIGEBI.Domain.Base.EntidadBase)
            .GetProperty(nameof(SIGEBI.Domain.Base.EntidadBase.Id))!
            .SetValue(entidad, id);
}
