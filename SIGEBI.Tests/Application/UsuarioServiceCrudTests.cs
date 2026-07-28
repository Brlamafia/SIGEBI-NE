using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SIGEBI.Application.Dtos.Usuarios;
using SIGEBI.Application.Services.Usuarios;
using SIGEBI.Domain.Base;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.Notificaciones;

namespace SIGEBI.Tests.Application;

public class UsuarioServiceCrudTests
{
    [Fact]
    public async Task CrearActualizarYEliminar_UsaPersistenciaYTodosLosCampos()
    {
        var genericRepository = new Mock<IRepository<Usuario>>();
        var users = new Mock<IUsuarioRepository>();
        var mapper = new Mock<IMapper>();
        var createdEntity = new Usuario(
            "Prueba",
            "Inicial",
            "TEST-001",
            "inicial@sigebi.test",
            TipoUsuario.Estudiante,
            "8090000000");
        AsignarId(createdEntity, 77);
        mapper.Setup(value => value.Map<Usuario>(It.IsAny<SaveUsuarioDto>()))
            .Returns(createdEntity);
        mapper.Setup(value => value.Map<UsuarioDto>(It.IsAny<Usuario>()))
            .Returns<Usuario>(entity => new UsuarioDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Apellido = entity.Apellido,
                Cedula = entity.Cedula,
                Telefono = entity.Telefono,
                Email = entity.Email,
                TipoUsuario = entity.TipoUsuario.ToString(),
                Estado = entity.Estado.ToString()
            });
        users.Setup(value => value.ObtenerPorIdAsync(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEntity);
        users.Setup(value => value.TieneRelacionesAsync(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new UsuarioService(
            genericRepository.Object,
            users.Object,
            Mock.Of<ISolicitudPrestamoRepository>(),
            Mock.Of<IPrestamoService>(),
            Mock.Of<IMultaService>(),
            Mock.Of<INotificacionService>(),
            mapper.Object,
            NullLogger<UsuarioService>.Instance);

        var created = await service.CrearAsync(new SaveUsuarioDto
        {
            Nombre = "Prueba",
            Apellido = "Inicial",
            Cedula = "TEST-001",
            Telefono = "8090000000",
            Email = "inicial@sigebi.test",
            Password = "Segura123",
            TipoUsuario = TipoUsuario.Estudiante
        });
        var updated = await service.ActualizarAsync(77, new UpdateUsuarioDto
        {
            Nombre = "Prueba",
            Apellido = "Actualizada",
            Cedula = "TEST-002",
            Telefono = "8290000000",
            Email = "actualizada@sigebi.test",
            TipoUsuario = TipoUsuario.Docente,
            Estado = EstadoUsuario.Suspendido
        });
        await service.EliminarAsync(77);

        Assert.Equal(77, created.Id);
        Assert.Equal("Actualizada", updated.Apellido);
        Assert.Equal("TEST-002", updated.Cedula);
        Assert.Equal("8290000000", updated.Telefono);
        Assert.Equal("actualizada@sigebi.test", updated.Email);
        Assert.Equal("Docente", updated.TipoUsuario);
        Assert.Equal("Suspendido", updated.Estado);
        users.Verify(value => value.AgregarAsync(createdEntity, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(EstadoUsuario.Inactivo, createdEntity.Estado);
        genericRepository.Verify(value => value.ActualizarAsync(createdEntity), Times.Exactly(2));
        genericRepository.Verify(value => value.EliminarAsync(createdEntity), Times.Never);
    }

    [Fact]
    public async Task Eliminar_ConRelacionesDevuelveConflictoSinBorrar()
    {
        var genericRepository = new Mock<IRepository<Usuario>>();
        var users = new Mock<IUsuarioRepository>();
        var user = new Usuario(
            "Usuario",
            "Relacionado",
            "REL-001",
            "relacionado@sigebi.test",
            TipoUsuario.Estudiante);
        AsignarId(user, 9);
        users.Setup(value => value.ObtenerPorIdAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        users.Setup(value => value.TieneRelacionesAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = new UsuarioService(
            genericRepository.Object,
            users.Object,
            Mock.Of<ISolicitudPrestamoRepository>(),
            Mock.Of<IPrestamoService>(),
            Mock.Of<IMultaService>(),
            Mock.Of<INotificacionService>(),
            Mock.Of<IMapper>(),
            NullLogger<UsuarioService>.Instance);

        await service.EliminarAsync(9);

        Assert.Equal(SIGEBI.Domain.Enums.EstadoUsuario.Inactivo, user.Estado);
        genericRepository.Verify(value => value.EliminarAsync(It.IsAny<Usuario>()), Times.Never);
        genericRepository.Verify(value => value.ActualizarAsync(user), Times.Once);
    }

    private static void AsignarId(EntidadBase entity, int id) =>
        typeof(EntidadBase).GetProperty(nameof(EntidadBase.Id), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(entity, id);
}
