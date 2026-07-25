using System.Data;
using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SIGEBI.Application.Dtos.Inventario;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Services.Inventario;
using SIGEBI.Domain.Base;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Tests.Application;

public class InventarioServiceTests
{
    [Fact]
    public async Task CambiarEstado_ActualizaConteosYRegistraHistorialConActorAutenticado()
    {
        var ejemplar = new Ejemplar(2, "EJ-2-1");
        var inventario = new Inventario(2, 1);
        AsignarId(ejemplar, 4);
        AsignarId(inventario, 6);

        var ejemplares = new Mock<IEjemplarRepository>();
        ejemplares.Setup(repository => repository.ObtenerPorIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ejemplar);
        var inventarios = new Mock<IInventarioRepository>();
        inventarios.Setup(repository => repository.ObtenerPorLibroIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventario);
        var auditoria = new Mock<IAuditoriaWriter>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.EjecutarEnTransaccionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                IsolationLevel.Serializable,
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, IsolationLevel, CancellationToken>(
                (operation, _, token) => operation(token));
        var currentUser = new Mock<IUsuarioActual>();
        currentUser.SetupGet(user => user.EstaAutenticado).Returns(true);
        currentUser.SetupGet(user => user.UsuarioId).Returns(9);
        var mapper = new Mock<IMapper>();
        mapper.Setup(value => value.Map<EjemplarDto>(ejemplar))
            .Returns(new EjemplarDto { Id = 4, LibroId = 2, Codigo = "EJ-2-1", Estado = "Reservado" });

        var service = new InventarioService(
            inventarios.Object,
            ejemplares.Object,
            Mock.Of<ILibroRepository>(),
            auditoria.Object,
            Mock.Of<IAuditoriaRepository>(),
            currentUser.Object,
            unitOfWork.Object,
            mapper.Object,
            NullLogger<InventarioService>.Instance);

        await service.CambiarEstadoEjemplarAsync(new CambiarEstadoEjemplarDto
        {
            EjemplarId = 4,
            NuevoEstado = "Reservado",
            UsuarioResponsableId = 100,
            Motivo = "Reserva administrativa"
        });

        Assert.Equal(EstadoEjemplar.Reservado, ejemplar.Estado);
        Assert.Equal(0, inventario.CantidadDisponible);
        Assert.Equal(1, inventario.CantidadReservada);
        auditoria.Verify(writer => writer.RegistrarAsync(
            9,
            ModuloAuditoria.Inventario,
            AccionAuditoria.ActualizarEstado,
            It.Is<string>(description =>
                description.Contains("Anterior=Disponible") &&
                description.Contains("Nuevo=Reservado") &&
                description.Contains("Motivo=Reserva administrativa")),
            ResultadoAuditoria.Exitoso,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static void AsignarId(EntidadBase entity, int id) =>
        typeof(EntidadBase)
            .GetProperty(nameof(EntidadBase.Id), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(entity, id);
}
