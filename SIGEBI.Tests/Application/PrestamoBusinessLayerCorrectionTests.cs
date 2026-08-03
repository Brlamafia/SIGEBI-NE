using System.Data;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Models.Prestamos;
using SIGEBI.Application.Services.Prestamos;
using SIGEBI.Domain.Base;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Notificaciones;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Policies;
using SIGEBI.Domain.Services;
using AuditoriaEntidad = SIGEBI.Domain.Entities.Auditoria.Auditoria;

namespace SIGEBI.Tests.Application;

public sealed class PrestamoBusinessLayerCorrectionTests
{
    [Fact]
    public void PrestamoService_EsUnaFachadaConDependenciasAcotadas()
    {
        var constructor = typeof(PrestamoService).GetConstructors().Single();

        Assert.Equal(6, constructor.GetParameters().Length);
        Assert.All(
            constructor.GetParameters(),
            parameter => Assert.True(parameter.ParameterType.IsInterface));
    }

    [Fact]
    public async Task ActualizarVencidos_UsaReadCommittedYRegistraUnSoloLote()
    {
        var fecha = new DateTime(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);
        var prestamo1 = CrearPrestamo(1, fecha, 20);
        var prestamo2 = CrearPrestamo(2, fecha, 21);
        var prestamos = new Mock<IPrestamoRepository>();
        prestamos.Setup(repository => repository.ObtenerActivosVencidosAsync(
                fecha,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([prestamo1, prestamo2]);
        var responsables = new Mock<IResponsablePrestamoResolver>();
        responsables.Setup(service => service.ResolverUsuario(99)).Returns(42);
        var eventos = new Mock<IPrestamoEventosService>();
        var unitOfWork = UnidadDeTrabajoEjecutable();
        var service = new PrestamoMantenimientoService(
            prestamos.Object,
            responsables.Object,
            eventos.Object,
            unitOfWork.Object,
            new PoliticaPrestamos(),
            NullLogger<PrestamoMantenimientoService>.Instance);

        var actualizados = await service.ActualizarPrestamosVencidosAsync(
            new ActualizarPrestamosVencidosDto
            {
                FechaReferencia = fecha,
                UsuarioResponsableId = 99
            });

        Assert.Equal(2, actualizados);
        Assert.Equal(EstadoPrestamo.Vencido, prestamo1.Estado);
        Assert.Equal(EstadoPrestamo.Vencido, prestamo2.Estado);
        eventos.Verify(service => service.AgregarRangoAsync(
            It.Is<IReadOnlyCollection<PrestamoEventoAplicacion>>(lote =>
                lote.Count == 2 &&
                lote.All(evento => evento.UsuarioResponsableId == 42)),
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(service => service.EjecutarEnTransaccionAsync(
            It.IsAny<Func<CancellationToken, Task>>(),
            IsolationLevel.ReadCommitted,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistroContexto_BloqueaElEjemplarDisponiblePorFila()
    {
        var solicitud = new SolicitudPrestamo(5, 8);
        var usuario = new Usuario(
            "Ana",
            "Pérez",
            "001",
            "ana@sigebi.test",
            TipoUsuario.Estudiante);
        var empleado = new Empleado(7, 2);
        var inventario = new Inventario(8, 1);
        var ejemplar = new Ejemplar(8, "EJ-001");
        AsignarId(solicitud, 11);
        AsignarId(usuario, 5);
        AsignarId(empleado, 3);
        AsignarId(inventario, 4);
        AsignarId(ejemplar, 9);
        var solicitudes = new Mock<ISolicitudPrestamoRepository>();
        solicitudes.Setup(repository => repository.ObtenerPorIdAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(solicitud);
        var usuarios = new Mock<IUsuarioRepository>();
        usuarios.Setup(repository => repository.ObtenerPorIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        var inventarios = new Mock<IInventarioRepository>();
        inventarios.Setup(repository => repository.ObtenerPorLibroIdAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventario);
        var ejemplares = new Mock<IEjemplarRepository>();
        ejemplares.Setup(repository => repository.ObtenerDisponibleParaPrestamoAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ejemplar);
        var multas = new Mock<IMultaRepository>();
        multas.Setup(repository => repository.TienePendientesPorUsuarioAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var prestamos = new Mock<IPrestamoRepository>();
        prestamos.Setup(repository => repository.TieneVencidosPorUsuarioAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        prestamos.Setup(repository => repository.ContarActivosPorUsuarioAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var resolver = new PrestamoRegistroContextoResolver(
            solicitudes.Object,
            usuarios.Object,
            inventarios.Object,
            ejemplares.Object,
            multas.Object,
            prestamos.Object);

        var resultado = await resolver.ResolverAsync(11, empleado);

        Assert.Same(ejemplar, resultado.Ejemplar);
        ejemplares.Verify(repository => repository.ObtenerDisponibleParaPrestamoAsync(
            8,
            It.IsAny<CancellationToken>()), Times.Once);
        ejemplares.Verify(repository => repository.ObtenerDisponiblePorLibroAsync(
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResponsableResolver_PriorizaLaIdentidadAutenticada()
    {
        var empleado = new Empleado(15, 2);
        AsignarId(empleado, 4);
        var empleados = new Mock<IEmpleadoRepository>();
        empleados.Setup(repository => repository.ObtenerPorUsuarioIdAsync(
                15,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(empleado);
        var usuarioActual = new Mock<IUsuarioActual>();
        usuarioActual.SetupGet(contexto => contexto.EstaAutenticado).Returns(true);
        usuarioActual.SetupGet(contexto => contexto.UsuarioId).Returns(15);
        var resolver = new ResponsablePrestamoResolver(
            empleados.Object,
            usuarioActual.Object);

        var resultado = await resolver.ResolverEmpleadoAsync(999);

        Assert.Same(empleado, resultado);
        empleados.Verify(repository => repository.ObtenerPorIdAsync(
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarPrestamo_ConservaLaExcepcionOriginalYLaTraza()
    {
        var empleado = new Empleado(7, 2);
        AsignarId(empleado, 3);
        var errorDominio = new BusinessRuleException("No hay ejemplares disponibles.");
        var responsables = new Mock<IResponsablePrestamoResolver>();
        responsables.Setup(service => service.ResolverEmpleadoAsync(
                3,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(empleado);
        var contexto = new Mock<IPrestamoRegistroContextoResolver>();
        contexto.Setup(service => service.ResolverAsync(
                11,
                empleado,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(errorDominio);
        var service = new PrestamoRegistroService(
            contexto.Object,
            responsables.Object,
            Mock.Of<IPrestamoPersistenciaOperaciones>(),
            new PrestamoDomainService(),
            new PoliticaPrestamos(),
            UnidadDeTrabajoEjecutable().Object,
            Mock.Of<IPrestamoEventosService>(),
            Mock.Of<IMapper>(),
            NullLogger<PrestamoRegistroService>.Instance);

        var capturada = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RegistrarPrestamoAsync(new RegistrarPrestamoDto
            {
                SolicitudPrestamoId = 11,
                EmpleadoPrestamoId = 3,
                FechaPrestamo = DateTime.UtcNow
            }));

        Assert.Same(errorDominio, capturada);
    }

    [Fact]
    public async Task EventosPrestamo_AgregaAuditoriaYNotificacionesPorLote()
    {
        var auditorias = new Mock<IAuditoriaRepository>();
        var notificaciones = new Mock<INotificacionRepository>();
        var service = new PrestamoEventosService(
            auditorias.Object,
            notificaciones.Object);
        IReadOnlyCollection<PrestamoEventoAplicacion> lote =
        [
            new(7, ModuloAuditoria.Prestamos, AccionAuditoria.Aprobar,
                "Préstamo aprobado.", 5, "Préstamo disponible.", TipoNotificacion.Informacion),
            new(7, ModuloAuditoria.Prestamos, AccionAuditoria.ActualizarEstado,
                "Préstamo vencido.", 6, "Préstamo vencido.", TipoNotificacion.Vencimiento)
        ];

        await service.AgregarRangoAsync(lote);

        auditorias.Verify(repository => repository.AgregarRangoAsync(
            It.Is<IEnumerable<AuditoriaEntidad>>(items => items.Count() == 2),
            It.IsAny<CancellationToken>()), Times.Once);
        notificaciones.Verify(repository => repository.AgregarRangoAsync(
            It.Is<IEnumerable<Notificacion>>(items => items.Count() == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Prestamo CrearPrestamo(
        int usuarioId,
        DateTime fechaReferencia,
        int id)
    {
        var prestamo = new Prestamo(
            usuarioId,
            libroId: 2,
            ejemplarId: id,
            solicitudPrestamoId: id,
            empleadoPrestamoId: 7,
            fechaPrestamo: fechaReferencia.AddDays(-10),
            fechaEsperadaDevolucion: fechaReferencia.AddDays(-1));
        AsignarId(prestamo, id);
        return prestamo;
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

    private static void AsignarId(EntidadBase entidad, int id) =>
        typeof(EntidadBase)
            .GetProperty(nameof(EntidadBase.Id))!
            .SetValue(entidad, id);
}
