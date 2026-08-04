using Moq;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Options;
using SIGEBI.Application.Security;
using SIGEBI.Application.Services.Seguridad;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Tests.Application;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public async Task CredencialesValidas_GuardanEstadoYAuditoriaAntesDeResponder()
    {
        var usuario = new Usuario(
            "Ana",
            "Pérez",
            "002",
            "ana@sigebi.test",
            TipoUsuario.Estudiante);
        usuario.EstablecerContrasenaHash(PasswordHasher.Hash("Correcta123"));
        typeof(SIGEBI.Domain.Base.EntidadBase)
            .GetProperty(nameof(SIGEBI.Domain.Base.EntidadBase.Id))!
            .SetValue(usuario, 8);

        var usuarios = new Mock<IUsuarioRepository>();
        usuarios.Setup(repository => repository.ObtenerPorEmailAsync(
                usuario.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        var auditoria = new Mock<IAuditoriaWriter>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(item => item.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        var service = new AuthenticationService(
            usuarios.Object,
            auditoria.Object,
            unitOfWork.Object,
            new AuthenticationOptions());

        var resultado = await service.AuthenticateAsync(usuario.Email, "Correcta123");

        Assert.Equal(8, resultado.Usuario.Id);
        usuarios.Verify(repository => repository.Actualizar(usuario), Times.Once);
        auditoria.Verify(
            writer => writer.RegistrarAsync(
                8,
                ModuloAuditoria.Usuarios,
                AccionAuditoria.Registrar,
                It.IsAny<string>(),
                ResultadoAuditoria.Exitoso,
                It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(
            item => item.GuardarCambiosAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CredencialesInvalidas_BloqueanCuentaTrasIntentosConfigurados()
    {
        var usuario = new Usuario(
            "Luis",
            "Pérez",
            "001",
            "luis@sigebi.test",
            TipoUsuario.Estudiante);
        usuario.EstablecerContrasenaHash(PasswordHasher.Hash("Correcta123"));
        typeof(SIGEBI.Domain.Base.EntidadBase)
            .GetProperty(nameof(SIGEBI.Domain.Base.EntidadBase.Id))!
            .SetValue(usuario, 7);

        var usuarios = new Mock<IUsuarioRepository>();
        usuarios.Setup(repository => repository.ObtenerPorEmailAsync(
                usuario.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        var auditoria = new Mock<IAuditoriaWriter>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(item => item.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var service = new AuthenticationService(
            usuarios.Object,
            auditoria.Object,
            unitOfWork.Object,
            new AuthenticationOptions { MaxFailedAttempts = 2, LockoutMinutes = 15 });

        await Assert.ThrowsAsync<AuthenticationException>(() =>
            service.AuthenticateAsync(usuario.Email, "Incorrecta123"));
        await Assert.ThrowsAsync<AuthenticationException>(() =>
            service.AuthenticateAsync(usuario.Email, "Incorrecta123"));

        Assert.True(usuario.EstaBloqueado(DateTime.UtcNow));
        Assert.Equal(2, usuario.IntentosAccesoFallidos);
        auditoria.Verify(
            writer => writer.RegistrarAsync(
                7,
                ModuloAuditoria.Usuarios,
                AccionAuditoria.ActualizarEstado,
                It.IsAny<string>(),
                ResultadoAuditoria.Fallido,
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}
