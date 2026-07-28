using Moq;
using SIGEBI.Application.Dtos.Auth;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Security;
using SIGEBI.Application.Services.Seguridad;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Tests.Application;

public sealed class PasswordRecoveryServiceTests
{
    [Fact]
    public async Task ResetPassword_TokenValido_ActualizaHash()
    {
        var user = new Usuario(
            "Ana",
            "Pérez",
            "001",
            "ana@sigebi.test",
            TipoUsuario.Estudiante);
        user.EstablecerContrasenaHash(PasswordHasher.Hash("Anterior123"));
        typeof(SIGEBI.Domain.Base.EntidadBase)
            .GetProperty(nameof(SIGEBI.Domain.Base.EntidadBase.Id))!
            .SetValue(user, 12);

        var users = new Mock<IUsuarioRepository>();
        users.Setup(repository => repository.ObtenerPorEmailAsync(
                user.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        users.Setup(repository => repository.ObtenerPorIdAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(item => item.GuardarCambiosAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var protector = new FakeTokenProtector();
        var service = new PasswordRecoveryService(
            users.Object,
            protector,
            unitOfWork.Object);

        var token = await service.CreateTokenAsync(user.Email);
        await service.ResetPasswordAsync(token!, "NuevaClave123");

        Assert.True(PasswordHasher.Verify("NuevaClave123", user.ContrasenaHash));
        users.Verify(repository => repository.Actualizar(user), Times.Once);
        unitOfWork.Verify(item => item.GuardarCambiosAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class FakeTokenProtector : IPasswordResetTokenProtector
    {
        private PasswordResetTokenData? _data;

        public string Protect(PasswordResetTokenData data, TimeSpan lifetime)
        {
            _data = data;
            return "protected-token";
        }

        public PasswordResetTokenData Unprotect(string token) =>
            token == "protected-token" && _data is not null
                ? _data
                : throw new InvalidOperationException();
    }
}
