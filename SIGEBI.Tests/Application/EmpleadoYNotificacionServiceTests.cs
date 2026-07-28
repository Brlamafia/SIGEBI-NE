using System.Data;
using AutoMapper;
using Moq;
using SIGEBI.Application.Dtos.Empleados;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Services.Empleados;
using SIGEBI.Application.Services.Notificaciones;
using SIGEBI.Domain.Entities.Notificaciones;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Tests.Application;

public sealed class EmpleadoYNotificacionServiceTests
{
    [Fact]
    public async Task CrearEmpleado_EsAtomicoYRegistraAuditoria()
    {
        var repositorio = new Mock<IRepository<Empleado>>();
        var empleados = new Mock<IEmpleadoRepository>();
        var usuarios = new Mock<IUsuarioRepository>();
        var cargos = new Mock<ICargoRepository>();
        var auditoria = new Mock<IAuditoriaWriter>();
        var usuarioActual = UsuarioActual(99);
        var unitOfWork = UnitOfWorkSerializable();
        var mapper = new Mock<IMapper>();
        var usuario = new Usuario(
            "María", "López", "001", "maria@sigebi.test", TipoUsuario.Administrativo);
        var cargo = new Cargo("Bibliotecario");

        usuarios.Setup(x => x.ObtenerPorIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        cargos.Setup(x => x.ObtenerPorIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cargo);
        mapper.Setup(x => x.Map<EmpleadoDto>(It.IsAny<Empleado>()))
            .Returns<Empleado>(e => new EmpleadoDto { UsuarioId = e.UsuarioId, CargoId = e.CargoId });

        var servicio = new EmpleadoService(
            repositorio.Object,
            empleados.Object,
            usuarios.Object,
            cargos.Object,
            auditoria.Object,
            usuarioActual.Object,
            unitOfWork.Object,
            mapper.Object);

        var resultado = await servicio.CrearAsync(new SaveEmpleadoDto
        {
            UsuarioId = 7,
            CargoId = 3
        });

        Assert.Equal(7, resultado.UsuarioId);
        Assert.Equal(3, resultado.CargoId);
        repositorio.Verify(
            x => x.AgregarAsync(It.Is<Empleado>(e => e.UsuarioId == 7), It.IsAny<CancellationToken>()),
            Times.Once);
        auditoria.Verify(
            x => x.RegistrarAsync(
                99,
                ModuloAuditoria.Administracion,
                AccionAuditoria.Registrar,
                It.IsAny<string>(),
                ResultadoAuditoria.Exitoso,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnviarNotificacionManual_EsAtomicoYRegistraAuditoria()
    {
        var repositorio = new Mock<INotificacionRepository>();
        var auditoria = new Mock<IAuditoriaWriter>();
        var usuarioActual = UsuarioActual(99);
        var unitOfWork = UnitOfWork();
        var mapper = new Mock<IMapper>();
        var entidad = new Notificacion(7, "Aviso de biblioteca");
        var dto = new NotificacionDto
        {
            Id = 1,
            UsuarioId = 7,
            Mensaje = entidad.Mensaje,
            FechaEnvio = entidad.FechaEnvio,
            Leida = false,
            TipoEvento = "Informacion"
        };
        mapper.Setup(x => x.Map<Notificacion>(It.IsAny<SaveNotificacionDto>()))
            .Returns(entidad);
        mapper.Setup(x => x.Map<NotificacionDto>(entidad)).Returns(dto);

        var servicio = new NotificacionService(
            repositorio.Object,
            auditoria.Object,
            usuarioActual.Object,
            unitOfWork.Object,
            mapper.Object);

        var resultado = await servicio.AddAsync(new SaveNotificacionDto
        {
            UsuarioId = 7,
            Mensaje = "Aviso de biblioteca"
        });

        Assert.Equal(7, resultado.UsuarioId);
        repositorio.Verify(
            x => x.AgregarAsync(entidad, It.IsAny<CancellationToken>()),
            Times.Once);
        auditoria.Verify(
            x => x.RegistrarAsync(
                99,
                ModuloAuditoria.Notificaciones,
                AccionAuditoria.Registrar,
                It.IsAny<string>(),
                ResultadoAuditoria.Exitoso,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Mock<IUsuarioActual> UsuarioActual(int usuarioId)
    {
        var actual = new Mock<IUsuarioActual>();
        actual.SetupGet(x => x.EstaAutenticado).Returns(true);
        actual.SetupGet(x => x.UsuarioId).Returns(usuarioId);
        return actual;
    }

    private static Mock<IUnitOfWork> UnitOfWorkSerializable()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.EjecutarEnTransaccionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task> operation, IsolationLevel _, CancellationToken ct) =>
                operation(ct));
        return unitOfWork;
    }

    private static Mock<IUnitOfWork> UnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.EjecutarEnTransaccionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task> operation, CancellationToken ct) =>
                operation(ct));
        return unitOfWork;
    }
}
