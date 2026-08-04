using System.Data;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Notificaciones;
using SIGEBI.Application.Services.Prestamos;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Policies;

namespace SIGEBI.Tests.Application;

public sealed class SolicitudPrestamoServiceTests
{
    [Fact]
    public async Task RegistrarSolicitud_RechazaUsuarioConMultasPendientes()
    {
        var user = new Usuario(
            "Luis", "Pérez", "001", "luis@sigebi.test", TipoUsuario.Estudiante);
        var book = new Libro("Libro", "Autor", "ISBN-1", "Novela", "Editorial");
        AssignId(user, 5);
        AssignId(book, 8);
        var requests = new Mock<ISolicitudPrestamoRepository>();
        var books = new Mock<ILibroRepository>();
        books.Setup(item => item.ObtenerPorIdAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        var users = new Mock<IUsuarioRepository>();
        users.Setup(item => item.ObtenerPorIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var inventory = new Mock<IInventarioRepository>();
        inventory.Setup(item => item.ObtenerPorLibroIdAsync(
                8,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Inventario(8, 1));
        var fines = new Mock<IMultaRepository>();
        fines.Setup(item => item.TienePendientesPorUsuarioAsync(
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var unitOfWork = ExecutingUnitOfWork();
        var service = CreateService(
            requests.Object,
            books.Object,
            users.Object,
            inventory.Object,
            fines.Object,
            unitOfWork.Object);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RegistrarSolicitudAsync(new SaveSolicitudPrestamoDto
            {
                UsuarioId = 5,
                LibroId = 8
            }));

        Assert.Contains("multas pendientes", exception.Message);
        requests.Verify(item => item.AgregarAsync(
            It.IsAny<SolicitudPrestamo>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelarSolicitud_RechazaSolicitudDeOtroUsuario()
    {
        var request = new SolicitudPrestamo(5, 8);
        AssignId(request, 12);
        var requests = new Mock<ISolicitudPrestamoRepository>();
        requests.Setup(item => item.ObtenerPorIdAsync(
                12,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        var service = CreateService(
            requests.Object,
            Mock.Of<ILibroRepository>(),
            Mock.Of<IUsuarioRepository>(),
            Mock.Of<IInventarioRepository>(),
            Mock.Of<IMultaRepository>(),
            ExecutingUnitOfWork().Object);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CancelarAsync(12, 99));

        Assert.Contains("no pertenece", exception.Message);
        requests.Verify(item => item.Actualizar(
            It.IsAny<SolicitudPrestamo>()), Times.Never);
    }

    private static SolicitudPrestamoService CreateService(
        ISolicitudPrestamoRepository requests,
        ILibroRepository books,
        IUsuarioRepository users,
        IInventarioRepository inventory,
        IMultaRepository fines,
        IUnitOfWork unitOfWork) =>
        new(
            requests,
            books,
            users,
            Mock.Of<INotificacionService>(),
            fines,
            Mock.Of<IPrestamoRepository>(),
            inventory,
            Mock.Of<IAuditoriaWriter>(),
            unitOfWork,
            new PoliticaPrestamos(),
            Mock.Of<IMapper>(),
            NullLogger<SolicitudPrestamoService>.Instance);

    private static Mock<IUnitOfWork> ExecutingUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(item => item.EjecutarEnTransaccionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                Func<CancellationToken, Task> operation,
                IsolationLevel _,
                CancellationToken cancellationToken) =>
                operation(cancellationToken));
        return unitOfWork;
    }

    private static void AssignId(SIGEBI.Domain.Base.EntidadBase entity, int id) =>
        typeof(SIGEBI.Domain.Base.EntidadBase)
            .GetProperty(nameof(SIGEBI.Domain.Base.EntidadBase.Id))!
            .SetValue(entity, id);
}
