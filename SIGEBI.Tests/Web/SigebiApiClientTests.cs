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

    private sealed class StubHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
