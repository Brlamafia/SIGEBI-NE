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
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

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

    [Fact]
    public async Task LoginExitoso_EmiteIssuerAudienceRolesYPermisos()
    {
        var permission = new Permiso("Administración completa", "SIGEBI.ADMIN");
        var role = new Rol("Gestor", "Gestión del sistema");
        role.AsignarPermiso(permission);
        var user = new Usuario(
            "Ana", "Gestora", "002", "ana@sigebi.test", TipoUsuario.Administrativo);
        user.AsignarRol(role);
        user.EstablecerContrasenaHash(PasswordHasher.Hash("Correcta123"));
        AsignarId(user, 8);

        var users = new Mock<IUsuarioRepository>();
        users.Setup(repository => repository.ObtenerPorEmailAsync(
                user.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var administrators = new Mock<IAdministradorRepository>();
        administrators.Setup(repository => repository.ObtenerPorUsuarioIdAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Administrador(user.Id, 1));
        var userService = new Mock<IUsuarioService>();
        userService.Setup(service => service.GetByIdAsync(user.Id))
            .ReturnsAsync(new SIGEBI.Application.Dtos.Usuarios.UsuarioDto
            {
                Id = user.Id,
                Email = user.Email
            });
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(item => item.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "SIGEBI-test-key-with-more-than-32-characters",
                ["Jwt:Issuer"] = "SIGEBI.API",
                ["Jwt:Audience"] = "SIGEBI.Clients"
            })
            .Build();
        var controller = new AuthController(
            configuration,
            userService.Object,
            users.Object,
            administrators.Object,
            Mock.Of<IEmpleadoRepository>(),
            Mock.Of<IAuditoriaWriter>(),
            unitOfWork.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = Assert.IsType<OkObjectResult>(await controller.Login(
            new LoginRequest { Email = user.Email, Password = "Correcta123" }));
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        var tokenText = payload.RootElement.GetProperty("Token").GetString();
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenText);

        Assert.Equal("SIGEBI.API", token.Issuer);
        Assert.Contains("SIGEBI.Clients", token.Audiences);
        Assert.Contains(token.Claims, claim =>
            claim.Type == "permission" && claim.Value == "SIGEBI.ADMIN");
        Assert.Contains(token.Claims, claim =>
            claim.Type == "role" && claim.Value == "Administrador");
        Assert.Contains(token.Claims, claim =>
            claim.Type == "role" && claim.Value == "Gestor");
        Assert.Equal(0, user.IntentosAccesoFallidos);
    }

    private static void AsignarId(SIGEBI.Domain.Base.EntidadBase entity, int id) =>
        typeof(SIGEBI.Domain.Base.EntidadBase)
            .GetProperty(nameof(SIGEBI.Domain.Base.EntidadBase.Id))!
            .SetValue(entity, id);
}
