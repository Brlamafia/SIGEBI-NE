using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using SIGEBI.API.Controllers;

namespace SIGEBI.Tests.API;

public class AuthorizationContractTests
{
    [Fact]
    public void Catalogo_RestringeEscrituraAPersonalAutorizado()
    {
        foreach (var method in new[]
                 {
                     nameof(LibrosController.Post),
                     nameof(LibrosController.Put),
                     nameof(LibrosController.Delete)
                 })
        {
            var authorize = typeof(LibrosController).GetMethod(method)!
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>()
                .Single();
            Assert.Equal("Administrador,Bibliotecario", authorize.Roles);
        }
    }

    [Fact]
    public void Reportes_SoloAdmitenAdministradorOAuditor()
    {
        var authorize = typeof(ReportesController)
            .GetMethod(nameof(ReportesController.GetReporteCatalogo))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .Single();
        Assert.Equal("Administrador,Auditor", authorize.Roles);
    }

    [Fact]
    public void Notificaciones_NoExponeEliminacion()
    {
        var deleteRoutes = typeof(NotificacionesController).GetMethods()
            .SelectMany(method => method.GetCustomAttributes(true))
            .OfType<HttpMethodAttribute>()
            .Where(attribute => attribute.HttpMethods.Contains("DELETE"));
        Assert.Empty(deleteRoutes);
    }

    [Fact]
    public void Usuarios_PermiteConsultaABibliotecarioPeroRestringeEscritura()
    {
        foreach (var method in new[]
                 {
                     nameof(UsuariosController.GetAll),
                     nameof(UsuariosController.GetById),
                     nameof(UsuariosController.GetDetallesUsuario)
                 })
        {
            var authorize = GetAuthorize<UsuariosController>(method);
            Assert.Equal("Administrador,Bibliotecario", authorize.Roles);
        }

        foreach (var method in new[]
                 {
                     nameof(UsuariosController.Post),
                     nameof(UsuariosController.Put),
                     nameof(UsuariosController.Delete)
                 })
        {
            var authorize = GetAuthorize<UsuariosController>(method);
            Assert.Equal("Administrador", authorize.Roles);
        }
    }

    private static AuthorizeAttribute GetAuthorize<TController>(string method) =>
        typeof(TController).GetMethod(method)!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .Single();
}
