using System.Net;
using System.Text;
using SIGEBI.Desktop;

namespace SIGEBI.Tests.Desktop;

public sealed class ApiClientTests
{
    [Fact]
    public async Task GetAsync_ReutilizaRespuestaRecienteSinOtraPeticionHttp()
    {
        var requests = 0;
        var handler = new StubHandler(_ =>
        {
            requests++;
            return JsonResponse("""{"items":[1]}""");
        });
        using var httpClient = new HttpClient(handler);
        using var client = new ApiClient(httpClient);
        client.ConfigurarBaseUrl("https://api.sigebi.test");

        await client.GetAsync("api/catalogo");
        await client.GetAsync("api/catalogo");

        Assert.Equal(1, requests);
    }

    [Fact]
    public async Task GetAsync_ComparteUnaPeticionCuandoVariasConsultasCoinciden()
    {
        var requests = 0;
        var handler = new DelayedStubHandler(async cancellationToken =>
        {
            Interlocked.Increment(ref requests);
            await Task.Delay(75, cancellationToken);
            return JsonResponse("""{"items":[1]}""");
        });
        using var httpClient = new HttpClient(handler);
        using var client = new ApiClient(httpClient);
        client.ConfigurarBaseUrl("https://api.sigebi.test");

        await Task.WhenAll(
            client.GetAsync("api/catalogo"),
            client.GetAsync("api/catalogo"),
            client.GetAsync("api/catalogo"));

        Assert.Equal(1, requests);
    }

    [Fact]
    public async Task EscrituraYActualizacionExplicita_InvalidanLaCacheDeLectura()
    {
        var getRequests = 0;
        var handler = new StubHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
                getRequests++;
            return JsonResponse("{}");
        });
        using var httpClient = new HttpClient(handler);
        using var client = new ApiClient(httpClient);
        client.ConfigurarBaseUrl("https://api.sigebi.test");

        await client.GetAsync("api/catalogo");
        await client.GetFreshAsync("api/catalogo");
        await client.PostAsync("api/catalogo", new { titulo = "Prueba" });
        await client.GetAsync("api/catalogo");

        Assert.Equal(3, getRequests);
    }

    [Fact]
    public async Task ConfigurarBaseUrl_PermiteReutilizarLaMismaUrlDespuesDeUnaSolicitud()
    {
        var handler = new StubHandler(_ => JsonResponse("""{"items":[]}"""));
        using var httpClient = new HttpClient(handler);
        using var client = new ApiClient(httpClient);
        client.ConfigurarBaseUrl("https://api.sigebi.test");

        await client.GetAsync("api/prueba");

        var exception = Record.Exception(
            () => client.ConfigurarBaseUrl("https://api.sigebi.test"));
        Assert.Null(exception);
    }

    [Fact]
    public async Task IniciarSesion_ConfiguraTokenYPermiteConsumirLaApi()
    {
        string? authorization = null;
        var handler = new StubHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/Auth/login")
            {
                return JsonResponse(
                    """
                    {
                      "token": "token-prueba",
                      "usuario": {
                        "id": 7,
                        "nombre": "Stanley",
                        "apellido": "Prueba",
                        "email": "stanley@sigebi.test"
                      },
                      "roles": ["Administrador"],
                      "permisos": ["SIGEBI.ADMIN"]
                    }
                    """);
            }

            authorization = request.Headers.Authorization?.ToString();
            return JsonResponse("""{"items":[]}""");
        });
        using var httpClient = new HttpClient(handler);
        using var client = new ApiClient(httpClient);
        client.ConfigurarBaseUrl("https://api.sigebi.test");

        var session = await client.IniciarSesionAsync(
            "stanley@sigebi.test",
            "clave-segura");
        await client.GetAsync("api/usuarios");

        Assert.Equal(7, session.Usuario.Id);
        Assert.True(session.PuedeUsarDesktop);
        Assert.True(session.TienePermiso("sigebi.admin"));
        Assert.Equal("Bearer token-prueba", authorization);
    }

    [Theory]
    [InlineData("Administrador", true)]
    [InlineData("Bibliotecario", true)]
    [InlineData("Auditor", true)]
    [InlineData("Estudiante", false)]
    [InlineData("Docente", false)]
    public void PuedeUsarDesktop_RespetaLosRolesDelPersonal(
        string role,
        bool expected)
    {
        var session = new DesktopSession { Roles = [role] };

        Assert.Equal(expected, session.PuedeUsarDesktop);
    }

    [Fact]
    public async Task RespuestaNoAutorizada_LanzaExcepcionDeSesionExpirada()
    {
        var handler = new StubHandler(_ =>
            JsonResponse(
                """{"detail":"Token expirado"}""",
                HttpStatusCode.Unauthorized));
        using var httpClient = new HttpClient(handler);
        using var client = new ApiClient(httpClient);
        client.ConfigurarBaseUrl("https://api.sigebi.test");

        var exception = await Assert.ThrowsAsync<DesktopSessionExpiredException>(
            () => client.GetAsync("api/usuarios"));

        Assert.Equal("Token expirado", exception.Message);
    }

    [Fact]
    public async Task CerrarSesion_EliminaTokenDeLasPeticiones()
    {
        var authorizationHeaders = new List<string?>();
        var handler = new StubHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/Auth/login")
            {
                return JsonResponse(
                    """
                    {
                      "token":"token-prueba",
                      "usuario":{"id":1,"nombre":"A","apellido":"B","email":"a@b.test"},
                      "roles":["Bibliotecario"],
                      "permisos":[]
                    }
                    """);
            }

            authorizationHeaders.Add(request.Headers.Authorization?.ToString());
            return JsonResponse("{}");
        });
        using var httpClient = new HttpClient(handler);
        using var client = new ApiClient(httpClient);
        client.ConfigurarBaseUrl("https://api.sigebi.test");
        await client.IniciarSesionAsync("a@b.test", "clave");

        client.CerrarSesion();
        await client.GetAsync("api/catalogo");

        Assert.Null(client.Session);
        Assert.Single(authorizationHeaders);
        Assert.Null(authorizationHeaders[0]);
    }

    private static HttpResponseMessage JsonResponse(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }

    private sealed class DelayedStubHandler(
        Func<CancellationToken, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            responseFactory(cancellationToken);
    }
}
