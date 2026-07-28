using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using SIGEBI.API.Controllers;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Usuarios;
using SIGEBI.Application.Security;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Tests.API;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Login_BloqueaCuentaTrasIntentosConfiguradosYAUDita()
    {
        var usuario = new Usuario(
            "Luis", "Pérez", "001", "luis@sigebi.test", TipoUsuario.Estudiante);
        usuario.EstablecerContrasenaHash(PasswordHasher.Hash("Correcta123"));
        AsignarId(usuario, 7);

        var usuarios = new Mock<IUsuarioRepository>();
        usuarios.Setup(x => x.ObtenerPorEmailAsync(
                usuario.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        var auditoria = new Mock<IAuditoriaWriter>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "SIGEBI-test-key-with-more-than-32-characters",
                ["Jwt:Issuer"] = "SIGEBI.API",
                ["Jwt:Audience"] = "SIGEBI.Clients",
                ["Authentication:MaxFailedAttempts"] = "2",
                ["Authentication:LockoutMinutes"] = "15"
            })
            .Build();
        var controller = new AuthController(
            configuration,
            Mock.Of<IUsuarioService>(),
            usuarios.Object,
            Mock.Of<IAdministradorRepository>(),
            Mock.Of<IEmpleadoRepository>(),
            auditoria.Object,
            unitOfWork.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        var request = new LoginRequest
        {
            Email = usuario.Email,
            Password = "Incorrecta123"
        };

        Assert.IsType<UnauthorizedObjectResult>(await controller.Login(request));
        Assert.IsType<UnauthorizedObjectResult>(await controller.Login(request));

        Assert.True(usuario.EstaBloqueado(DateTime.UtcNow));
        Assert.Equal(2, usuario.IntentosAccesoFallidos);
        auditoria.Verify(
            x => x.RegistrarAsync(
                7,
                ModuloAuditoria.Usuarios,
                AccionAuditoria.ActualizarEstado,
                It.IsAny<string>(),
                ResultadoAuditoria.Fallido,
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    private static void AsignarId(SIGEBI.Domain.Base.EntidadBase entity, int id) =>
        typeof(SIGEBI.Domain.Base.EntidadBase)
            .GetProperty(nameof(SIGEBI.Domain.Base.EntidadBase.Id))!
            .SetValue(entity, id);
}
