using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SIGEBI.API.Controllers;
using SIGEBI.Application.Dtos.Auth;
using SIGEBI.Application.Dtos.Usuarios;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Interfaces.Usuarios;

namespace SIGEBI.Tests.API;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Login_CredencialesInvalidas_DevuelveNoAutorizado()
    {
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(service => service.AuthenticateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthenticationException("Credenciales inválidas."));
        var controller = CreateController(authentication.Object);

        var result = await controller.Login(new LoginRequest
        {
            Email = "usuario@sigebi.test",
            Password = "Incorrecta123"
        });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task LoginExitoso_EmiteIssuerAudienceRolesYPermisos()
    {
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(service => service.AuthenticateAsync(
                "ana@sigebi.test",
                "Correcta123",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticatedUserDto
            {
                Usuario = new UsuarioDto { Id = 8, Email = "ana@sigebi.test" },
                Roles = ["Administrador", "Gestor"],
                Permisos = ["SIGEBI.ADMIN"]
            });
        var controller = CreateController(authentication.Object);

        var result = Assert.IsType<OkObjectResult>(await controller.Login(
            new LoginRequest
            {
                Email = "ana@sigebi.test",
                Password = "Correcta123"
            }));
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        var token = new JwtSecurityTokenHandler().ReadJwtToken(
            payload.RootElement.GetProperty("Token").GetString());

        Assert.Equal("SIGEBI.API", token.Issuer);
        Assert.Contains("SIGEBI.Clients", token.Audiences);
        Assert.Contains(token.Claims, claim =>
            claim.Type == "permission" && claim.Value == "SIGEBI.ADMIN");
        Assert.Contains(token.Claims, claim =>
            claim.Type == "role" && claim.Value == "Administrador");
        Assert.Contains(token.Claims, claim =>
            claim.Type == "role" && claim.Value == "Gestor");
    }

    private static AuthController CreateController(IAuthenticationService authentication)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "SIGEBI-test-key-with-more-than-32-characters",
                ["Jwt:Issuer"] = "SIGEBI.API",
                ["Jwt:Audience"] = "SIGEBI.Clients"
            })
            .Build();
        return new AuthController(
            configuration,
            authentication,
            Mock.Of<IUsuarioService>(),
            Mock.Of<IPasswordRecoveryService>(),
            Mock.Of<IPasswordResetEmailSender>(),
            Mock.Of<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(),
            NullLogger<AuthController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }
}
