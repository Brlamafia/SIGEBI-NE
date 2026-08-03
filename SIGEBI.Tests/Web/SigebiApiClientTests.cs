using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using SIGEBI.Web.Services;

namespace SIGEBI.Tests.Web;

public sealed class SigebiApiClientTests
{
    [Fact]
    public async Task ErrorDeValidacion_MuestraElMensajeDelCampo()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(
            HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """
                {
                  "title": "One or more validation errors occurred.",
                  "status": 400,
                  "errors": {
                    "LibroId": ["Se requiere un identificador de libro válido."]
                  }
                }
                """,
                Encoding.UTF8,
                "application/problem+json")
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.sigebi.test/")
        };
        var client = new SigebiApiClient(
            httpClient,
            new ConfigurationBuilder().Build());

        var exception = await Assert.ThrowsAsync<SigebiApiException>(
            () => client.CreateRequestAsync(0));

        Assert.Equal(
            "Se requiere un identificador de libro válido.",
            exception.Message);
        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task ApiNoDisponible_MuestraUnMensajeComprensible()
    {
        using var httpClient = new HttpClient(new ThrowingHandler(
            new HttpRequestException("Connection refused")))
        {
            BaseAddress = new Uri("https://api.sigebi.test/")
        };
        var client = new SigebiApiClient(
            httpClient,
            new ConfigurationBuilder().Build());

        var exception = await Assert.ThrowsAsync<SigebiApiException>(
            () => client.GetMeAsync());

        Assert.Equal(503, exception.StatusCode);
        Assert.Contains("comunicarse con la API", exception.Message);
        Assert.DoesNotContain("Connection refused", exception.Message);
    }

    [Fact]
    public async Task TiempoDeEsperaAgotado_MuestraUnMensajeComprensible()
    {
        using var httpClient = new HttpClient(new ThrowingHandler(
            new TaskCanceledException("timeout")))
        {
            BaseAddress = new Uri("https://api.sigebi.test/")
        };
        var client = new SigebiApiClient(
            httpClient,
            new ConfigurationBuilder().Build());

        var exception = await Assert.ThrowsAsync<SigebiApiException>(
            () => client.GetMeAsync());

        Assert.Equal(504, exception.StatusCode);
        Assert.Contains("tardó demasiado", exception.Message);
    }

    [Fact]
    public async Task DetalleLibro_UsaElEndpointPorIdentificador()
    {
        string? requestedPath = null;
        var handler = new StubHandler(request =>
        {
            requestedPath = request.RequestUri?.AbsolutePath;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "id": 10,
                      "titulo": "Clean Architecture",
                      "autor": "Robert C. Martin",
                      "isbn": "9780134494166",
                      "genero": "Tecnología",
                      "editorial": "Prentice Hall",
                      "estado": "Activo",
                      "descripcion": "Explica los principios de una arquitectura de software sostenible.",
                      "cantidadTotal": 3,
                      "cantidadDisponible": 2,
                      "cantidadPrestada": 1
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.sigebi.test/")
        };
        var client = new SigebiApiClient(
            httpClient,
            new ConfigurationBuilder().Build());

        var book = await client.GetBookByIdAsync(10);

        Assert.Equal("/api/Libros/10", requestedPath);
        Assert.Equal("Clean Architecture", book.Titulo);
        Assert.Equal(2, book.CantidadDisponible);
        Assert.Contains("arquitectura", book.Descripcion);
    }

    [Fact]
    public async Task ErrorApi_ConservaElDetalleParaDecisionesDeIntegracion()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(
            HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(
                "\"No existe una cuenta activa de SIGEBI asociada a este correo.\"",
                Encoding.UTF8,
                "application/json")
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.sigebi.test/")
        };
        var client = new SigebiApiClient(
            httpClient,
            new ConfigurationBuilder().Build());

        var exception = await Assert.ThrowsAsync<SigebiApiException>(
            () => client.LoginAsync("lector@sigebi.test", "Clave123"));

        Assert.Equal(HttpStatusCode.Unauthorized, (HttpStatusCode)exception.StatusCode);
        Assert.Contains("No existe una cuenta activa", exception.ResponseDetail);
    }

    private sealed class StubHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(exception);
    }
}
